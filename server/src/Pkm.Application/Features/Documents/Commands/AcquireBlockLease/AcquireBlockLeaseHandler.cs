using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Policies;

namespace Pkm.Application.Features.Documents.Commands.AcquireBlockLease;

public sealed class AcquireBlockLeaseHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IBlockEditLeaseService _blockEditLeaseService;
    private readonly IDocumentRealtimePublisher _realtimePublisher;
    private readonly IClock _clock;

    public AcquireBlockLeaseHandler(
        ICurrentUser currentUser,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IBlockEditLeaseService blockEditLeaseService,
        IDocumentRealtimePublisher realtimePublisher,
        IClock clock)
    {
        _currentUser = currentUser;
        _documentAccessEvaluator = documentAccessEvaluator;
        _blockEditLeaseService = blockEditLeaseService;
        _realtimePublisher = realtimePublisher;
        _clock = clock;
    }

    public async Task<Result<BlockLeaseDto>> HandleAsync(
        AcquireBlockLeaseCommand request,
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

        var holderDisplayName = string.IsNullOrWhiteSpace(request.HolderDisplayName)
            ? _currentUser.UserName
            : request.HolderDisplayName;

        var result = await _blockEditLeaseService.AcquireAsync(
            request.BlockId,
            access.PageId,
            currentUserId,
            request.EditorSessionId,
            holderDisplayName,
            cancellationToken);

        var lease = result.Granted ? result.Lease : result.CurrentHolder;

        var dto = new BlockLeaseDto(
            BlockId: request.BlockId,
            PageId: access.PageId,
            Granted: result.Granted,
            Status: result.Granted ? "acquired" : "conflict",
            HolderUserId: lease?.UserId,
            HolderDisplayName: lease?.HolderDisplayName,
            ExpiresAtUtc: lease?.ExpiresAtUtc,
            IsHeldByCurrentUser: lease is not null &&
                                 lease.UserId == currentUserId &&
                                 string.Equals(lease.ConnectionId, request.EditorSessionId, StringComparison.Ordinal));

        if (result.Granted)
        {
            await _realtimePublisher.PublishToPageAsync(
                new DocumentRealtimeEnvelope(
                    EventName: "BlockLeaseChanged",
                    WorkspaceId: access.WorkspaceId,
                    PageId: access.PageId,
                    BlockId: request.BlockId,
                    ActorId: currentUserId,
                    OccurredAtUtc: _clock.UtcNow,
                    Revision: null,
                    Payload: dto),
                cancellationToken);
        }

        return Result.Success(dto);
    }
}