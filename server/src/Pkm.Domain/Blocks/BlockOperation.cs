using Pkm.Domain.Common;

namespace Pkm.Domain.Blocks;

public sealed class BlockOperation : CreationAuditedEntity
{
    private const int MaxPayloadLength = 10000;
    private const int MaxNoteLength = 500;

    public Guid PageId { get; private set; }
    public Guid BlockId { get; private set; }
    public BlockOperationType OperationType { get; private set; }
    public Guid ActorId { get; private set; }
    public long BaseRevision { get; private set; }
    public long AppliedRevision { get; private set; }
    public string? PayloadJson { get; private set; }
    public string? Note { get; private set; }

    private BlockOperation() { }

    private BlockOperation(
        Guid id,
        Guid pageId,
        Guid blockId,
        BlockOperationType operationType,
        Guid actorId,
        long baseRevision,
        long appliedRevision,
        DateTimeOffset createdDate,
        string? payloadJson = null,
        string? note = null) : base(id, createdDate)
    {
        DomainGuard.AgainstEmpty(pageId, nameof(pageId));
        DomainGuard.AgainstEmpty(blockId, nameof(blockId));
        DomainGuard.AgainstEmpty(actorId, nameof(actorId));
        DomainGuard.AgainstNegative(baseRevision, nameof(baseRevision));
        DomainGuard.AgainstNonPositive(appliedRevision, nameof(appliedRevision));

        if (appliedRevision <= baseRevision)
            throw new DomainException("AppliedRevision phải lớn hơn BaseRevision.");

        PageId = pageId;
        BlockId = blockId;
        OperationType = operationType;
        ActorId = actorId;
        BaseRevision = baseRevision;
        AppliedRevision = appliedRevision;
        PayloadJson = TextRules.NormalizeOptional(payloadJson, MaxPayloadLength, nameof(PayloadJson));
        Note = TextRules.NormalizeOptional(note, MaxNoteLength, nameof(Note));
    }

    public static BlockOperation Create(
        Guid pageId,
        Guid blockId,
        BlockOperationType operationType,
        Guid actorId,
        long baseRevision,
        long appliedRevision,
        DateTimeOffset now,
        string? payloadJson = null,
        string? note = null)
    {
        return new BlockOperation(
            Guid.NewGuid(),
            pageId,
            blockId,
            operationType,
            actorId,
            baseRevision,
            appliedRevision,
            now,
            payloadJson,
            note);
    }
}