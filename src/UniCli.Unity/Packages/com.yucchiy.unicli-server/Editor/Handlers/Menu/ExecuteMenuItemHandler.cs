using System;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class ExecuteMenuItemHandler : CommandHandler<ExecuteMenuItemRequest, ExecuteMenuItemResponse>
    {
        public override string CommandName => CommandNames.Menu.Execute;
        public override string Description => "Execute a Unity Editor menu item by path";

        protected override ValueTask<ExecuteMenuItemResponse> ExecuteAsync(ExecuteMenuItemRequest request)
        {
            if (string.IsNullOrEmpty(request.menuItemPath))
            {
                throw new ArgumentException("menuItemPath is required");
            }

            var executed = EditorApplication.ExecuteMenuItem(request.menuItemPath);

            return new ValueTask<ExecuteMenuItemResponse>(new ExecuteMenuItemResponse
            {
                executed = executed,
                menuItemPath = request.menuItemPath
            });
        }
    }

    [Serializable]
    public class ExecuteMenuItemRequest
    {
        public string menuItemPath;
    }

    [Serializable]
    public class ExecuteMenuItemResponse
    {
        public bool executed;
        public string menuItemPath;
    }
}
