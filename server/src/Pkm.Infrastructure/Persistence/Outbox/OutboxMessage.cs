namespace Pkm.Infrastructure.Persistence.Outbox;

internal sealed class OutboxMessage
{
    private OutboxMessage() { }

    private OutboxMessage(
        Guid id,
        string eventType,
        string source,
        string schemaVersion,
        string payloadJson,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset createdAtUtc,
        string? traceId,
        Guid? correlationId)
    {
        Id = id;
        EventType = eventType;
        Source = source;
        SchemaVersion = schemaVersion;
        PayloadJson = payloadJson;
        OccurredAtUtc = occurredAtUtc;
        CreatedAtUtc = createdAtUtc;
        TraceId = traceId;
        CorrelationId = correlationId;
        Status = OutboxMessageStatus.Pending;
    }

    public Guid Id { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public string Source { get; private set; } = string.Empty;
    public string SchemaVersion { get; private set; } = string.Empty;
    public string PayloadJson { get; private set; } = string.Empty;
    public string? TraceId { get; private set; }
    public Guid? CorrelationId { get; private set; }
    public DateTimeOffset OccurredAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public OutboxMessageStatus Status { get; private set; }
    public int RetryCount { get; private set; }
    public string? LastError { get; private set; }
    public string? LockId { get; private set; }
    public DateTimeOffset? LockedUntilUtc { get; private set; }
    public DateTimeOffset? ProcessedAtUtc { get; private set; }

    public static OutboxMessage Create(
        string eventType,
        string source,
        string schemaVersion,
        string payloadJson,
        DateTimeOffset occurredAtUtc,
        DateTimeOffset createdAtUtc,
        string? traceId = null,
        Guid? correlationId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(payloadJson);

        return new OutboxMessage(
            Guid.NewGuid(),
            eventType.Trim(),
            source.Trim(),
            schemaVersion.Trim(),
            payloadJson,
            occurredAtUtc,
            createdAtUtc,
            string.IsNullOrWhiteSpace(traceId) ? null : traceId.Trim(),
            correlationId);
    }
}
