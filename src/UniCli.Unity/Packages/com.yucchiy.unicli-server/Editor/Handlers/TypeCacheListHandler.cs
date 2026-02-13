using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class TypeCacheListHandler : CommandHandler<TypeCacheListRequest, TypeCacheListResponse>
    {
        public override string CommandName => "TypeCache.List";
        public override string Description => "List types derived from a base type or matching a pattern";

        protected override ValueTask<TypeCacheListResponse> ExecuteAsync(TypeCacheListRequest request)
        {
            if (string.IsNullOrEmpty(request.baseType))
            {
                throw new CommandFailedException(
                    "baseType is required",
                    new TypeCacheListResponse { types = Array.Empty<string>() });
            }

            var baseType = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .FirstOrDefault(t => t.FullName == request.baseType);

            if (baseType == null)
            {
                throw new CommandFailedException(
                    $"Type '{request.baseType}' not found",
                    new TypeCacheListResponse { types = Array.Empty<string>() });
            }

            var types = TypeCache.GetTypesDerivedFrom(baseType);
            var typeNames = types
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Select(t => t.FullName)
                .OrderBy(n => n)
                .ToArray();

            if (!string.IsNullOrEmpty(request.filter))
            {
                typeNames = typeNames
                    .Where(n => n.IndexOf(request.filter, StringComparison.OrdinalIgnoreCase) >= 0)
                    .ToArray();
            }

            return new ValueTask<TypeCacheListResponse>(new TypeCacheListResponse
            {
                types = typeNames,
                count = typeNames.Length
            });
        }
    }

    [Serializable]
    public class TypeCacheListRequest
    {
        public string baseType;
        public string filter;
    }

    [Serializable]
    public class TypeCacheListResponse
    {
        public string[] types;
        public int count;
    }
}
