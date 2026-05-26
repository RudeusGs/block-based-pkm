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

    public object Deserialize(string payloadJson, Type payloadType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);
        ArgumentNullException.ThrowIfNull(payloadType);

        return JsonSerializer.Deserialize(payloadJson, payloadType, SerializerOptions)
            ?? throw new InvalidOperationException(
                $"Outbox payload could not be deserialized as '{payloadType.FullName}'.");
    }
}
