using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class GetMenuItemsHandler : CommandHandler<GetMenuItemsRequest, GetMenuItemsResponse>
    {
        public override string CommandName => CommandNames.Menu.List;
        public override string Description => "List available Unity Editor menu items with filtering";

        protected override ValueTask<GetMenuItemsResponse> ExecuteAsync(GetMenuItemsRequest request)
        {
            var methods = TypeCache.GetMethodsWithAttribute<MenuItem>();
            var items = new List<MenuItemInfo>();

            foreach (var method in methods)
            {
                var attr = method.GetCustomAttribute<MenuItem>();
                if (attr == null) continue;
                if (attr.validate) continue;

                var path = attr.menuItem;

                if (!string.IsNullOrEmpty(request.filterText))
                {
                    var match = request.filterType switch
                    {
                        "startswith" => path.StartsWith(request.filterText, StringComparison.OrdinalIgnoreCase),
                        "exact" => path.Equals(request.filterText, StringComparison.OrdinalIgnoreCase),
                        _ => path.Contains(request.filterText, StringComparison.OrdinalIgnoreCase)
                    };

                    if (!match) continue;
                }

                items.Add(new MenuItemInfo
                {
                    path = path,
                    priority = attr.priority,
                    methodName = method.Name,
                    typeName = method.DeclaringType?.FullName ?? ""
                });

                if (request.maxCount > 0 && items.Count >= request.maxCount)
                    break;
            }

            items.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));

            return new ValueTask<GetMenuItemsResponse>(new GetMenuItemsResponse
            {
                items = items.ToArray(),
                totalCount = methods.Count,
                filteredCount = items.Count
            });
        }
    }

    [Serializable]
    public class GetMenuItemsRequest
    {
        public string filterText = "";
        public string filterType = "contains";
        public int maxCount = 200;
    }

    [Serializable]
    public class GetMenuItemsResponse
    {
        public MenuItemInfo[] items;
        public int totalCount;
        public int filteredCount;
    }

    [Serializable]
    public class MenuItemInfo
    {
        public string path;
        public int priority;
        public string methodName;
        public string typeName;
    }
}
