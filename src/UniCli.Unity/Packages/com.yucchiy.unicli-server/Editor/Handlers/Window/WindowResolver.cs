using System;
using System.Linq;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    internal static class WindowResolver
    {
        public static Type[] GetAllWindowTypes()
        {
            return TypeCache.GetTypesDerivedFrom<EditorWindow>()
                .Where(t => !t.IsAbstract)
                .OrderBy(t => t.FullName)
                .ToArray();
        }

        public static Type FindWindowType(string typeName)
        {
            var types = GetAllWindowTypes();

            // 完全修飾名の完全一致
            var exact = types.FirstOrDefault(t => t.FullName == typeName);
            if (exact != null) return exact;

            // 短縮名の完全一致
            var shortMatch = types.FirstOrDefault(t => t.Name == typeName);
            if (shortMatch != null) return shortMatch;

            // 末尾一致（例: "Inspector" → "InspectorWindow"）
            var suffix = types.FirstOrDefault(t =>
                t.Name.StartsWith(typeName, StringComparison.OrdinalIgnoreCase) ||
                t.FullName.EndsWith("." + typeName, StringComparison.OrdinalIgnoreCase));
            return suffix;
        }
    }
}
