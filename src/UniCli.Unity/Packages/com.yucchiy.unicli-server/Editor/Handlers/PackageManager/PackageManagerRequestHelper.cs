using System.Threading;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager.Requests;

namespace UniCli.Server.Editor.Handlers
{
    internal static class PackageManagerRequestHelper
    {
        public static Task WaitForCompletion(Request request, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();

            void Poll()
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    EditorApplication.update -= Poll;
                    tcs.TrySetCanceled(cancellationToken);
                    return;
                }

                if (!request.IsCompleted) return;

                EditorApplication.update -= Poll;
                tcs.TrySetResult(true);
            }

            EditorApplication.update += Poll;
            return tcs.Task;
        }
    }
}
