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

        private readonly string _pipeName;
        private readonly Action<CommandRequest, Action<CommandResponse>> _onCommandReceived;
        private readonly CancellationTokenSource _cts = new();
        private readonly TaskCompletionSource<bool> _shutdownTcs = new();
        private readonly SemaphoreSlim _commandSemaphore = new(1, 1);
        private readonly Task _serverLoop;

        public PipeServer(string pipeName, Action<CommandRequest, Action<CommandResponse>> onCommandReceived)
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
                    try
                    {
                        await AcceptClientAsync(cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        break;
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[UniCli] Server error: {ex.Message}");
                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }
            finally
            {
                _shutdownTcs.TrySetResult(true);
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
            using (server)
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
                            if (length <= 0 || length > 1024 * 1024)
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

                                // Send ACK to indicate the request was received
                                await server.WriteAsync(new[] { AckByte }, 0, 1, cancellationToken);
                                await server.FlushAsync(cancellationToken);

                                // Serialize command processing with semaphore
                                await _commandSemaphore.WaitAsync(cancellationToken);
                                try
                                {
                                    var responseTcs = new TaskCompletionSource<CommandResponse>();
                                    _onCommandReceived(request, response => responseTcs.TrySetResult(response));

                                    var commandResponse = await responseTcs.Task;

                                    var responseJson = JsonUtility.ToJson(commandResponse);
                                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                                    var responseLengthBytes = BitConverter.GetBytes(responseBytes.Length);

                                    await server.WriteAsync(responseLengthBytes, 0, 4, cancellationToken);
                                    await server.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);
                                    await server.FlushAsync(cancellationToken);
                                }
                                catch (IOException ex)
                                {
                                    Debug.LogWarning($"[UniCli] Client disconnected during response write for '{request.command}': {ex.Message}");
                                    break;
                                }
                                finally
                                {
                                    _commandSemaphore.Release();
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

                if (recvBuffer[0] != ProtocolConstants.MagicBytes[0] ||
                    recvBuffer[1] != ProtocolConstants.MagicBytes[1] ||
                    recvBuffer[2] != ProtocolConstants.MagicBytes[2] ||
                    recvBuffer[3] != ProtocolConstants.MagicBytes[3])
                {
                    Debug.LogWarning("[UniCli] Handshake failed: invalid magic bytes from client");
                    return false;
                }

                var clientVersion = BitConverter.ToUInt16(recvBuffer, 4);
                if (clientVersion != ProtocolConstants.ProtocolVersion)
                {
                    Debug.LogWarning(
                        $"[UniCli] Protocol version mismatch (server: {ProtocolConstants.ProtocolVersion}, client: {clientVersion})");
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(recvBuffer);
            }

            var sendBuffer = new byte[ProtocolConstants.HandshakeSize];
            Array.Copy(ProtocolConstants.MagicBytes, 0, sendBuffer, 0, 4);
            BitConverter.GetBytes(ProtocolConstants.ProtocolVersion).CopyTo(sendBuffer, 4);

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

        public void Dispose()
        {
            _cts.Cancel();
            _serverLoop?.Wait(TimeSpan.FromSeconds(5));
            _cts.Dispose();
            _commandSemaphore.Dispose();
        }
    }
}

