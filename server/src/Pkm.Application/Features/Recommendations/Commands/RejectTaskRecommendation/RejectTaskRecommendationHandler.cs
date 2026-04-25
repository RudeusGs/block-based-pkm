using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Recommendations.Models;
using Pkm.Domain.Common;
using Pkm.Domain.Recommendations;

namespace Pkm.Application.Features.Recommendations.Commands.RejectTaskRecommendation;

public sealed class RejectTaskRecommendationHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly ITaskRecommendationRepository _recommendationRepository;
    private readonly IUserTaskHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRecommendationRealtimePublisher _realtimePublisher;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IClock _clock;

    public RejectTaskRecommendationHandler(
        ICurrentUser currentUser,
        ITaskRecommendationRepository recommendationRepository,
        IUserTaskHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        IRecommendationRealtimePublisher realtimePublisher,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IClock clock)
    {
        _currentUser = currentUser;
        _recommendationRepository = recommendationRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _realtimePublisher = realtimePublisher;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _clock = clock;
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
        await _redisCache.SetAsync(
            RecommendationCacheKeys.UserPendingVersion(_redisKeyFactory, userId),
            Guid.NewGuid().ToString("N"),
            TimeSpan.FromDays(7),
            cancellationToken);
    }
}