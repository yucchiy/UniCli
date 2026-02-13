using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UniCli.Protocol;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class TypeInspectHandler : CommandHandler<TypeInspectRequest, TypeInspectResponse>
    {
        public override string CommandName => "TypeInspect";
        public override string Description => "Inspect nested types of a given type";

        protected override ValueTask<TypeInspectResponse> ExecuteAsync(TypeInspectRequest request)
        {
            var typeName = string.IsNullOrEmpty(request.typeName)
                ? "UnityEditor.PlayerSettings"
                : request.typeName;

            var type = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .FirstOrDefault(t => t.FullName == typeName);

            if (type == null)
            {
                throw new CommandFailedException(
                    $"Type '{typeName}' not found",
                    new TypeInspectResponse
                    {
                        nestedTypes = Array.Empty<TypeInspectNestedInfo>()
                    });
            }

            var nested = type.GetNestedTypes(BindingFlags.Public | BindingFlags.Static)
                .Select(nt => new TypeInspectNestedInfo
                {
                    name = nt.Name,
                    fullName = nt.FullName,
                    isStatic = nt.IsAbstract && nt.IsSealed,
                    isPublic = nt.IsPublic || nt.IsNestedPublic,
                    memberCount = nt.GetMembers(BindingFlags.Public | BindingFlags.Static).Length
                })
                .OrderBy(nt => nt.name)
                .ToArray();

            return new ValueTask<TypeInspectResponse>(new TypeInspectResponse
            {
                typeName = type.FullName,
                nestedTypes = nested,
                count = nested.Length
            });
        }
    }

    [Serializable]
    public class TypeInspectRequest
    {
        public string typeName;
    }

    [Serializable]
    public class TypeInspectResponse
    {
        public string typeName;
        public TypeInspectNestedInfo[] nestedTypes;
        public int count;
    }

    [Serializable]
    public class TypeInspectNestedInfo
    {
        public string name;
        public string fullName;
        public bool isStatic;
        public bool isPublic;
        public int memberCount;
    }
}
