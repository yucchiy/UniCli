using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.PackageManager.Requests;

namespace UniCli.Server.Editor.Handlers
{
    internal static class PackageManagerRequestHelper
    {
        public static Task WaitForCompletion(Request request)
        {
            var tcs = new TaskCompletionSource<bool>();

            void Poll()
            {
                if (!request.IsCompleted) return;

                EditorApplication.update -= Poll;
                tcs.TrySetResult(true);
            }

            EditorApplication.update += Poll;
            return tcs.Task;
        }
    }
}
