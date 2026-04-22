using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Policies;
using Pkm.Application.Features.Documents.Services;

namespace Pkm.Application.Features.Documents.Commands.ReleaseBlockLease;

public sealed class ReleaseBlockLeaseHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IBlockEditLeaseService _blockEditLeaseService;
    private readonly IDocumentRealtimePublisher _realtimePublisher;
    private readonly IClock _clock;

    public ReleaseBlockLeaseHandler(
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
        ReleaseBlockLeaseCommand request,
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

        var leaseBeforeRelease = await _blockEditLeaseService.GetCurrentAsync(
            request.BlockId,
            cancellationToken);

        if (leaseBeforeRelease is not null)
        {
            var leaseError = BlockLeaseGuard.ValidateWriteLease(
                leaseBeforeRelease,
                currentUserId,
                request.EditorSessionId);

            if (leaseError is not null)
                return Result.Failure<BlockLeaseDto>(leaseError);
        }

        await _blockEditLeaseService.ReleaseAsync(
            request.BlockId,
            currentUserId,
            request.EditorSessionId,
            cancellationToken);

        var dto = new BlockLeaseDto(
            BlockId: request.BlockId,
            PageId: access.PageId,
            Granted: true,
            Status: "released",
            HolderUserId: null,
            HolderDisplayName: null,
            ExpiresAtUtc: null,
            IsHeldByCurrentUser: false);

        if (leaseBeforeRelease is not null)
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