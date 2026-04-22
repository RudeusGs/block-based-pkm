using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Policies;

namespace Pkm.Application.Features.Documents.Commands.RenewBlockLease;

public sealed class RenewBlockLeaseHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IBlockEditLeaseService _blockEditLeaseService;

    public RenewBlockLeaseHandler(
        ICurrentUser currentUser,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IBlockEditLeaseService blockEditLeaseService)
    {
        _currentUser = currentUser;
        _documentAccessEvaluator = documentAccessEvaluator;
        _blockEditLeaseService = blockEditLeaseService;
    }

    public async Task<Result<BlockLeaseDto>> HandleAsync(
        RenewBlockLeaseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BlockId == Guid.Empty)
            return Result.Failure<BlockLeaseDto>(DocumentErrors.InvalidBlockId);

        if (string.IsNullOrWhiteSpace(request.EditorSessionId))
            return Result.Failure<BlockLeaseDto>(DocumentErrors.InvalidEditorSessionId);

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<BlockLeaseDto>(DocumentErrors.MissingUserContext);

        var access = await _documentAccessEvaluator.EvaluateByBlockAsync(
            request.BlockId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<BlockLeaseDto>(DocumentErrors.BlockNotFound);

        if (!access.CanAcquireLease)
            return Result.Failure<BlockLeaseDto>(DocumentErrors.BlockForbidden);

        var result = await _blockEditLeaseService.RenewAsync(
            request.BlockId,
            currentUserId,
            request.EditorSessionId,
            cancellationToken);

        var lease = result.Granted ? result.Lease : result.CurrentHolder;

        var dto = new BlockLeaseDto(
            BlockId: request.BlockId,
            PageId: access.PageId,
            Granted: result.Granted,
            Status: result.Granted ? "renewed" : "conflict",
            HolderUserId: lease?.UserId,
            HolderDisplayName: lease?.HolderDisplayName,
            ExpiresAtUtc: lease?.ExpiresAtUtc,
            IsHeldByCurrentUser: lease is not null &&
                                 lease.UserId == currentUserId &&
                                 string.Equals(lease.ConnectionId, request.EditorSessionId, StringComparison.Ordinal));

        return Result.Success(dto);
    }
}