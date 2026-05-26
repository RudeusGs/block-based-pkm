using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Caching;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Recommendations.Models;
using Pkm.Application.Features.Recommendations.Services;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Audit;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Notifications;
using Pkm.Domain.Recommendations;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Recommendations.Commands.GenerateTaskRecommendations;

public sealed class GenerateTaskRecommendationsHandler : ICommandHandler<GenerateTaskRecommendationsCommand, IReadOnlyList<TaskRecommendationDto>>
{
    private const int CandidateTake = 100;
    private const int RecommendationValidHours = 24;

    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkTaskReadRepository _workTaskReadRepository;
    private readonly IWorkTaskRecommendationReadRepository _workTaskRecommendationReadRepository;
    private readonly ITaskRecommendationRepository _taskRecommendationRepository;
    private readonly IUserTaskPreferenceRepository _preferenceRepository;
    private readonly IUserTaskHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRecommendationScoringService _scoringService;
    private readonly IRecommendationCandidateDeduplicator _candidateDeduplicator;
    private readonly IRecommendationRealtimePublisher _recommendationRealtimePublisher;
    private readonly INotificationService _notificationService;
    private readonly IBestEffortCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IClock _clock;
    private readonly GenerateTaskRecommendationsCommandValidator _validator;
    private readonly IActivityLogService _activityLogService;

    public GenerateTaskRecommendationsHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IWorkspaceRepository workspaceRepository,
        IWorkTaskReadRepository workTaskReadRepository,
        IWorkTaskRecommendationReadRepository workTaskRecommendationReadRepository,
        ITaskRecommendationRepository taskRecommendationRepository,
        IUserTaskPreferenceRepository preferenceRepository,
        IUserTaskHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        IRecommendationScoringService scoringService,
        IRecommendationCandidateDeduplicator candidateDeduplicator,
        IRecommendationRealtimePublisher recommendationRealtimePublisher,
        INotificationService notificationService,
        IBestEffortCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IClock clock,
        GenerateTaskRecommendationsCommandValidator validator,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _workspaceRepository = workspaceRepository;
        _workTaskReadRepository = workTaskReadRepository;
        _workTaskRecommendationReadRepository = workTaskRecommendationReadRepository;
        _taskRecommendationRepository = taskRecommendationRepository;
        _preferenceRepository = preferenceRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _scoringService = scoringService;
        _candidateDeduplicator = candidateDeduplicator;
        _recommendationRealtimePublisher = recommendationRealtimePublisher;
        _notificationService = notificationService;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _clock = clock;
        _validator = validator;
        _activityLogService = activityLogService;
    }

    public async Task<Result<IReadOnlyList<TaskRecommendationDto>>> HandleAsync(
        GenerateTaskRecommendationsCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                RecommendationErrors.InvalidGenerateRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                RecommendationErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanRead)
        {
            return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                WorkspaceErrors.WorkspaceForbidden);
        }

        var now = _clock.UtcNow;

        var isPersonalWorkspace = await IsPersonalWorkspaceAsync(
            request.WorkspaceId,
            currentUserId,
            access.Role,
            cancellationToken);

        var preference = await GetOrCreatePreferenceAsync(
            currentUserId,
            request.WorkspaceId,
            now,
            cancellationToken);

        if (!request.Force && !preference.EnableAutoRecommendation)
        {
            return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                RecommendationErrors.AutoRecommendationDisabled);
        }

        if (!request.Force && !preference.IsSuitableForRecommendation(now))
        {
            return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                RecommendationErrors.NotInRecommendationTime);
        }

        if (!request.Force)
        {
            var throttleKey = RecommendationCacheKeys.UserThrottle(
                _cacheKeyFactory,
                request.WorkspaceId,
                currentUserId);

            var isThrottled = await _cache.ExistsAsync(
                throttleKey,
                cancellationToken);

            if (isThrottled)
            {
                return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                    RecommendationErrors.Throttled);
            }

            if (isPersonalWorkspace)
            {
                var hasActiveAssignedTask = await _workTaskReadRepository.HasActiveAssignedTaskAsync(
                    currentUserId,
                    request.WorkspaceId,
                    cancellationToken);

                if (hasActiveAssignedTask)
                {
                    return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                        RecommendationErrors.AlreadyHasActiveTasks);
                }
            }
        }

        var candidates = await _workTaskRecommendationReadRepository.ListRecommendationCandidatesAsync(
            currentUserId,
            request.WorkspaceId,
            request.PageId,
            CandidateTake,
            cancellationToken);

        var previouslyRecommendedTaskIds =
            await _taskRecommendationRepository.ListPreviouslyRecommendedTaskIdsByUserAndWorkspaceAsync(
                currentUserId,
                request.WorkspaceId,
                cancellationToken);

        candidates = await _candidateDeduplicator.RemovePreviouslyRecommendedSemanticDuplicatesAsync(
            candidates,
            previouslyRecommendedTaskIds,
            currentUserId,
            cancellationToken);

        if (candidates.Count == 0)
        {
            await SetThrottleAsync(preference, request.WorkspaceId, currentUserId, cancellationToken);
            return Result.Success<IReadOnlyList<TaskRecommendationDto>>(Array.Empty<TaskRecommendationDto>());
        }

        var historyStats = await GetHistoryStatsAsync(
            currentUserId,
            request.WorkspaceId,
            cancellationToken);

        var scored = _scoringService.Score(
            candidates,
            preference,
            historyStats,
            now,
            isPersonalWorkspace);

        if (scored.Count == 0)
        {
            await SetThrottleAsync(preference, request.WorkspaceId, currentUserId, cancellationToken);
            return Result.Success<IReadOnlyList<TaskRecommendationDto>>(Array.Empty<TaskRecommendationDto>());
        }

        try
        {
            var recommendations = scored
                .Select(x => new TaskRecommendation(
                    Guid.NewGuid(),
                    x.Candidate.TaskId,
                    currentUserId,
                    request.WorkspaceId,
                    x.Score,
                    now,
                    x.Reason,
                    RecommendationValidHours))
                .ToArray();

            _taskRecommendationRepository.AddRange(recommendations);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await SetThrottleAsync(preference, request.WorkspaceId, currentUserId, cancellationToken);
            await InvalidateUserRecommendationCacheAsync(currentUserId, cancellationToken);

            var candidateMap = scored.ToDictionary(x => x.Candidate.TaskId, x => x.Candidate);

            var dto = recommendations
                .Select(x =>
                {
                    candidateMap.TryGetValue(x.TaskId, out var candidate);
                    return x.ToDto(candidate);
                })
                .ToArray();

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    request.WorkspaceId,
                    currentUserId,
                    ActivityAction.Create,
                    ActivityEntityType.UserPreference,
                    currentUserId,
                    $"{_currentUser.UserName ?? "Có người"} đã tạo {dto.Length} gợi ý task.",
                    ActivityLogMetadata.Serialize(new
                    {
                        recommendationIds = dto.Select(x => x.Id).ToArray(),
                        taskIds = dto.Select(x => x.TaskId).ToArray(),
                        pageId = request.PageId,
                        mode = isPersonalWorkspace ? "semantic_personal_prioritizer" : "semantic_team_prioritizer",
                        count = dto.Length
                    })),
                cancellationToken);

            await PublishGeneratedBestEffortAsync(
                currentUserId,
                request.WorkspaceId,
                request.PageId,
                now,
                dto,
                isPersonalWorkspace,
                cancellationToken);

            await NotifyGeneratedBestEffortAsync(
                currentUserId,
                request.WorkspaceId,
                dto,
                isPersonalWorkspace,
                cancellationToken);

            return Result.Success<IReadOnlyList<TaskRecommendationDto>>(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                RecommendationErrors.OperationFailed(ex.Message));
        }
    }

    private async Task<bool> IsPersonalWorkspaceAsync(
        Guid workspaceId,
        Guid currentUserId,
        WorkspaceRole? currentUserRole,
        CancellationToken cancellationToken)
    {
        if (currentUserRole != WorkspaceRole.Owner)
            return false;

        var memberCount = await _workspaceRepository.CountMembersAsync(
            workspaceId,
            cancellationToken);

        return memberCount <= 1;
    }

    private async Task<UserTaskPreference> GetOrCreatePreferenceAsync(
        Guid userId,
        Guid workspaceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var cacheKey = RecommendationCacheKeys.Preference(
            _cacheKeyFactory,
            workspaceId,
            userId);

        var cached = await _cache.GetAsync<UserTaskPreferenceDto>(
            cacheKey,
            cancellationToken);

        var preference = await _preferenceRepository.GetByUserAndWorkspaceAsync(
            userId,
            workspaceId,
            cancellationToken);

        if (preference is not null)
            return preference;

        preference = new UserTaskPreference(
            Guid.NewGuid(),
            userId,
            workspaceId,
            now);

        _preferenceRepository.Add(preference);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cache.SetAsync(
            cacheKey,
            preference.ToDto(),
            TimeSpan.FromMinutes(15),
            cancellationToken);

        return preference;
    }

    private async Task<UserTaskHistoryStatsDto> GetHistoryStatsAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var cacheKey = RecommendationCacheKeys.HistoryStats(
            _cacheKeyFactory,
            workspaceId,
            userId);

        var cached = await _cache.GetAsync<UserTaskHistoryStatsDto>(
            cacheKey,
            cancellationToken);

        if (cached is not null)
            return cached;

        var stats = await _historyRepository.GetStatsByUserAndWorkspaceAsync(
            userId,
            workspaceId,
            cancellationToken);

        await _cache.SetAsync(
            cacheKey,
            stats,
            TimeSpan.FromMinutes(10),
            cancellationToken);

        return stats;
    }

    private async Task SetThrottleAsync(
        UserTaskPreference preference,
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var throttleKey = RecommendationCacheKeys.UserThrottle(
            _cacheKeyFactory,
            workspaceId,
            userId);

        await _cache.SetAsync(
            throttleKey,
            "1",
            TimeSpan.FromMinutes(preference.RecommendationIntervalMinutes),
            cancellationToken);
    }

    private async Task InvalidateUserRecommendationCacheAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var versionKey = RecommendationCacheKeys.UserPendingVersion(
            _cacheKeyFactory,
            userId);

        await _cache.SetAsync(
            versionKey,
            Guid.NewGuid().ToString("N"),
            TimeSpan.FromDays(7),
            cancellationToken);
    }

    private async Task PublishGeneratedBestEffortAsync(
        Guid currentUserId,
        Guid workspaceId,
        Guid? pageId,
        DateTimeOffset now,
        IReadOnlyList<TaskRecommendationDto> recommendations,
        bool isPersonalWorkspace,
        CancellationToken cancellationToken)
    {
        try
        {
            await _recommendationRealtimePublisher.PublishToUserAsync(
                new RecommendationRealtimeEnvelope(
                    EventName: "TaskRecommendationsGenerated",
                    UserId: currentUserId,
                    WorkspaceId: workspaceId,
                    PageId: pageId,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Payload: new
                    {
                        workspaceId,
                        pageId,
                        mode = isPersonalWorkspace ? "semantic_personal_prioritizer" : "semantic_team_prioritizer",
                        recommendations
                    }),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
        }
    }

    private async Task NotifyGeneratedBestEffortAsync(
        Guid currentUserId,
        Guid workspaceId,
        IReadOnlyList<TaskRecommendationDto> recommendations,
        bool isPersonalWorkspace,
        CancellationToken cancellationToken)
    {
        try
        {
            var message = isPersonalWorkspace
                ? $"AI vừa chọn {recommendations.Count} task ưu tiên, đã lọc các task trùng nội dung và cân nhắc deadline/thói quen làm việc của bạn."
                : $"AI vừa chọn {recommendations.Count} task ưu tiên, đã lọc các task trùng nội dung và cân nhắc deadline/người phụ trách trong workspace.";

            await _notificationService.NotifyAsync(
                currentUserId,
                new NotificationDispatchRequest(
                    Type: NotificationType.RecommendationGenerated,
                    Title: "Có task được gợi ý cho bạn",
                    Message: message,
                    ActorUserId: currentUserId,
                    WorkspaceId: workspaceId,
                    ReferenceId: recommendations.FirstOrDefault()?.Id,
                    ReferenceType: "TaskRecommendation"),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
        }
    }
}


