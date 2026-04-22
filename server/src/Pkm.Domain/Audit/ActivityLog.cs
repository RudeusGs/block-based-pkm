using Pkm.Domain.Common;

namespace Pkm.Domain.Audit;

/// <summary>
/// ActivityLog: bản ghi audit append-only cho một hành động trong hệ thống.
/// Không hỗ trợ update / delete / restore.
/// </summary>
public sealed class ActivityLog : CreationAuditedEntity
{
    private const int MaxDescriptionLength = 500;
    private const int MaxMetadataLength = 10000;
    private const int MaxIpAddressLength = 100;

    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }

    public ActivityAction Action { get; private set; }
    public ActivityEntityType EntityType { get; private set; }

    public Guid EntityId { get; private set; }

    public string? Description { get; private set; }
    public string? MetadataJson { get; private set; }
    public string? IpAddress { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    private ActivityLog() { }

    private ActivityLog(
        Guid id,
        Guid workspaceId,
        Guid userId,
        ActivityAction action,
        ActivityEntityType entityType,
        Guid entityId,
        DateTimeOffset occurredAt,
        string? description = null,
        string? metadataJson = null,
        string? ipAddress = null) : base(id, occurredAt)
    {
        DomainGuard.AgainstEmpty(workspaceId, nameof(workspaceId));
        DomainGuard.AgainstEmpty(userId, nameof(userId));
        DomainGuard.AgainstEmpty(entityId, nameof(entityId));

        WorkspaceId = workspaceId;
        UserId = userId;
        Action = action;
        EntityType = entityType;
        EntityId = entityId;

        Description = TextRules.NormalizeOptional(description, MaxDescriptionLength, nameof(Description));
        MetadataJson = TextRules.NormalizeOptional(metadataJson, MaxMetadataLength, nameof(MetadataJson));
        IpAddress = TextRules.NormalizeOptional(ipAddress, MaxIpAddressLength, nameof(IpAddress));

        OccurredAt = occurredAt;
    }

    public static ActivityLog Create(
        Guid workspaceId,
        Guid userId,
        ActivityAction action,
        ActivityEntityType entityType,
        Guid entityId,
        DateTimeOffset occurredAt,
        string? description = null,
        string? metadataJson = null,
        string? ipAddress = null)
    {
        return new ActivityLog(
            Guid.NewGuid(),
            workspaceId,
            userId,
            action,
            entityType,
            entityId,
            occurredAt,
            description,
            metadataJson,
            ipAddress);
    }
}