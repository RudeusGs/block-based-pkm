using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Policies;

namespace Pkm.Application.Features.Documents.Queries.GetBlockLease;

public sealed class GetBlockLeaseHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IBlockEditLeaseService _blockEditLeaseService;

    public GetBlockLeaseHandler(
        ICurrentUser currentUser,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IBlockEditLeaseService blockEditLeaseService)
    {
        _currentUser = currentUser;
        _documentAccessEvaluator = documentAccessEvaluator;
        _blockEditLeaseService = blockEditLeaseService;
    }

    public async Task<Result<BlockLeaseDto>> HandleAsync(
        GetBlockLeaseQuery request,
        CancellationToken cancellationToken)
    {
        if (request.BlockId == Guid.Empty)
            return Result.Failure<BlockLeaseDto>(DocumentErrors.InvalidBlockId);

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<BlockLeaseDto>(DocumentErrors.MissingUserContext);

        var access = await _documentAccessEvaluator.EvaluateByBlockAsync(
            request.BlockId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<BlockLeaseDto>(DocumentErrors.BlockNotFound);

        if (!access.CanRead)
            return Result.Failure<BlockLeaseDto>(DocumentErrors.BlockForbidden);

        var lease = await _blockEditLeaseService.GetCurrentAsync(
            request.BlockId,
            cancellationToken);

        if (lease is null)
        {
            return Result.Success(new BlockLeaseDto(
                BlockId: request.BlockId,
                PageId: access.PageId,
                Granted: false,
                Status: "not_held",
                HolderUserId: null,
                HolderDisplayName: null,
                ExpiresAtUtc: null,
                IsHeldByCurrentUser: false));
        }

        return Result.Success(new BlockLeaseDto(
            BlockId: request.BlockId,
            PageId: access.PageId,
            Granted: lease.UserId == currentUserId,
            Status: lease.UserId == currentUserId ? "held_by_current_user" : "held_by_other_user",
            HolderUserId: lease.UserId,
            HolderDisplayName: lease.HolderDisplayName,
            ExpiresAtUtc: lease.ExpiresAtUtc,
            IsHeldByCurrentUser: lease.UserId == currentUserId));
    }
}