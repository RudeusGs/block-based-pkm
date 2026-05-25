using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Domain.Audit;

namespace Pkm.Application.Features.Workspaces.Commands.DeleteWorkspace;

public sealed class DeleteWorkspaceHandler : ICommandHandler<DeleteWorkspaceCommand>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly IActivityLogService _activityLogService;

    public DeleteWorkspaceHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _activityLogService = activityLogService;
    }

    public async Task<Result> HandleAsync(
        DeleteWorkspaceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure(WorkspaceErrors.MissingUserContext);
        }

        var isOwner = await _workspaceRepository.IsOwnerAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!isOwner)
        {
            var exists = await _workspaceRepository.ExistsAsync(request.WorkspaceId, cancellationToken);
            return exists
                ? Result.Failure(WorkspaceErrors.WorkspaceOwnerOnly)
                : Result.Failure(WorkspaceErrors.WorkspaceNotFound);
        }

        var workspace = await _workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure(WorkspaceErrors.WorkspaceNotFound);
        }

        var affectedMembers = await _workspaceMemberRepository.ListByWorkspaceAsync(
            request.WorkspaceId,
            cancellationToken);

        workspace.SoftDelete(_clock.UtcNow);
        _workspaceRepository.Update(workspace);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _activityLogService.RecordAsync(
            new ActivityLogRequest(
                workspace.Id,
                currentUserId,
                ActivityAction.Delete,
                ActivityEntityType.Workspace,
                workspace.Id,
                $"{_currentUser.UserName ?? "Có người"} đã xóa workspace '{workspace.Name}'."),
            cancellationToken);

        await _redisCache.RemoveAsync(
            WorkspaceCacheKeys.Detail(_redisKeyFactory, request.WorkspaceId),
            cancellationToken);

        await _redisCache.RemoveAsync(
            WorkspaceCacheKeys.Members(_redisKeyFactory, request.WorkspaceId),
            cancellationToken);

        foreach (var userId in affectedMembers.Select(x => x.UserId).Distinct())
        {
            await _redisCache.RemoveAsync(
                WorkspaceCacheKeys.Access(_redisKeyFactory, request.WorkspaceId, userId),
                cancellationToken);

            var versionKey = WorkspaceCacheKeys.UserListVersion(_redisKeyFactory, userId);
            await _redisCache.SetAsync(
                versionKey,
                Guid.NewGuid().ToString("N"),
                cancellationToken: cancellationToken);
        }

        return Result.Success();
    }
}
