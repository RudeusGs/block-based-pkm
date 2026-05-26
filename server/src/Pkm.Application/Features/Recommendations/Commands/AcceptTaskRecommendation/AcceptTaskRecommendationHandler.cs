using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Recommendations.Models;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Audit;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Recommendations;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Recommendations.Commands.AcceptTaskRecommendation;

public sealed class AcceptTaskRecommendationHandler : ICommandHandler<AcceptTaskRecommendationCommand, TaskRecommendationDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly ITaskRecommendationRepository _recommendationRepository;
    private readonly ITaskAssigneeRepository _taskAssigneeRepository;
    private readonly IWorkTaskWriteRepository _workTaskWriteRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly IWorkTaskRecommendationReadRepository _workTaskRecommendationReadRepository;
    private readonly IUserTaskHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRecommendationRealtimePublisher _realtimePublisher;
    private readonly ITaskRealtimePublisher _taskRealtimePublisher;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IClock _clock;
    private readonly IActivityLogService _activityLogService;

    public AcceptTaskRecommendationHandler(
        ICurrentUser currentUser,
        ITaskRecommendationRepository recommendationRepository,
        ITaskAssigneeRepository taskAssigneeRepository,
        IWorkTaskWriteRepository workTaskWriteRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        IWorkTaskRecommendationReadRepository workTaskRecommendationReadRepository,
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
        _taskAssigneeRepository = taskAssigneeRepository;
        _workTaskWriteRepository = workTaskWriteRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _workTaskRecommendationReadRepository = workTaskRecommendationReadRepository;
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
        AcceptTaskRecommendationCommand request,
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
        {
            return Result.Failure<TaskRecommendationDto>(
                RecommendationErrors.RecommendationNotFound);
        }

        if (recommendation.UserId != currentUserId)
        {
            return Result.Failure<TaskRecommendationDto>(
                RecommendationErrors.RecommendationForbidden);
        }

        var task = await _workTaskWriteRepository.GetByIdForUpdateAsync(
            recommendation.TaskId,
            cancellationToken);

        if (task is null)
        {
            return Result.Failure<TaskRecommendationDto>(
                RecommendationErrors.TaskNotFound);
        }

        try
        {
            var now = _clock.UtcNow;

            recommendation.Accept(now);
            _recommendationRepository.Update(recommendation);

            var alreadyAssigned = await _taskAssigneeRepository.ExistsAsync(
                recommendation.TaskId,
                currentUserId,
                cancellationToken);

            if (!alreadyAssigned)
            {
                _taskAssigneeRepository.Add(
                    TaskAssignee.Create(
                        recommendation.TaskId,
                        currentUserId,
                        now));

                task.RecordAssignmentChange(currentUserId, now);
                _workTaskWriteRepository.Update(task);
            }

            _historyRepository.Add(
                new UserTaskHistory(
                    Guid.NewGuid(),
                    recommendation.TaskId,
                    currentUserId,
                    now));

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await InvalidateUserRecommendationCacheAsync(
                currentUserId,
                cancellationToken);

            var taskDetail = await _workTaskReadRepository.GetDetailAsync(
                task.Id,
                cancellationToken);

            var taskDto = taskDetail is null
                ? task.ToDto()
                : taskDetail.ToDto();

            var recommendationTaskMap =
                await _workTaskRecommendationReadRepository.ListRecommendationTaskDetailsByIdsAsync(
                    currentUserId,
                    new[] { recommendation.TaskId },
                    cancellationToken);

            recommendationTaskMap.TryGetValue(
                recommendation.TaskId,
                out var recommendationTask);

            var recommendationDto = recommendation.ToDto(recommendationTask);

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    recommendation.WorkspaceId,
                    currentUserId,
                    ActivityAction.Assign,
                    ActivityEntityType.WorkTask,
                    task.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã nhận task từ gợi ý.",
                    ActivityLogMetadata.Serialize(new
                    {
                        recommendationId = recommendation.Id,
                        taskId = task.Id,
                        pageId = task.PageId,
                        alreadyAssigned
                    })),
                cancellationToken);

            await _realtimePublisher.PublishToUserAsync(
                new RecommendationRealtimeEnvelope(
                    EventName: "TaskRecommendationAccepted",
                    UserId: currentUserId,
                    WorkspaceId: recommendation.WorkspaceId,
                    PageId: task.PageId,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Payload: new
                    {
                        recommendation = recommendationDto,
                        task = taskDto,
                        recommendationId = recommendation.Id,
                        taskId = task.Id
                    }),
                cancellationToken);

            await _taskRealtimePublisher.PublishToPageAsync(
                new TaskRealtimeEnvelope(
                    EventName: "TaskAssignedFromRecommendation",
                    WorkspaceId: task.WorkspaceId,
                    PageId: task.PageId,
                    TaskId: task.Id,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Payload: new
                    {
                        task = taskDto,
                        recommendation = recommendationDto,
                        taskId = task.Id,
                        recommendationId = recommendation.Id,
                        assigneeUserId = currentUserId
                    }),
                cancellationToken);

            await _taskRealtimePublisher.PublishToPageAsync(
                new TaskRealtimeEnvelope(
                    EventName: "TaskAssigned",
                    WorkspaceId: task.WorkspaceId,
                    PageId: task.PageId,
                    TaskId: task.Id,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Payload: new
                    {
                        task = taskDto,
                        taskId = task.Id,
                        assigneeUserId = currentUserId
                    }),
                cancellationToken);

            return Result.Success(recommendationDto);
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
