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
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Recommendations.Commands.CompleteTaskRecommendation;

public sealed class CompleteTaskRecommendationHandler : ICommandHandler<CompleteTaskRecommendationCommand, TaskRecommendationDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly ITaskRecommendationRepository _recommendationRepository;
    private readonly IWorkTaskWriteRepository _workTaskWriteRepository;
    private readonly IUserTaskHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRecommendationRealtimePublisher _realtimePublisher;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IClock _clock;
    private readonly IActivityLogService _activityLogService;

    public CompleteTaskRecommendationHandler(
        ICurrentUser currentUser,
        ITaskRecommendationRepository recommendationRepository,
        IWorkTaskWriteRepository workTaskWriteRepository,
        IUserTaskHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        IRecommendationRealtimePublisher realtimePublisher,
        ITaskRealtimePublisher taskRealtimePublisher,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IClock clock,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _recommendationRepository = recommendationRepository;
        _workTaskWriteRepository = workTaskWriteRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _realtimePublisher = realtimePublisher;
        _taskRealtimePublisher = taskRealtimePublisher;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _clock = clock;
        _activityLogService = activityLogService;
    }

    public async Task<Result<TaskRecommendationDto>> HandleAsync(
        CompleteTaskRecommendationCommand request,
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

        var task = await _workTaskWriteRepository.GetByIdForUpdateAsync(
            recommendation.TaskId,
            cancellationToken);

        if (task is null)
            return Result.Failure<TaskRecommendationDto>(RecommendationErrors.TaskNotFound);

        try
        {
            var now = _clock.UtcNow;

            if (recommendation.Status == StatusTaskRecommendation.Pending)
            {
                recommendation.Accept(now);
            }

            recommendation.MarkCompleted(now);
            _recommendationRepository.Update(recommendation);

            task.Complete(currentUserId, now);
            _workTaskWriteRepository.Update(task);

            var history = new UserTaskHistory(
                Guid.NewGuid(),
                recommendation.TaskId,
                currentUserId,
                recommendation.AcceptedAt ?? recommendation.CreatedDate);

            history.MarkAsCompleted(now, request.Notes);
            _historyRepository.Add(history);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateCachesAsync(currentUserId, recommendation.WorkspaceId, cancellationToken);

            var dto = recommendation.ToDto();

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    recommendation.WorkspaceId,
                    currentUserId,
                    ActivityAction.Complete,
                    ActivityEntityType.WorkTask,
                    task.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã hoàn thành task từ gợi ý.",
                    ActivityLogMetadata.Serialize(new
                    {
                        recommendationId = recommendation.Id,
                        taskId = task.Id,
                        pageId = task.PageId,
                        notes = request.Notes
                    })),
                cancellationToken);

            await _realtimePublisher.PublishToUserAsync(
                new RecommendationRealtimeEnvelope(
                    EventName: "TaskRecommendationCompleted",
                    UserId: currentUserId,
                    WorkspaceId: recommendation.WorkspaceId,
                    PageId: task.PageId,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Payload: dto),
                cancellationToken);

            await _taskRealtimePublisher.PublishToPageAsync(
                new TaskRealtimeEnvelope(
                    EventName: "TaskCompletedFromRecommendation",
                    WorkspaceId: task.WorkspaceId,
                    PageId: task.PageId,
                    TaskId: task.Id,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Payload: new
                    {
                        taskId = task.Id,
                        recommendationId = recommendation.Id
                    }),
                cancellationToken);

            return Result.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<TaskRecommendationDto>(
                RecommendationErrors.OperationFailed(ex.Message));
        }
    }

    private async Task InvalidateCachesAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        await _cache.SetAsync(
            RecommendationCacheKeys.UserPendingVersion(_cacheKeyFactory, userId),
            Guid.NewGuid().ToString("N"),
            TimeSpan.FromDays(7),
            cancellationToken);

        await _cache.RemoveAsync(
            RecommendationCacheKeys.HistoryStats(_cacheKeyFactory, workspaceId, userId),
            cancellationToken);
    }
}
