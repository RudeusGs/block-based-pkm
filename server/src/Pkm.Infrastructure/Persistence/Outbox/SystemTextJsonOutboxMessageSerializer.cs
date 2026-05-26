using System.Text.Json;

namespace Pkm.Infrastructure.Persistence.Outbox;

internal sealed class SystemTextJsonOutboxMessageSerializer : IOutboxMessageSerializer
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public string Serialize(object payload)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return JsonSerializer.Serialize(payload, payload.GetType(), SerializerOptions);
    }
}
