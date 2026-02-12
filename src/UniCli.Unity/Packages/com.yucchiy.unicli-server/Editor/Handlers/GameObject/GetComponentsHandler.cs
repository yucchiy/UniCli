using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniCli.Protocol;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    public sealed class GetComponentsHandler : CommandHandler<GetComponentsRequest, GetComponentsResponse>
    {
        public override string CommandName => CommandNames.GameObject.GetComponents;
        public override string Description => "Get detailed component information for a GameObject";

        protected override ValueTask<GetComponentsResponse> ExecuteAsync(GetComponentsRequest request)
        {
            var go = GameObjectResolver.Resolve(request.instanceId, request.path);
            if (go == null)
            {
                throw new CommandFailedException(
                    $"GameObject not found (instanceId={request.instanceId}, path=\"{request.path}\")",
                    new GetComponentsResponse { components = Array.Empty<ComponentDetail>() });
            }

            var components = go.GetComponents<Component>();
            var details = new List<ComponentDetail>(components.Length);

            foreach (var component in components)
            {
                if (component == null) continue;

                var detail = new ComponentDetail
                {
                    instanceId = component.GetInstanceID(),
                    typeName = component.GetType().FullName,
                    enabled = component is not Behaviour behaviour || behaviour.enabled,
                    properties = ExtractProperties(component)
                };

                details.Add(detail);
            }

            return new ValueTask<GetComponentsResponse>(new GetComponentsResponse
            {
                components = details.ToArray()
            });
        }

        private static SerializedPropertyInfo[] ExtractProperties(Component component)
        {
            var result = new List<SerializedPropertyInfo>();

            using var serializedObject = new SerializedObject(component);
            var iterator = serializedObject.GetIterator();

            if (!iterator.NextVisible(true)) return Array.Empty<SerializedPropertyInfo>();

            do
            {
                result.Add(new SerializedPropertyInfo
                {
                    name = iterator.name,
                    type = iterator.propertyType.ToString(),
                    value = GetPropertyValueString(iterator)
                });
            } while (iterator.NextVisible(false));

            return result.ToArray();
        }

        private static string GetPropertyValueString(SerializedProperty property)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return property.intValue.ToString();
                case SerializedPropertyType.Boolean:
                    return property.boolValue.ToString().ToLowerInvariant();
                case SerializedPropertyType.Float:
                    return property.floatValue.ToString("G");
                case SerializedPropertyType.String:
                    return property.stringValue ?? "";
                case SerializedPropertyType.Enum:
                    return property.enumNames.Length > property.enumValueIndex && property.enumValueIndex >= 0
                        ? property.enumNames[property.enumValueIndex]
                        : property.enumValueIndex.ToString();
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue != null
                        ? property.objectReferenceValue.name
                        : "(null)";
                case SerializedPropertyType.Vector2:
                    return property.vector2Value.ToString();
                case SerializedPropertyType.Vector3:
                    return property.vector3Value.ToString();
                case SerializedPropertyType.Vector4:
                    return property.vector4Value.ToString();
                case SerializedPropertyType.Color:
                    return property.colorValue.ToString();
                case SerializedPropertyType.Rect:
                    return property.rectValue.ToString();
                case SerializedPropertyType.Bounds:
                    return property.boundsValue.ToString();
                case SerializedPropertyType.LayerMask:
                    return property.intValue.ToString();
                default:
                    return $"({property.propertyType})";
            }
        }
    }

    [Serializable]
    public class GetComponentsRequest
    {
        public int instanceId;
        public string path = "";
    }

    [Serializable]
    public class GetComponentsResponse
    {
        public ComponentDetail[] components;
    }

    [Serializable]
    public class ComponentDetail
    {
        public int instanceId;
        public string typeName;
        public bool enabled;
        public SerializedPropertyInfo[] properties;
    }

    [Serializable]
    public class SerializedPropertyInfo
    {
        public string name;
        public string type;
        public string value;
    }
}
