namespace Pkm.Application.Common.Abstractions.Messaging;

public sealed class IntegrationEventEnvelope
{
    public const string DefaultSource = "pkm-api";
    public const string DefaultSchemaVersion = "1";

    public IntegrationEventEnvelope(
        string type,
        object payload,
        DateTimeOffset occurredAtUtc,
        string source = DefaultSource,
        string schemaVersion = DefaultSchemaVersion,
        string? traceId = null,
        Guid? correlationId = null)
    {
        if (string.IsNullOrWhiteSpace(type))
            throw new ArgumentException("Integration event type is required.", nameof(type));

        if (string.IsNullOrWhiteSpace(source))
            throw new ArgumentException("Integration event source is required.", nameof(source));

        if (string.IsNullOrWhiteSpace(schemaVersion))
            throw new ArgumentException("Integration event schema version is required.", nameof(schemaVersion));

        ArgumentNullException.ThrowIfNull(payload);

        Type = type.Trim();
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
        Source = source.Trim();
        SchemaVersion = schemaVersion.Trim();
        TraceId = string.IsNullOrWhiteSpace(traceId) ? null : traceId.Trim();
        CorrelationId = correlationId;
    }

    public string Type { get; }
    public object Payload { get; }
    public DateTimeOffset OccurredAtUtc { get; }
    public string Source { get; }
    public string SchemaVersion { get; }
    public string? TraceId { get; }
    public Guid? CorrelationId { get; }
}
