using System;
using System.Threading;
using System.Threading.Tasks;

namespace UniCli.Server.Editor
{
    internal static class TaskExtensions
    {
        public static ValueTask<T> WithCancellation<T>(this Task<T> task, CancellationToken ct)
        {
            if (!ct.CanBeCanceled || task.IsCompleted)
                return new ValueTask<T>(task);

            if (ct.IsCancellationRequested)
                return new ValueTask<T>(Task.FromCanceled<T>(ct));

            return new ValueTask<T>(WaitWithCancellation(task, ct));

            static async Task<T> WaitWithCancellation(Task<T> task, CancellationToken ct)
            {
                var tcs = new TaskCompletionSource<bool>();
                using (ct.Register(() => tcs.TrySetResult(true)))
                {
                    if (task != await Task.WhenAny(task, tcs.Task))
                        throw new OperationCanceledException(ct);
                }
                return await task;
            }
        }

        public static ValueTask WithCancellation(this Task task, CancellationToken ct)
        {
            if (!ct.CanBeCanceled || task.IsCompleted)
                return new ValueTask(task);

            if (ct.IsCancellationRequested)
                return new ValueTask(Task.FromCanceled(ct));

            return new ValueTask(WaitWithCancellation(task, ct));

            static async Task WaitWithCancellation(Task task, CancellationToken ct)
            {
                var tcs = new TaskCompletionSource<bool>();
                using (ct.Register(() => tcs.TrySetResult(true)))
                {
                    if (task != await Task.WhenAny(task, tcs.Task))
                        throw new OperationCanceledException(ct);
                }
                await task;
            }
        }
    }
}
