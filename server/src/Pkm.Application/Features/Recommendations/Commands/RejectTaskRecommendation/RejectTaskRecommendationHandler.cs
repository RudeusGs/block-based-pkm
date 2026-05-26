using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Recommendations.Models;
using Pkm.Domain.Audit;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Recommendations;

namespace Pkm.Application.Features.Recommendations.Commands.RejectTaskRecommendation;

public sealed class RejectTaskRecommendationHandler : ICommandHandler<RejectTaskRecommendationCommand, TaskRecommendationDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly ITaskRecommendationRepository _recommendationRepository;
    private readonly IUserTaskHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRecommendationRealtimePublisher _realtimePublisher;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IClock _clock;
    private readonly IActivityLogService _activityLogService;

    public RejectTaskRecommendationHandler(
        ICurrentUser currentUser,
        ITaskRecommendationRepository recommendationRepository,
        IUserTaskHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        IRecommendationRealtimePublisher realtimePublisher,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IClock clock,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _recommendationRepository = recommendationRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _realtimePublisher = realtimePublisher;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _clock = clock;
        _activityLogService = activityLogService;
    }

    public async Task<Result<TaskRecommendationDto>> HandleAsync(
        RejectTaskRecommendationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RecommendationId == Guid.Empty)
        {
            return Result.Failure<TaskRecommendationDto>(
                RecommendationErrors.InvalidListRequest(new[] { "RecommendationId không hợp lệ." }));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<TaskRecommendationDto>(
                RecommendationErrors.MissingUserContext);
        }

        var recommendation = await _recommendationRepository.GetByIdForUpdateAsync(
            request.RecommendationId,
            cancellationToken);

        if (recommendation is null)
            return Result.Failure<TaskRecommendationDto>(RecommendationErrors.RecommendationNotFound);

        if (recommendation.UserId != currentUserId)
            return Result.Failure<TaskRecommendationDto>(RecommendationErrors.RecommendationForbidden);

        try
        {
            var now = _clock.UtcNow;

            recommendation.Reject(now);
            _recommendationRepository.Update(recommendation);

            var history = new UserTaskHistory(
                Guid.NewGuid(),
                recommendation.TaskId,
                currentUserId,
                now);

            history.MarkAsSkipped(now);
            _historyRepository.Add(history);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateUserRecommendationCacheAsync(currentUserId, cancellationToken);

            var dto = recommendation.ToDto();

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    recommendation.WorkspaceId,
                    currentUserId,
                    ActivityAction.Delete,
                    ActivityEntityType.UserPreference,
                    recommendation.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã bỏ qua một gợi ý task.",
                    ActivityLogMetadata.Serialize(new
                    {
                        recommendationId = recommendation.Id,
                        taskId = recommendation.TaskId
                    })),
                cancellationToken);

            await _realtimePublisher.PublishToUserAsync(
                new RecommendationRealtimeEnvelope(
                    EventName: "TaskRecommendationRejected",
                    UserId: currentUserId,
                    WorkspaceId: recommendation.WorkspaceId,
                    PageId: null,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Payload: dto),
                cancellationToken);

            return Result.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<TaskRecommendationDto>(
                RecommendationErrors.OperationFailed(ex.Message));
        }
    }

    private async Task InvalidateUserRecommendationCacheAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        await _cache.SetAsync(
            RecommendationCacheKeys.UserPendingVersion(_cacheKeyFactory, userId),
            Guid.NewGuid().ToString("N"),
            TimeSpan.FromDays(7),
            cancellationToken);
    }
}
