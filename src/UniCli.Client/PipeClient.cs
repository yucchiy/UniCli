using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Client
{
    public sealed class PipeClient : IDisposable
    {
        private readonly string _pipeName;
        private NamedPipeClientStream? _pipeStream;

        public PipeClient(string pipeName)
        {
            if (string.IsNullOrWhiteSpace(pipeName))
            {
                throw new ArgumentException("Pipe name cannot be null or empty", nameof(pipeName));
            }

            _pipeName = pipeName;
        }

        /// <summary>
        /// Connects to the server and performs protocol handshake
        /// </summary>
        public async Task<Result<bool, string>> ConnectAsync(
            int timeoutMs = 5000,
            CancellationToken cancellationToken = default)
        {
            if (_pipeStream != null)
                return Result<bool, string>.Error("Client is already connected");

            try
            {
                _pipeStream = new NamedPipeClientStream(
                    ".",
                    _pipeName,
                    PipeDirection.InOut,
                    PipeOptions.Asynchronous);

                await _pipeStream.ConnectAsync(timeoutMs, cancellationToken);

                // Perform handshake within the remaining timeout
                using var handshakeCts = timeoutMs > 0
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    : null;
                handshakeCts?.CancelAfter(timeoutMs);
                var handshakeToken = handshakeCts?.Token ?? cancellationToken;

                var handshakeResult = await PerformHandshakeAsync(handshakeToken);
                if (!handshakeResult.IsSuccess)
                {
                    _pipeStream.Dispose();
                    _pipeStream = null;
                    return handshakeResult;
                }

                return Result<bool, string>.Success(true);
            }
            catch (TimeoutException)
            {
                _pipeStream?.Dispose();
                _pipeStream = null;
                return Result<bool, string>.Error("Connection timeout - Unity server may not be running");
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                _pipeStream?.Dispose();
                _pipeStream = null;
                return Result<bool, string>.Error("Connection timeout - Unity server may not be running");
            }
            catch (IOException ex)
            {
                _pipeStream?.Dispose();
                _pipeStream = null;
                return Result<bool, string>.Error($"Connection failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                _pipeStream?.Dispose();
                _pipeStream = null;
                return Result<bool, string>.Error($"Unexpected error: {ex.Message}");
            }
        }

        private async Task<Result<bool, string>> PerformHandshakeAsync(CancellationToken cancellationToken)
        {
            var sendBuffer = ProtocolConstants.BuildHandshakeBuffer();

            await _pipeStream!.WriteAsync(sendBuffer.AsMemory(0, ProtocolConstants.HandshakeSize), cancellationToken);
            await _pipeStream.FlushAsync(cancellationToken);

            var recvBuffer = new byte[ProtocolConstants.HandshakeSize];
            if (!await ReadExactAsync(recvBuffer, ProtocolConstants.HandshakeSize, cancellationToken))
                return Result<bool, string>.Error("Server closed connection during handshake");

            if (!ProtocolConstants.ValidateMagicBytes(recvBuffer))
                return Result<bool, string>.Error("Handshake failed: not a UniCli server (invalid magic bytes)");

            var serverVersion = BitConverter.ToUInt16(recvBuffer, 4);
            if (serverVersion != ProtocolConstants.ProtocolVersion)
            {
                return Result<bool, string>.Error(
                    $"Protocol version mismatch (client: {ProtocolConstants.ProtocolVersion}, server: {serverVersion}). "
                    + "Please update unicli or the Unity server package.");
            }

            return Result<bool, string>.Success(true);
        }

        /// <summary>
        /// Sends a command and receives the response.
        /// Uses a two-phase timeout: the timeout applies only to sending the request
        /// and receiving the ACK. After ACK, the response is awaited without timeout.
        /// </summary>
        public async Task<Result<CommandResponse, string>> SendCommandAsync(
            CommandRequest request,
            int timeoutMs = 0,
            CancellationToken cancellationToken = default)
        {
            if (_pipeStream == null || !_pipeStream.IsConnected)
                return Result<CommandResponse, string>.Error("Not connected to server");

            try
            {
                // --- Phase 1: Send request + read ACK (with timeout) ---
                using var timeoutCts = timeoutMs > 0
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                    : null;
                timeoutCts?.CancelAfter(timeoutMs);
                var phase1Token = timeoutCts?.Token ?? cancellationToken;

                var requestJson = JsonSerializer.Serialize(request, ProtocolJsonContext.Default.CommandRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                var lengthBytes = BitConverter.GetBytes(requestBytes.Length);

                await _pipeStream.WriteAsync(lengthBytes.AsMemory(0, 4), phase1Token);
                await _pipeStream.WriteAsync(requestBytes, phase1Token);
                await _pipeStream.FlushAsync(phase1Token);

                // Read ACK byte (0x01)
                var ackBuffer = new byte[1];
                var ackBytesRead = await _pipeStream.ReadAsync(ackBuffer.AsMemory(0, 1), phase1Token);
                if (ackBytesRead == 0)
                    return Result<CommandResponse, string>.Error(
                        $"Server closed connection before sending ACK\n"
                        + $"  Command: {request.command}\n"
                        + $"  Pipe: {_pipeName}\n"
                        + $"  The server may have crashed or does not support the ACK protocol.");

                if (ackBuffer[0] != 0x01)
                    return Result<CommandResponse, string>.Error(
                        $"Unexpected ACK byte from server: 0x{ackBuffer[0]:X2}\n"
                        + $"  Command: {request.command}\n"
                        + $"  Pipe: {_pipeName}\n"
                        + $"  Expected: 0x01");

                // --- Phase 2: Read response (no timeout, only external cancellation) ---
                var responseLengthBytes = new byte[4];
                if (!await ReadExactAsync(responseLengthBytes, 4, cancellationToken))
                    return Result<CommandResponse, string>.Error(
                        $"Server closed connection while reading response length header\n"
                        + $"  Command: {request.command}\n"
                        + $"  Pipe: {_pipeName}\n"
                        + $"  The server may have crashed or restarted during command execution.");

                var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                if (responseLength <= 0 || responseLength > ProtocolConstants.MaxMessageSize)
                    return Result<CommandResponse, string>.Error(
                        $"Invalid response length: {responseLength} bytes\n"
                        + $"  Command: {request.command}\n"
                        + $"  Pipe: {_pipeName}\n"
                        + $"  Expected: 1 to {ProtocolConstants.MaxMessageSize} bytes\n"
                        + $"  Raw header: [{responseLengthBytes[0]:X2} {responseLengthBytes[1]:X2} {responseLengthBytes[2]:X2} {responseLengthBytes[3]:X2}]");

                var responseBytes = new byte[responseLength];
                if (!await ReadExactAsync(responseBytes, responseLength, cancellationToken))
                    return Result<CommandResponse, string>.Error(
                        $"Server closed connection while reading response body\n"
                        + $"  Command: {request.command}\n"
                        + $"  Pipe: {_pipeName}");

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize(responseJson, ProtocolJsonContext.Default.CommandResponse);

                if (response == null)
                    return Result<CommandResponse, string>.Error(
                        $"Failed to deserialize response JSON\n"
                        + $"  Command: {request.command}\n"
                        + $"  Response size: {responseLength} bytes");

                return Result<CommandResponse, string>.Success(response);
            }
            catch (OperationCanceledException) when (timeoutMs > 0 && !cancellationToken.IsCancellationRequested)
            {
                return Result<CommandResponse, string>.Error(
                    $"Command '{request.command}' timed out after {timeoutMs}ms while waiting for server to accept the request.\n"
                    + $"  Pipe: {_pipeName}\n"
                    + $"  Use --timeout to increase the limit.");
            }
            catch (IOException ex)
            {
                return Result<CommandResponse, string>.Error(
                    $"Communication error during '{request.command}': {ex.Message}\n"
                    + $"  Pipe: {_pipeName}");
            }
            catch (Exception ex)
            {
                return Result<CommandResponse, string>.Error(
                    $"Unexpected error during '{request.command}': {ex.Message}\n"
                    + $"  Pipe: {_pipeName}");
            }
        }

        private async Task<bool> ReadExactAsync(byte[] buffer, int count, CancellationToken cancellationToken)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var bytesRead = await _pipeStream!.ReadAsync(
                    buffer.AsMemory(totalRead, count - totalRead), cancellationToken);
                if (bytesRead == 0)
                    return false;
                totalRead += bytesRead;
            }
            return true;
        }

        public void Dispose()
        {
            _pipeStream?.Dispose();
        }
    }
}

