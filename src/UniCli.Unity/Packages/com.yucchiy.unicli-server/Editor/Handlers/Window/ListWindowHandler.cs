using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ListWindowHandler : CommandHandler<Unit, ListWindowResponse>
    {
        public override string CommandName => "Window.List";
        public override string Description => "List all available EditorWindow types";

        protected override bool TryWriteFormatted(ListWindowResponse response, bool success, IFormatWriter writer)
        {
            if (!success || response.windows == null || response.windows.Length == 0)
            {
                writer.WriteLine("No EditorWindow types found.");
                return true;
            }

            writer.WriteLine($"Available EditorWindows ({response.windows.Length}):");
            foreach (var w in response.windows)
            {
                writer.WriteLine($"  {w.typeName}");
            }

            return true;
        }

        protected override ValueTask<ListWindowResponse> ExecuteAsync(Unit request, CancellationToken cancellationToken)
        {
            var types = WindowResolver.GetAllWindowTypes();
            var windows = types
                .Select(t => new WindowInfo { typeName = t.FullName })
                .ToArray();

            return new ValueTask<ListWindowResponse>(new ListWindowResponse { windows = windows });
        }
    }

    [Serializable]
    public class ListWindowResponse
    {
        public WindowInfo[] windows;
    }

    [Serializable]
    public class WindowInfo
    {
        public string typeName;
    }
}
