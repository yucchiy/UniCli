using System.Text.Json;
using System.Text.Json.Serialization;
using UniCli.Protocol;

namespace UniCli.Client;

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.Unspecified,
    IncludeFields = true,
    WriteIndented = true)]
[JsonSerializable(typeof(CommandRequest))]
[JsonSerializable(typeof(CommandResponse))]
[JsonSerializable(typeof(CommandListResponse))]
[JsonSerializable(typeof(CommandInfo[]))]
[JsonSerializable(typeof(JsonElement))]
internal partial class ProtocolJsonContext : JsonSerializerContext
{
}
