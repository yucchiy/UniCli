using Microsoft.CodeAnalysis;

namespace UniCli.SourceGenerator.Analysis
{
    internal static class TypeSerializabilityChecker
    {
        private static readonly string[] SupportedPrimitiveTypes =
        {
            "System.String",
            "System.Boolean",
            "System.Int32",
            "System.Int64",
            "System.Single",
            "System.Double",
            "System.Byte",
            "System.SByte",
            "System.Int16",
            "System.UInt16",
            "System.UInt32",
            "System.UInt64",
            "System.Char",
        };

        private static readonly string[] SupportedUnityValueTypes =
        {
            "UnityEngine.Vector2",
            "UnityEngine.Vector3",
            "UnityEngine.Vector4",
            "UnityEngine.Vector2Int",
            "UnityEngine.Vector3Int",
            "UnityEngine.Color",
            "UnityEngine.Color32",
            "UnityEngine.Rect",
            "UnityEngine.RectInt",
            "UnityEngine.Bounds",
            "UnityEngine.BoundsInt",
            "UnityEngine.Quaternion",
        };

        public static bool IsSerializableType(ITypeSymbol type)
        {
            if (type == null)
                return false;

            if (type is INamedTypeSymbol namedType)
            {
                if (namedType.IsGenericType)
                    return false;

                if (IsNullableType(namedType))
                    return false;
            }

            if (IsPrimitiveType(type))
                return true;

            if (type.TypeKind == TypeKind.Enum)
                return true;

            if (IsUnityValueType(type))
                return true;

            return false;
        }

        public static bool IsPrimitiveType(ITypeSymbol type)
        {
            var fullName = GetFullMetadataName(type);
            foreach (var primitive in SupportedPrimitiveTypes)
            {
                if (fullName == primitive)
                    return true;
            }
            return false;
        }

        public static bool IsEnumType(ITypeSymbol type)
        {
            return type.TypeKind == TypeKind.Enum;
        }

        public static bool IsUnityValueType(ITypeSymbol type)
        {
            var fullName = GetFullMetadataName(type);
            foreach (var unityType in SupportedUnityValueTypes)
            {
                if (fullName == unityType)
                    return true;
            }
            return false;
        }

        public static bool IsUnityObjectType(ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                if (GetFullMetadataName(current) == "UnityEngine.Object")
                    return true;
                current = current.BaseType;
            }
            return false;
        }

        public static bool IsNullableType(INamedTypeSymbol type)
        {
            return type.IsGenericType &&
                   GetFullMetadataName(type.OriginalDefinition) == "System.Nullable`1";
        }

        public static bool IsDelegateType(ITypeSymbol type)
        {
            var current = type;
            while (current != null)
            {
                if (GetFullMetadataName(current) == "System.Delegate" ||
                    GetFullMetadataName(current) == "System.MulticastDelegate")
                    return true;
                current = current.BaseType;
            }
            return type.TypeKind == TypeKind.Delegate;
        }

        public static string GetFullMetadataName(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol)
                return type.ToDisplayString();

            var parts = new System.Collections.Generic.List<string>();
            var current = type;

            while (current != null)
            {
                if (current is INamedTypeSymbol named && named.Arity > 0)
                    parts.Insert(0, $"{current.Name}`{named.Arity}");
                else
                    parts.Insert(0, current.Name);

                if (current.ContainingType != null)
                {
                    current = current.ContainingType;
                }
                else
                {
                    break;
                }
            }

            var ns = type.ContainingNamespace;
            if (ns != null && !ns.IsGlobalNamespace)
            {
                return $"{ns.ToDisplayString()}.{string.Join(".", parts)}";
            }

            return string.Join(".", parts);
        }
    }
}
