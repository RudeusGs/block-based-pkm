using Pkm.Domain.Common;

namespace Pkm.Domain.Workspaces;

/// <summary>
/// Workspace: đại diện cho một không gian làm việc trong hệ thống.
/// </summary>
public sealed class Workspace : EntityBase
{
    private const int MaxNameLength = 50;
    private const int MaxDescriptionLength = 500;

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }

    public Guid OwnerId { get; private set; }
    public Guid? LastModifiedBy { get; private set; }

    private Workspace() { }

    public Workspace(Guid id, string name, Guid ownerId, DateTimeOffset now, string? description = null)
        : base(id, now)
    {
        DomainGuard.AgainstEmpty(ownerId, nameof(ownerId));

        OwnerId = ownerId;
        Name = TextRules.NormalizeRequired(name, MaxNameLength, "Tên Workspace");
        Description = TextRules.NormalizeOptional(description, MaxDescriptionLength, nameof(Description));
    }

    public void UpdateInformation(string newName, string? newDescription, Guid actorId, DateTimeOffset now)
    {
        ThrowIfDeleted();
        DomainGuard.AgainstEmpty(actorId, "ActorId");

        Name = TextRules.NormalizeRequired(newName, MaxNameLength, "Tên Workspace");
        Description = TextRules.NormalizeOptional(newDescription, MaxDescriptionLength, nameof(Description));

        LastModifiedBy = actorId;
        Touch(now);
    }
}