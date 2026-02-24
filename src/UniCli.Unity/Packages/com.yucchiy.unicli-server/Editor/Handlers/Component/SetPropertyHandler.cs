using System.Threading;
using System;
using System.Globalization;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace UniCli.Server.Editor.Handlers
{
    [Module("GameObject")]
    public sealed class SetPropertyHandler : CommandHandler<SetPropertyRequest, SetPropertyResponse>
    {
        public override string CommandName => "Component.SetProperty";
        public override string Description => "Set a component property value via SerializedProperty";

        protected override bool TryWriteFormatted(SetPropertyResponse response, bool success, IFormatWriter writer)
        {
            if (success)
                writer.WriteLine($"Set {response.propertyPath} = {response.currentValue} (was {response.previousValue})");
            else
                writer.WriteLine("Failed to set property");

            return true;
        }

        protected override ValueTask<SetPropertyResponse> ExecuteAsync(SetPropertyRequest request, CancellationToken cancellationToken)
        {
            if (request.componentInstanceId == 0)
                throw new ArgumentException("componentInstanceId is required");
            if (string.IsNullOrEmpty(request.propertyPath))
                throw new ArgumentException("propertyPath is required");

            var obj = EditorUtility.InstanceIDToObject(request.componentInstanceId);
            if (obj is not UnityEngine.Component component)
            {
                throw new CommandFailedException(
                    $"Component not found for instanceId={request.componentInstanceId}",
                    new SetPropertyResponse());
            }

            using var serializedObject = new SerializedObject(component);
            var property = serializedObject.FindProperty(request.propertyPath);
            if (property == null)
            {
                throw new CommandFailedException(
                    $"Property \"{request.propertyPath}\" not found on {component.GetType().FullName}",
                    new SetPropertyResponse());
            }

            var previousValue = GetPropertyValueString(property);
            SetPropertyValue(property, request.value);
            serializedObject.ApplyModifiedProperties();

            var verifyProperty = serializedObject.FindProperty(request.propertyPath);
            var currentValue = GetPropertyValueString(verifyProperty);

            return new ValueTask<SetPropertyResponse>(new SetPropertyResponse
            {
                componentInstanceId = request.componentInstanceId,
                propertyPath = request.propertyPath,
                previousValue = previousValue,
                currentValue = currentValue
            });
        }

        private static void SetPropertyValue(SerializedProperty property, string value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = int.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = float.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = bool.Parse(value);
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = value;
                    break;
                case SerializedPropertyType.Enum:
                    if (int.TryParse(value, out var enumIndex))
                    {
                        property.enumValueIndex = enumIndex;
                    }
                    else
                    {
                        var index = Array.IndexOf(property.enumNames, value);
                        if (index < 0)
                            throw new CommandFailedException(
                                $"Invalid enum value \"{value}\". Valid values: {string.Join(", ", property.enumNames)}",
                                new SetPropertyResponse());
                        property.enumValueIndex = index;
                    }
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = ParseVector2(value);
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = ParseVector3(value);
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = ParseVector4(value);
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = ParseColor(value);
                    break;
                case SerializedPropertyType.Rect:
                    property.rectValue = ParseRect(value);
                    break;
                case SerializedPropertyType.Bounds:
                    property.boundsValue = ParseBounds(value);
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = ParseQuaternion(value);
                    break;
                case SerializedPropertyType.LayerMask:
                    property.intValue = int.Parse(value, CultureInfo.InvariantCulture);
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = ResolveObjectReference(value);
                    break;
                default:
                    throw new CommandFailedException(
                        $"Unsupported property type: {property.propertyType}",
                        new SetPropertyResponse());
            }
        }

        private static Vector2 ParseVector2(string value)
        {
            var parts = value.Split(',');
            if (parts.Length != 2)
                throw new CommandFailedException("Vector2 requires 2 comma-separated values (e.g. \"1.0,2.0\")", new SetPropertyResponse());
            return new Vector2(
                float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture));
        }

        private static Vector3 ParseVector3(string value)
        {
            var parts = value.Split(',');
            if (parts.Length != 3)
                throw new CommandFailedException("Vector3 requires 3 comma-separated values (e.g. \"1.0,2.0,3.0\")", new SetPropertyResponse());
            return new Vector3(
                float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture));
        }

        private static Vector4 ParseVector4(string value)
        {
            var parts = value.Split(',');
            if (parts.Length != 4)
                throw new CommandFailedException("Vector4 requires 4 comma-separated values (e.g. \"1.0,2.0,3.0,4.0\")", new SetPropertyResponse());
            return new Vector4(
                float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture));
        }

        private static Rect ParseRect(string value)
        {
            var parts = value.Split(',');
            if (parts.Length != 4)
                throw new CommandFailedException("Rect requires 4 comma-separated values (e.g. \"x,y,width,height\")", new SetPropertyResponse());
            return new Rect(
                float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture));
        }

        private static Bounds ParseBounds(string value)
        {
            var parts = value.Split(',');
            if (parts.Length != 6)
                throw new CommandFailedException("Bounds requires 6 comma-separated values (e.g. \"cx,cy,cz,sx,sy,sz\")", new SetPropertyResponse());
            return new Bounds(
                new Vector3(
                    float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                    float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                    float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture)),
                new Vector3(
                    float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture),
                    float.Parse(parts[4].Trim(), CultureInfo.InvariantCulture),
                    float.Parse(parts[5].Trim(), CultureInfo.InvariantCulture)));
        }

        private static Quaternion ParseQuaternion(string value)
        {
            var parts = value.Split(',');
            if (parts.Length != 4)
                throw new CommandFailedException("Quaternion requires 4 comma-separated values (e.g. \"x,y,z,w\")", new SetPropertyResponse());
            return new Quaternion(
                float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture));
        }

        private static Color ParseColor(string value)
        {
            var parts = value.Split(',');
            if (parts.Length < 3 || parts.Length > 4)
                throw new CommandFailedException("Color requires 3-4 comma-separated values (e.g. \"1.0,0.5,0.0\" or \"1.0,0.5,0.0,1.0\")", new SetPropertyResponse());
            return new Color(
                float.Parse(parts[0].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[1].Trim(), CultureInfo.InvariantCulture),
                float.Parse(parts[2].Trim(), CultureInfo.InvariantCulture),
                parts.Length == 4 ? float.Parse(parts[3].Trim(), CultureInfo.InvariantCulture) : 1f);
        }

        private static UnityEngine.Object ResolveObjectReference(string value)
        {
            if (string.IsNullOrEmpty(value) || value == "null")
                return null;

            if (value.StartsWith("guid:"))
            {
                var guid = value.Substring("guid:".Length);
                var assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath))
                    throw new CommandFailedException(
                        $"Asset not found for GUID: {guid}",
                        new SetPropertyResponse());
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(assetPath);
                if (asset == null)
                    throw new CommandFailedException(
                        $"Failed to load asset at: {assetPath}",
                        new SetPropertyResponse());
                return asset;
            }

            if (value.StartsWith("instanceId:"))
            {
                var idStr = value.Substring("instanceId:".Length);
                if (!int.TryParse(idStr, out var instanceId))
                    throw new CommandFailedException(
                        $"Invalid instanceId: {idStr}",
                        new SetPropertyResponse());
                var obj = EditorUtility.InstanceIDToObject(instanceId);
                if (obj == null)
                    throw new CommandFailedException(
                        $"Object not found for instanceId: {instanceId}",
                        new SetPropertyResponse());
                return obj;
            }

            // Try as asset path
            var assetByPath = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(value);
            if (assetByPath != null)
                return assetByPath;

            throw new CommandFailedException(
                $"Cannot resolve ObjectReference from \"{value}\". Use \"guid:<GUID>\", \"instanceId:<ID>\", an asset path, or \"null\".",
                new SetPropertyResponse());
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
                case SerializedPropertyType.Quaternion:
                    return property.quaternionValue.ToString();
                case SerializedPropertyType.LayerMask:
                    return property.intValue.ToString();
                default:
                    return $"({property.propertyType})";
            }
        }
    }

    [Serializable]
    public class SetPropertyRequest
    {
        public int componentInstanceId;
        public string propertyPath = "";
        public string value = "";
    }

    [Serializable]
    public class SetPropertyResponse
    {
        public int componentInstanceId;
        public string propertyPath;
        public string previousValue;
        public string currentValue;
    }
}
