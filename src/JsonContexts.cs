using System.Text.Json.Serialization;
using GetMoarFediverse.Configuration;
using GetMoarFediverse.Configuration.Unsafe;
using GetMoarFediverse.Responses;

namespace GetMoarFediverse;

[JsonSerializable(typeof(UnsafeConfig))]
internal partial class JsonContext : JsonSerializerContext
{
}

[JsonSerializable(typeof(TagResponse))]
[JsonSerializable(typeof(FollowedTag[]))]
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
internal partial class CamelCaseJsonContext : JsonSerializerContext
{
}