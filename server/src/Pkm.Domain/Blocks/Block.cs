using Pkm.Domain.Common;

namespace Pkm.Domain.Blocks;

public sealed class Block : EntityBase
{
    private const int MaxOrderKeyLength = 100;
    private const int MaxTextLength = 20000;
    private const int MaxPropsLength = 20000;

    public Guid PageId { get; private set; }
    public Guid? ParentBlockId { get; private set; }
    public BlockTypeCode Type { get; private set; } = null!;
    public string? TextContent { get; private set; }
    public string? PropsJson { get; private set; }
    public int SchemaVersion { get; private set; }
    public string OrderKey { get; private set; } = string.Empty;
    public Guid CreatedBy { get; private set; }
    public Guid? LastModifiedBy { get; private set; }

    private Block() { }

    public Block(
        Guid id,
        Guid pageId,
        BlockTypeCode type,
        string orderKey,
        Guid createdBy,
        DateTimeOffset now,
        string? textContent = null,
        string? propsJson = null,
        Guid? parentBlockId = null,
        int schemaVersion = 1) : base(id, now)
    {
        DomainGuard.AgainstEmpty(pageId, nameof(pageId));
        if (type is null)
            throw new DomainException("Type không hợp lệ.");
        DomainGuard.AgainstEmpty(createdBy, nameof(createdBy));
        DomainGuard.AgainstNonPositive(schemaVersion, nameof(schemaVersion));

        PageId = pageId;
        ParentBlockId = parentBlockId;
        Type = type;
        OrderKey = TextRules.NormalizeRequired(orderKey, MaxOrderKeyLength, nameof(OrderKey));
        CreatedBy = createdBy;
        TextContent = TextRules.NormalizeOptional(textContent, MaxTextLength, nameof(TextContent));
        PropsJson = TextRules.NormalizeOptional(propsJson, MaxPropsLength, nameof(PropsJson));
        SchemaVersion = schemaVersion;

        ValidateContentByType();
    }

    public void UpdateContent(string? textContent, string? propsJson, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable(actorId);

        TextContent = TextRules.NormalizeOptional(textContent, MaxTextLength, nameof(TextContent));
        PropsJson = TextRules.NormalizeOptional(propsJson, MaxPropsLength, nameof(PropsJson));

        ValidateContentByType();
        RegisterModification(actorId, now);
    }

    public void MoveTo(Guid? newParentBlockId, string newOrderKey, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable(actorId);

        if (newParentBlockId.HasValue && newParentBlockId == Id)
            throw new DomainException("Không thể chuyển block vào chính nó.");

        ParentBlockId = newParentBlockId;
        OrderKey = TextRules.NormalizeRequired(newOrderKey, MaxOrderKeyLength, nameof(OrderKey));

        RegisterModification(actorId, now);
    }

    public void ChangeType(BlockTypeCode newType, Guid actorId, DateTimeOffset now)
    {
        EnsureEditable(actorId);

        if (newType is null)
            throw new DomainException("Type không hợp lệ.");

        Type = newType;
        ValidateContentByType();

        RegisterModification(actorId, now);
    }

    private void EnsureEditable(Guid actorId)
    {
        ThrowIfDeleted();
        DomainGuard.AgainstEmpty(actorId, "ActorId");
    }

    private void RegisterModification(Guid actorId, DateTimeOffset now)
    {
        LastModifiedBy = actorId;
        Touch(now);
    }

    private void ValidateContentByType()
    {
        if (Type.RequiresProps() && string.IsNullOrWhiteSpace(PropsJson))
            throw new DomainException($"{Type} block yêu cầu PropsJson.");
    }
}