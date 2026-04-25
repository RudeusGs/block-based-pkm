using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
using Pkm.Application.Features.Recommendations.Models;
using Pkm.Application.Features.Recommendations.Services;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Common;
using Pkm.Domain.Notifications;
using Pkm.Domain.Recommendations;

namespace Pkm.Application.Features.Recommendations.Commands.GenerateTaskRecommendations;

public sealed class GenerateTaskRecommendationsHandler
{
    private const int CandidateTake = 100;
    private const int RecommendationValidHours = 24;

    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IWorkTaskRepository _workTaskRepository;
    private readonly ITaskRecommendationRepository _taskRecommendationRepository;
    private readonly IUserTaskPreferenceRepository _preferenceRepository;
    private readonly IUserTaskHistoryRepository _historyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRecommendationScoringService _scoringService;
    private readonly IRecommendationRealtimePublisher _recommendationRealtimePublisher;
    private readonly INotificationService _notificationService;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IClock _clock;
    private readonly GenerateTaskRecommendationsCommandValidator _validator;

    public GenerateTaskRecommendationsHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IWorkTaskRepository workTaskRepository,
        ITaskRecommendationRepository taskRecommendationRepository,
        IUserTaskPreferenceRepository preferenceRepository,
        IUserTaskHistoryRepository historyRepository,
        IUnitOfWork unitOfWork,
        IRecommendationScoringService scoringService,
        IRecommendationRealtimePublisher recommendationRealtimePublisher,
        INotificationService notificationService,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IClock clock,
        GenerateTaskRecommendationsCommandValidator validator)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _workTaskRepository = workTaskRepository;
        _taskRecommendationRepository = taskRecommendationRepository;
        _preferenceRepository = preferenceRepository;
        _historyRepository = historyRepository;
        _unitOfWork = unitOfWork;
        _scoringService = scoringService;
        _recommendationRealtimePublisher = recommendationRealtimePublisher;
        _notificationService = notificationService;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _clock = clock;
        _validator = validator;
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
                _redisKeyFactory,
                request.WorkspaceId,
                currentUserId);

            if (await _redisCache.ExistsAsync(throttleKey, cancellationToken))
            {
                return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                    RecommendationErrors.Throttled);
            }

            var hasActiveAssignedTask = await _workTaskRepository.HasActiveAssignedTaskAsync(
                currentUserId,
                request.WorkspaceId,
                cancellationToken);

            if (hasActiveAssignedTask)
            {
                return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                    RecommendationErrors.AlreadyHasActiveTasks);
            }
        }

        var candidates = await _workTaskRepository.ListRecommendationCandidatesAsync(
            currentUserId,
            request.WorkspaceId,
            request.PageId,
            CandidateTake,
            cancellationToken);

        var pending = await _taskRecommendationRepository.ListPendingByUserAndWorkspaceAsync(
            currentUserId,
            request.WorkspaceId,
            cancellationToken);

        var pendingTaskIds = pending.Select(x => x.TaskId).ToHashSet();

        candidates = candidates
            .Where(x => !pendingTaskIds.Contains(x.TaskId))
            .ToArray();

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
            now);

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

            await _recommendationRealtimePublisher.PublishToUserAsync(
                new RecommendationRealtimeEnvelope(
                    EventName: "TaskRecommendationsGenerated",
                    UserId: currentUserId,
                    WorkspaceId: request.WorkspaceId,
                    PageId: request.PageId,
                    ActorId: currentUserId,
                    OccurredAtUtc: now,
                    Payload: new
                    {
                        workspaceId = request.WorkspaceId,
                        pageId = request.PageId,
                        recommendations = dto
                    }),
                cancellationToken);

            await _notificationService.NotifyAsync(
                currentUserId,
                new NotificationDispatchRequest(
                    Type: NotificationType.RecommendationGenerated,
                    Title: "Có task được gợi ý cho bạn",
                    Message: $"Hệ thống vừa gợi ý {dto.Length} task phù hợp với lịch sử làm việc của bạn.",
                    ActorUserId: currentUserId,
                    WorkspaceId: request.WorkspaceId,
                    ReferenceId: dto.FirstOrDefault()?.Id,
                    ReferenceType: "TaskRecommendation"),
                cancellationToken);

            return Result.Success<IReadOnlyList<TaskRecommendationDto>>(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<IReadOnlyList<TaskRecommendationDto>>(
                RecommendationErrors.OperationFailed(ex.Message));
        }
    }

    private async Task<UserTaskPreference> GetOrCreatePreferenceAsync(
        Guid userId,
        Guid workspaceId,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var cacheKey = RecommendationCacheKeys.Preference(
            _redisKeyFactory,
            workspaceId,
            userId);

        var cached = await _redisCache.GetAsync<UserTaskPreferenceDto>(
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

        await _redisCache.SetAsync(
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
            _redisKeyFactory,
            workspaceId,
            userId);

        var cached = await _redisCache.GetAsync<UserTaskHistoryStatsDto>(
            cacheKey,
            cancellationToken);

        if (cached is not null)
            return cached;

        var stats = await _historyRepository.GetStatsByUserAndWorkspaceAsync(
            userId,
            workspaceId,
            cancellationToken);

        await _redisCache.SetAsync(
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
            _redisKeyFactory,
            workspaceId,
            userId);

        await _redisCache.SetAsync(
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
            _redisKeyFactory,
            userId);

        await _redisCache.SetAsync(
            versionKey,
            Guid.NewGuid().ToString("N"),
            TimeSpan.FromDays(7),
            cancellationToken);
    }
}