using System.Text.Json;

namespace Pkm.Application.Features.Activity.Services;

public static class ActivityLogMetadata
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static string Serialize(object value)
        => JsonSerializer.Serialize(value, JsonOptions);
}
