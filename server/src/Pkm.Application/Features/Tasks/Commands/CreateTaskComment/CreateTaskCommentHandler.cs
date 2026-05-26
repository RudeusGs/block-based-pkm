using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Features.Tasks.Commands.CreateTaskComment;

public sealed class CreateTaskCommentHandler : ICommandHandler<CreateTaskCommentCommand, TaskCommentDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly ITaskCommentRepository _taskCommentRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IClock _clock;
    private readonly CreateTaskCommentCommandValidator _validator;

    public CreateTaskCommentHandler(
        ICurrentUser currentUser,
        ITaskCommentRepository taskCommentRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IClock clock,
        CreateTaskCommentCommandValidator validator)
    {
        _currentUser = currentUser;
        _taskCommentRepository = taskCommentRepository;
        _taskAccessEvaluator = taskAccessEvaluator;
        _unitOfWork = unitOfWork;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _clock = clock;
        _validator = validator;
    }

    public async Task<Result<TaskCommentDto>> HandleAsync(
        CreateTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<TaskCommentDto>(
                TaskCommentErrors.InvalidCreateRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<TaskCommentDto>(
                TaskErrors.MissingUserContext);
        }

        var access = await _taskAccessEvaluator.EvaluateAsync(
            request.TaskId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<TaskCommentDto>(
                TaskErrors.TaskNotFound);
        }

        if (!access.CanCommentTask)
        {
            return Result.Failure<TaskCommentDto>(
                TaskCommentErrors.CommentCreateForbidden);
        }

        if (request.ParentId.HasValue)
        {
            var parentComment = await _taskCommentRepository.GetByIdAsync(
                request.ParentId.Value,
                cancellationToken);

            if (parentComment is null || parentComment.TaskId != request.TaskId)
            {
                return Result.Failure<TaskCommentDto>(
                    TaskCommentErrors.ParentCommentNotFound);
            }
        }

        try
        {
            var now = _clock.UtcNow;

            var comment = request.ParentId.HasValue
                ? TaskComment.CreateReply(
                    request.TaskId,
                    currentUserId,
                    request.Content,
                    request.ParentId.Value,
                    now)
                : TaskComment.Create(
                    request.TaskId,
                    currentUserId,
                    request.Content,
                    now);

            _taskCommentRepository.Add(comment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateCommentListCacheAsync(request.TaskId, cancellationToken);

            var dto = comment.ToDto();

            return Result.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<TaskCommentDto>(
                TaskCommentErrors.OperationFailed(ex.Message));
        }
    }

    private async Task InvalidateCommentListCacheAsync(
        Guid taskId,
        CancellationToken cancellationToken)
    {
        var versionKey = TaskCommentCacheKeys.ListVersion(
            _cacheKeyFactory,
            taskId);

        await _cache.SetAsync(
            versionKey,
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);
    }

}
