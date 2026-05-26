using AppDocuments = Pkm.Application.Features.Documents.Models;

namespace Pkm.Api.Contracts.Responses.Blocks;

public static class BlockResponseMappings
{
    public static BlockResponse ToResponse(this AppDocuments.BlockDto dto)
        => new(
            dto.Id,
            dto.PageId,
            dto.ParentBlockId,
            dto.Type,
            dto.TextContent,
            dto.PropsJson,
            dto.SchemaVersion,
            dto.OrderKey,
            dto.CreatedBy,
            dto.LastModifiedBy,
            dto.CreatedDate,
            dto.UpdatedDate);

    public static BlockLeaseResponse ToResponse(this AppDocuments.BlockLeaseDto dto)
        => new(
            dto.BlockId,
            dto.PageId,
            dto.Granted,
            dto.Status,
            dto.HolderUserId,
            dto.HolderDisplayName,
            dto.ExpiresAtUtc,
            dto.IsHeldByCurrentUser);

    public static BlockMutationResponse ToResponse(this AppDocuments.BlockMutationDto dto)
        => new(
            dto.PageId,
            dto.BlockId,
            dto.AppliedRevision,
            dto.Block?.ToResponse());

    public static PageDocumentResponse ToResponse(this AppDocuments.PageDocumentDto dto)
        => new(
            dto.PageId,
            dto.CurrentRevision,
            dto.Blocks.Select(x => x.ToResponse()).ToArray(),
            dto.PageNumber,
            dto.PageSize,
            dto.TotalCount,
            dto.TotalPages);
}
