#nullable enable
using System;
using System.Collections.Concurrent;
using System.Linq;
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
        private CommandDispatcher _dispatcher;
        private readonly ConcurrentQueue<(CommandRequest request, CancellationToken cancellationToken, Action<CommandResponse> callback)> _commandQueue;
        private readonly CancellationTokenSource _cts;
        private readonly Action<string> _logger;
        private readonly Action<string> _errorLogger;
        private readonly Task _serverLoop;
        private Task? _currentCommand;

        public string? CurrentCommandName { get; private set; }
        public DateTime? CurrentCommandStartTime { get; private set; }
        public string[] QueuedCommandNames => _commandQueue.ToArray().Select(item => item.request.command).ToArray();

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

            _commandQueue = new ConcurrentQueue<(CommandRequest, CancellationToken, Action<CommandResponse>)>();
            _cts = new CancellationTokenSource();

            _serverLoop = Task.Run(
                async () => await RunServerLoopAsync(_cts.Token),
                _cts.Token);
        }

        private void Stop()
        {
            _cts.Cancel();
            _serverLoop.Wait(TimeSpan.FromMilliseconds(500));
        }

        public void ReplaceDispatcher(CommandDispatcher dispatcher)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        }

        public void ProcessCommands()
        {
            if (_currentCommand is { IsCompleted: false })
                return;

            _currentCommand = null;

            if (_commandQueue.TryDequeue(out var item))
            {
                var (request, cancellationToken, callback) = item;
                CurrentCommandName = request.command;
                CurrentCommandStartTime = DateTime.UtcNow;
                _currentCommand = ProcessCommandAsync(request, cancellationToken, callback);
            }
        }

        private async Task RunServerLoopAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var pipeServer = new PipeServer(
                        _pipeName,
                        OnCommandReceived);

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

                try
                {
                    if (!cancellationToken.IsCancellationRequested)
                        await Task.Delay(2000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }

        private void OnCommandReceived(CommandRequest request, CancellationToken cancellationToken, Action<CommandResponse> callback)
        {
            _commandQueue.Enqueue((request, cancellationToken, callback));
        }

        private async Task ProcessCommandAsync(CommandRequest request, CancellationToken cancellationToken, Action<CommandResponse> callback)
        {
            try
            {
                var response = await _dispatcher.DispatchAsync(request, cancellationToken);
                callback(response);
            }
            catch (OperationCanceledException)
            {
                _logger($"[UniCli] Command '{request.command}' cancelled (client disconnected)");
                callback(new CommandResponse
                {
                    success = false,
                    message = "Command cancelled: client disconnected",
                    data = ""
                });
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
            finally
            {
                CurrentCommandName = null;
                CurrentCommandStartTime = null;
            }
        }

        public void Dispose()
        {
            Stop();
            _cts.Dispose();
        }
    }
}
