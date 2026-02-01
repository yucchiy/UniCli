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
        /// Connects to the server
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

                return Result<bool, string>.Success(true);
            }
            catch (TimeoutException)
            {
                return Result<bool, string>.Error("Connection timeout - Unity server may not be running");
            }
            catch (IOException ex)
            {
                return Result<bool, string>.Error($"Connection failed: {ex.Message}");
            }
            catch (Exception ex)
            {
                return Result<bool, string>.Error($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Sends a command and receives the response
        /// </summary>
        public async Task<Result<CommandResponse, string>> SendCommandAsync(
            CommandRequest request,
            int timeoutMs = 0,
            CancellationToken cancellationToken = default)
        {
            if (_pipeStream == null || !_pipeStream.IsConnected)
                return Result<CommandResponse, string>.Error("Not connected to server");

            using var timeoutCts = timeoutMs > 0
                ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
                : null;
            timeoutCts?.CancelAfter(timeoutMs);
            var token = timeoutCts?.Token ?? cancellationToken;

            try
            {
                // Send request as JSON
                var requestJson = JsonSerializer.Serialize(request, ProtocolJsonContext.Default.CommandRequest);
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                var lengthBytes = BitConverter.GetBytes(requestBytes.Length);

                await _pipeStream.WriteAsync(lengthBytes.AsMemory(0, 4), token);
                await _pipeStream.WriteAsync(requestBytes, token);
                await _pipeStream.FlushAsync(token);

                // Read response length (4 bytes)
                var responseLengthBytes = new byte[4];
                var lengthBytesRead = 0;
                while (lengthBytesRead < 4)
                {
                    var bytesRead = await _pipeStream.ReadAsync(
                        responseLengthBytes,
                        lengthBytesRead,
                        4 - lengthBytesRead,
                        token);

                    if (bytesRead == 0)
                        return Result<CommandResponse, string>.Error(
                            $"Server closed connection while reading response length header\n"
                            + $"  Command: {request.command}\n"
                            + $"  Pipe: {_pipeName}\n"
                            + $"  Bytes read: {lengthBytesRead}/4\n"
                            + $"  The server may have crashed or restarted during command execution.");

                    lengthBytesRead += bytesRead;
                }

                var responseLength = BitConverter.ToInt32(responseLengthBytes, 0);
                if (responseLength <= 0 || responseLength > 1024 * 1024)
                    return Result<CommandResponse, string>.Error(
                        $"Invalid response length: {responseLength} bytes\n"
                        + $"  Command: {request.command}\n"
                        + $"  Pipe: {_pipeName}\n"
                        + $"  Expected: 1 to {1024 * 1024} bytes\n"
                        + $"  Raw header: [{responseLengthBytes[0]:X2} {responseLengthBytes[1]:X2} {responseLengthBytes[2]:X2} {responseLengthBytes[3]:X2}]");

                // Read response JSON (may require multiple reads)
                var responseBytes = new byte[responseLength];
                var totalBytesRead = 0;
                while (totalBytesRead < responseLength)
                {
                    var bytesRead = await _pipeStream.ReadAsync(
                        responseBytes,
                        totalBytesRead,
                        responseLength - totalBytesRead,
                        token);

                    if (bytesRead == 0)
                        return Result<CommandResponse, string>.Error(
                            $"Server closed connection while reading response body\n"
                            + $"  Command: {request.command}\n"
                            + $"  Pipe: {_pipeName}\n"
                            + $"  Bytes read: {totalBytesRead}/{responseLength}");

                    totalBytesRead += bytesRead;
                }

                var responseJson = Encoding.UTF8.GetString(responseBytes);
                var response = JsonSerializer.Deserialize(responseJson, ProtocolJsonContext.Default.CommandResponse);

                if (response == null)
                    return Result<CommandResponse, string>.Error(
                        $"Failed to deserialize response JSON\n"
                        + $"  Command: {request.command}\n"
                        + $"  Response size: {responseLength} bytes");

                return Result<CommandResponse, string>.Success(response);
            }
            catch (OperationCanceledException) when (timeoutCts != null && timeoutCts.IsCancellationRequested)
            {
                return Result<CommandResponse, string>.Error(
                    $"Command '{request.command}' timed out after {timeoutMs}ms.\n"
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

        public void Dispose()
        {
            _pipeStream?.Dispose();
        }
    }
}

