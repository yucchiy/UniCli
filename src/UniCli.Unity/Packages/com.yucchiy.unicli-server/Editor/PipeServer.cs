using System;
using System.Buffers;
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
        private readonly string _pipeName;
        private readonly Action<CommandRequest, Action<CommandResponse>> _onCommandReceived;
        private readonly CancellationTokenSource _cts = new();
        private readonly TaskCompletionSource<bool> _shutdownTcs = new();
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
            using var server = new NamedPipeServerStream(
                _pipeName,
                PipeDirection.InOut,
                maxNumberOfServerInstances: 10,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

            await server.WaitForConnectionAsync(cancellationToken);

            await HandleClientAsync(server, cancellationToken);
        }

        private async Task HandleClientAsync(NamedPipeServerStream server, CancellationToken cancellationToken)
        {
            try
            {
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
        }
    }
}

