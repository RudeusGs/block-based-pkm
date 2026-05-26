using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Tasks.Models;
using Pkm.Application.Features.Tasks.Policies;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Tasks.Commands.UpdateTaskComment;

public sealed class UpdateTaskCommentHandler : ICommandHandler<UpdateTaskCommentCommand, TaskCommentDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly ITaskCommentRepository _taskCommentRepository;
    private readonly ITaskAccessEvaluator _taskAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IClock _clock;
    private readonly UpdateTaskCommentCommandValidator _validator;

    public UpdateTaskCommentHandler(
        ICurrentUser currentUser,
        ITaskCommentRepository taskCommentRepository,
        ITaskAccessEvaluator taskAccessEvaluator,
        IUnitOfWork unitOfWork,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IClock clock,
        UpdateTaskCommentCommandValidator validator)
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
        UpdateTaskCommentCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.InvalidUpdateRequest(validationErrors));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<TaskCommentDto>(TaskErrors.MissingUserContext);

        var comment = await _taskCommentRepository.GetByIdForUpdateAsync(
            request.CommentId,
            cancellationToken);

        if (comment is null)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.CommentNotFound);

        var access = await _taskAccessEvaluator.EvaluateAsync(
            comment.TaskId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<TaskCommentDto>(TaskErrors.TaskNotFound);

        if (!access.CanReadTask)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.CommentReadForbidden);

        if (comment.UserId != currentUserId)
            return Result.Failure<TaskCommentDto>(TaskCommentErrors.CommentForbidden);

        try
        {
            var now = _clock.UtcNow;

            comment.UpdateContent(request.Content, currentUserId, now);
            _taskCommentRepository.Update(comment);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await InvalidateCommentListCacheAsync(comment.TaskId, cancellationToken);

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
        var versionKey = TaskCommentCacheKeys.ListVersion(_cacheKeyFactory, taskId);

        await _cache.SetAsync(
            versionKey,
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);
    }

}
