using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Features.Activity.Services;
using Pkm.Domain.Blocks;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Documents.Services;

public interface IDocumentMutationCoordinator
{
    bool TryGetCurrentUserId(out Guid currentUserId);

    string ActorDisplayName { get; }

    DateTimeOffset UtcNow { get; }

    void AddRevision(
        Guid pageId,
        long revision,
        Guid userId,
        DateTimeOffset createdAtUtc,
        string description);

    void AddBlockOperation(
        Guid pageId,
        Guid blockId,
        BlockOperationType operationType,
        Guid userId,
        long baseRevision,
        long appliedRevision,
        DateTimeOffset createdAtUtc,
        string payloadJson,
        string description);

    Task SaveChangesAsync(CancellationToken cancellationToken);

    Task RecordActivityAsync(ActivityLogRequest request, CancellationToken cancellationToken);

    Task PublishToPageAsync(DocumentRealtimeEnvelope envelope, CancellationToken cancellationToken);
}

public sealed class DocumentMutationCoordinator : IDocumentMutationCoordinator
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageRevisionRepository _pageRevisionRepository;
    private readonly IBlockOperationRepository _blockOperationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IDocumentRealtimePublisher _realtimePublisher;
    private readonly IActivityLogService _activityLogService;

    public DocumentMutationCoordinator(
        ICurrentUser currentUser,
        IPageRevisionRepository pageRevisionRepository,
        IBlockOperationRepository blockOperationRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IDocumentRealtimePublisher realtimePublisher,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _pageRevisionRepository = pageRevisionRepository;
        _blockOperationRepository = blockOperationRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _realtimePublisher = realtimePublisher;
        _activityLogService = activityLogService;
    }

    public string ActorDisplayName => _currentUser.UserName ?? "Có người";

    public DateTimeOffset UtcNow => _clock.UtcNow;

    public bool TryGetCurrentUserId(out Guid currentUserId)
        => _currentUser.TryGetUserId(out currentUserId);

    public void AddRevision(
        Guid pageId,
        long revision,
        Guid userId,
        DateTimeOffset createdAtUtc,
        string description)
        => _pageRevisionRepository.Add(PageRevision.Create(
            pageId,
            revision,
            userId,
            createdAtUtc,
            description));

    public void AddBlockOperation(
        Guid pageId,
        Guid blockId,
        BlockOperationType operationType,
        Guid userId,
        long baseRevision,
        long appliedRevision,
        DateTimeOffset createdAtUtc,
        string payloadJson,
        string description)
        => _blockOperationRepository.Add(BlockOperation.Create(
            pageId,
            blockId,
            operationType,
            userId,
            baseRevision,
            appliedRevision,
            createdAtUtc,
            payloadJson,
            description));

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _unitOfWork.SaveChangesAsync(cancellationToken);

    public Task RecordActivityAsync(ActivityLogRequest request, CancellationToken cancellationToken)
        => _activityLogService.RecordAsync(request, cancellationToken);

    public Task PublishToPageAsync(DocumentRealtimeEnvelope envelope, CancellationToken cancellationToken)
        => _realtimePublisher.PublishToPageAsync(envelope, cancellationToken);
}
