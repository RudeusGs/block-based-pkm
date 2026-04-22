using System.Text.Json;

namespace Pkm.Infrastructure.Cache;

internal sealed class SystemTextJsonRedisSerializer : IRedisSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public string Serialize<T>(T value)
        => JsonSerializer.Serialize(value, Options);

    public T? Deserialize<T>(string value)
        => JsonSerializer.Deserialize<T>(value, Options);
}