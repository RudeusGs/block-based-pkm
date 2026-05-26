using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Recommendations.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Application.Features.Recommendations.Queries.ListTaskRecommendations;

public sealed class ListTaskRecommendationsHandler : IQueryHandler<ListTaskRecommendationsQuery, TaskRecommendationPagedResultDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly ITaskRecommendationRepository _taskRecommendationRepository;
    private readonly IWorkTaskRecommendationReadRepository _workTaskRepository;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ListTaskRecommendationsQueryValidator _validator;

    public ListTaskRecommendationsHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        ITaskRecommendationRepository taskRecommendationRepository,
        IWorkTaskRecommendationReadRepository workTaskRepository,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        ListTaskRecommendationsQueryValidator validator)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _taskRecommendationRepository = taskRecommendationRepository;
        _workTaskRepository = workTaskRepository;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _validator = validator;
    }

    public async Task<Result<TaskRecommendationPagedResultDto>> HandleAsync(
        ListTaskRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<TaskRecommendationPagedResultDto>(
                RecommendationErrors.InvalidListRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<TaskRecommendationPagedResultDto>(
                RecommendationErrors.MissingUserContext);
        }

        if (request.WorkspaceId.HasValue)
        {
            var access = await _workspaceAccessEvaluator.EvaluateAsync(
                request.WorkspaceId.Value,
                currentUserId,
                cancellationToken);

            if (!access.Exists)
                return Result.Failure<TaskRecommendationPagedResultDto>(WorkspaceErrors.WorkspaceNotFound);

            if (!access.CanRead)
                return Result.Failure<TaskRecommendationPagedResultDto>(WorkspaceErrors.WorkspaceForbidden);
        }

        var versionKey = RecommendationCacheKeys.UserPendingVersion(
            _cacheKeyFactory,
            currentUserId);

        var version = await _cache.GetAsync<string>(
            versionKey,
            cancellationToken) ?? "1";

        var statusKey = request.Status?.ToString() ?? "all";

        var cacheKey = RecommendationCacheKeys.PendingList(
            _cacheKeyFactory,
            currentUserId,
            request.WorkspaceId,
            statusKey,
            request.PageNumber,
            request.PageSize,
            version);

        var cached = await _cache.GetAsync<TaskRecommendationPagedResultDto>(
            cacheKey,
            cancellationToken);

        if (cached is not null)
            return Result.Success(cached);

        var items = await _taskRecommendationRepository.ListByUserAsync(
            currentUserId,
            request.WorkspaceId,
            request.Status,
            request.PageNumber,
            request.PageSize,
            cancellationToken);

        var totalCount = await _taskRecommendationRepository.CountByUserAsync(
            currentUserId,
            request.WorkspaceId,
            request.Status,
            cancellationToken);

        var taskIds = items
            .Select(x => x.TaskId)
            .Distinct()
            .ToArray();

        var taskMap = await _workTaskRepository.ListRecommendationTaskDetailsByIdsAsync(
            currentUserId,
            taskIds,
            cancellationToken);

        var dto = new TaskRecommendationPagedResultDto(
            items.Select(x =>
            {
                taskMap.TryGetValue(x.TaskId, out var task);
                return x.ToDto(task);
            }).ToArray(),
            request.PageNumber,
            request.PageSize,
            totalCount,
            totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize));

        await _cache.SetAsync(
            cacheKey,
            dto,
            CacheTtl,
            cancellationToken);

        return Result.Success(dto);
    }
}
