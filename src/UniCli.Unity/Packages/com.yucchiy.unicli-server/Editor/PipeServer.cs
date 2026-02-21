using System;
using System.Buffers;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEngine;

namespace UniCli.Server.Editor
{
    public sealed class PipeServer : IDisposable
    {
        private const byte AckByte = 0x01;
        private static readonly byte[] AckBuffer = { AckByte };

        private readonly string _pipeName;
        private readonly Action<CommandRequest, CancellationToken, Action<CommandResponse>> _onCommandReceived;
        private readonly CancellationTokenSource _cts = new();
        private readonly TaskCompletionSource<bool> _shutdownTcs = new();
        private readonly Task _serverLoop;

        public PipeServer(
            string pipeName,
            Action<CommandRequest, CancellationToken, Action<CommandResponse>> onCommandReceived)
        {
            _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            _onCommandReceived = onCommandReceived ?? throw new ArgumentNullException(nameof(onCommandReceived));

            _serverLoop = Task.Run(async () => await RunLoopAsync(_cts.Token));
        }

        public Task WaitForShutdownAsync(CancellationToken cancellationToken = default)
        {
            using var registration = cancellationToken.Register(() => _shutdownTcs.TrySetCanceled());
            return _shutdownTcs.Task;
        }

        private async Task RunLoopAsync(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    await AcceptClientAsync(cancellationToken);
                }
                _shutdownTcs.TrySetResult(true);
            }
            catch (OperationCanceledException)
            {
                _shutdownTcs.TrySetCanceled();
            }
            catch (Exception ex)
            {
                _shutdownTcs.TrySetException(ex);
            }
        }

        private async Task AcceptClientAsync(CancellationToken cancellationToken)
        {
            var server = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                NamedPipeServerStream.MaxAllowedServerInstances,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await server.WaitForConnectionAsync(cancellationToken);

            // Fire-and-forget: HandleClientAsync owns the server stream and will dispose it
            _ = HandleClientAsync(server, cancellationToken);
        }

        private async Task HandleClientAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
        {
            await using (server)
            {
                try
                {
                    if (!await PerformHandshakeAsync(server, cancellationToken)) 
                        return;

                    while (!cancellationToken.IsCancellationRequested && server.IsConnected)
                    {
                        // Read length prefix (4 bytes)
                        var lengthBuffer = ArrayPool<byte>.Shared.Rent(4);
                        try
                        {
                            if (!await ReadExactAsync(server, lengthBuffer, 4, cancellationToken))
                                break; // Client disconnected

                            var length = BitConverter.ToInt32(lengthBuffer, 0);
                            if (length <= 0 || length > ProtocolConstants.MaxMessageSize)
                            {
                                Debug.LogWarning($"[UniCli] Invalid request length: {length} bytes, closing connection");
                                break;
                            }

                            var jsonBuffer = ArrayPool<byte>.Shared.Rent(length);
                            try
                            {
                                if (!await ReadExactAsync(server, jsonBuffer, length, cancellationToken))
                                {
                                    Debug.LogWarning($"[UniCli] Client disconnected while reading request body ({length} bytes)");
                                    break;
                                }

                                var json = Encoding.UTF8.GetString(jsonBuffer, 0, length);
                                var request = JsonUtility.FromJson<CommandRequest>(json);

                                var commandCts = new CancellationTokenSource();
                                try
                                {
                                    var responseTcs = new TaskCompletionSource<CommandResponse>();

                                    _onCommandReceived(request, commandCts.Token, response => responseTcs.TrySetResult(response));

                                    await server.WriteAsync(AckBuffer, 0, 1, cancellationToken);
                                    await server.FlushAsync(cancellationToken);

                                    var monitorTask = MonitorDisconnectAsync(server, commandCts);
                                    await Task.WhenAny(responseTcs.Task, monitorTask);

                                    if (!responseTcs.Task.IsCompleted)
                                    {
                                        commandCts.Cancel();
                                        break;
                                    }

                                    commandCts.Cancel();
                                    var commandResponse = await responseTcs.Task;

                                    var responseJson = JsonUtility.ToJson(commandResponse);
                                    var responseByteCount = Encoding.UTF8.GetByteCount(responseJson);
                                    var responseBuffer = ArrayPool<byte>.Shared.Rent(responseByteCount);
                                    try
                                    {
                                        Encoding.UTF8.GetBytes(responseJson, 0, responseJson.Length, responseBuffer, 0);
                                        var responseLengthBytes = BitConverter.GetBytes(responseByteCount);

                                        await server.WriteAsync(responseLengthBytes, 0, 4, cancellationToken);
                                        await server.WriteAsync(responseBuffer, 0, responseByteCount, cancellationToken);
                                        await server.FlushAsync(cancellationToken);
                                    }
                                    finally
                                    {
                                        ArrayPool<byte>.Shared.Return(responseBuffer);
                                    }
                                }
                                catch (IOException ex)
                                {
                                    Debug.LogWarning($"[UniCli] Client disconnected during response write for '{request.command}': {ex.Message}");
                                    break;
                                }
                                finally
                                {
                                    commandCts.Dispose();
                                }
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(jsonBuffer);
                            }
                        }
                        finally
                        {
                            ArrayPool<byte>.Shared.Return(lengthBuffer);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[UniCli] Client handling error: {ex.Message}\n{ex.StackTrace}");
                }
            }
        }

        private static async Task<bool> PerformHandshakeAsync(
            NamedPipeServerStream server, CancellationToken cancellationToken)
        {
            var recvBuffer = ArrayPool<byte>.Shared.Rent(ProtocolConstants.HandshakeSize);
            try
            {
                if (!await ReadExactAsync(server, recvBuffer, ProtocolConstants.HandshakeSize, cancellationToken))
                {
                    Debug.LogWarning("[UniCli] Client disconnected during handshake");
                    return false;
                }

                if (!ProtocolConstants.ValidateMagicBytes(recvBuffer))
                {
                    Debug.LogWarning("[UniCli] Handshake failed: invalid magic bytes from client");
                    return false;
                }

                var clientVersion = BitConverter.ToUInt16(recvBuffer, 4);
                if (clientVersion != ProtocolConstants.ProtocolVersion)
                {
                    Debug.LogWarning(
                        $"[UniCli] Protocol version mismatch (server: {ProtocolConstants.ProtocolVersion}, client: {clientVersion}). "
                        + "Please update unicli or the Unity server package.");
                    return false;
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(recvBuffer);
            }

            var sendBuffer = ProtocolConstants.BuildHandshakeBuffer();

            await server.WriteAsync(sendBuffer, 0, ProtocolConstants.HandshakeSize, cancellationToken);
            await server.FlushAsync(cancellationToken);

            return true;
        }

        private static async Task<bool> ReadExactAsync(
            NamedPipeServerStream stream, byte[] buffer, int count, CancellationToken cancellationToken)
        {
            var totalRead = 0;
            while (totalRead < count)
            {
                var bytesRead = await stream.ReadAsync(buffer, totalRead, count - totalRead, cancellationToken);
                if (bytesRead == 0)
                    return false;
                totalRead += bytesRead;
            }
            return true;
        }

        private static async Task MonitorDisconnectAsync(NamedPipeServerStream server, CancellationTokenSource commandCts)
        {
            try
            {
                var buffer = new byte[1];
                var bytesRead = await server.ReadAsync(buffer, 0, 1, commandCts.Token);
                if (bytesRead == 0)
                    commandCts.Cancel();
            }
            catch (OperationCanceledException) { }
            catch (IOException) { commandCts.Cancel(); }
            catch (ObjectDisposedException) { commandCts.Cancel(); }
        }

        public void Dispose()
        {
            _cts.Cancel();
            _serverLoop?.Wait(TimeSpan.FromMilliseconds(500));
            _cts.Dispose();
        }
    }
}
