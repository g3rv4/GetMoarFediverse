using System.Text.Json.Serialization;

namespace GetMoarFediverse;

[JsonSerializable(typeof(Config.ConfigData))]
internal partial class JsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(Status[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class CamelCaseJsonContext : JsonSerializerContext
{
}