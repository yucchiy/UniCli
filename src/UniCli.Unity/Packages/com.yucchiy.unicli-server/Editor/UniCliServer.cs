using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor
{
    /// <summary>
    /// UniCli Server (pure C# implementation)
    /// Unity-independent server logic
    /// </summary>
    public sealed class UniCliServer : IDisposable
    {
        private readonly string _pipeName;
        private readonly CommandDispatcher _dispatcher;
        private readonly ConcurrentQueue<(CommandRequest request, Action<CommandResponse> callback)> _commandQueue;
        private readonly CancellationTokenSource _cts;
        private readonly Action<string> _logger;
        private readonly Action<string> _errorLogger;
        private readonly Task _serverLoop;

        public UniCliServer(
            string pipeName,
            CommandDispatcher dispatcher,
            Action<string> logger,
            Action<string> errorLogger)
        {
            _pipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _errorLogger = errorLogger ?? throw new ArgumentNullException(nameof(errorLogger));

            _commandQueue = new ConcurrentQueue<(CommandRequest, Action<CommandResponse>)>();
            _cts = new CancellationTokenSource();

            _serverLoop = Task.Run(
                async () => await RunServerLoopAsync(_cts.Token),
                _cts.Token);
        }

        public void Stop()
        {
            _cts.Cancel();
            _serverLoop?.Wait(TimeSpan.FromSeconds(5));
        }

        public void ProcessCommands()
        {
            while (_commandQueue.TryDequeue(out var item))
            {
                var (request, callback) = item;
                ProcessCommand(request, callback);
            }
        }

        private async Task RunServerLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var pipeServer = new PipeServer(_pipeName, OnCommandReceived);

                    await pipeServer.WaitForShutdownAsync(cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _errorLogger($"[UniCli] Server error: {ex.Message}");
                }

                if (!cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(2000, cancellationToken);
                }
            }
        }

        private void OnCommandReceived(CommandRequest request, Action<CommandResponse> callback)
        {
            _commandQueue.Enqueue((request, callback));
        }

        private async void ProcessCommand(CommandRequest request, Action<CommandResponse> callback)
        {
            try
            {
                var response = await _dispatcher.DispatchAsync(request);
                callback(response);
            }
            catch (Exception ex)
            {
                _errorLogger($"[UniCli] Command processing error: {ex.Message}");
                callback(new CommandResponse
                {
                    success = false,
                    message = $"Internal error: {ex.Message}",
                    data = ""
                });
            }
        }

        public void Dispose()
        {
            Stop();
            _cts.Dispose();
        }
    }
}
