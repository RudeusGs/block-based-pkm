namespace Pkm.Application.Features.Documents.Policies;

public interface IDocumentAccessEvaluator
{
    Task<DocumentBlockAccessResult> EvaluateByBlockAsync(
        Guid blockId,
        Guid userId,
        CancellationToken cancellationToken = default);
}

public sealed record DocumentBlockAccessResult(
    bool Exists,
    Guid WorkspaceId,
    Guid PageId,
    bool IsPageArchived,
    bool CanReadDocument,
    bool CanEditDocument,
    bool CanReorderBlocks,
    bool CanDeleteBlocks,
    bool CanAcquireLease,
    bool CanManagePage)
{
    public bool CanRead => CanReadDocument;
    public bool CanWrite => CanEditDocument;
}