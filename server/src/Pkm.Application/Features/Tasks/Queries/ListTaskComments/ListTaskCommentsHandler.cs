using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;

namespace Pkm.Application.Features.Tasks.Queries.ListTaskComments;

public sealed class ListTaskCommentsHandler : IQueryHandler<ListTaskCommentsQuery, TaskCommentPagedResultDto>
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(2);

    private readonly ICurrentUser _currentUser;
    private readonly ITaskCommentRepository _taskCommentRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly ListTaskCommentsQueryValidator _validator;

    public ListTaskCommentsHandler(
        ICurrentUser currentUser,
        ITaskCommentRepository taskCommentRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        ListTaskCommentsQueryValidator validator)
    {
        _currentUser = currentUser;
        _taskCommentRepository = taskCommentRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _validator = validator;
    }

    public async Task<Result<TaskCommentPagedResultDto>> HandleAsync(
        ListTaskCommentsQuery request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
            return Result.Failure<TaskCommentPagedResultDto>(
                TaskCommentErrors.InvalidListRequest(validationErrors));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<TaskCommentPagedResultDto>(TaskErrors.MissingUserContext);

        var access = await _taskAccessEvaluator.EvaluateAsync(
            request.TaskId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<TaskCommentPagedResultDto>(TaskErrors.TaskNotFound);

        if (!access.CanReadTask)
            return Result.Failure<TaskCommentPagedResultDto>(TaskCommentErrors.CommentReadForbidden);

        var versionKey = TaskCommentCacheKeys.ListVersion(_cacheKeyFactory, request.TaskId);
        var version = await _cache.GetAsync<string>(versionKey, cancellationToken) ?? "1";

        var cacheKey = TaskCommentCacheKeys.List(
            _cacheKeyFactory,
            request.TaskId,
            request.PageNumber,
            request.PageSize,
            request.IncludeDeleted,
            version);

        var cached = await _cache.GetAsync<TaskCommentPagedResultDto>(
            cacheKey,
            cancellationToken);

        if (cached is not null)
            return Result.Success(cached);

        var items = await _taskCommentRepository.ListByTaskAsync(
            request.TaskId,
            request.PageNumber,
            request.PageSize,
            request.IncludeDeleted,
            cancellationToken);

        var totalCount = await _taskCommentRepository.CountByTaskAsync(
            request.TaskId,
            request.IncludeDeleted,
            cancellationToken);

        var dto = new TaskCommentPagedResultDto(
            items.Select(x => x.ToDto()).ToArray(),
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
