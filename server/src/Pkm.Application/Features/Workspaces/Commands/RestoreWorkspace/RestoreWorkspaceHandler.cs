using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Audit;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.RestoreWorkspace;

public sealed class RestoreWorkspaceHandler : ICommandHandler<RestoreWorkspaceCommand, WorkspaceDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IActivityLogService _activityLogService;

    public RestoreWorkspaceHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _activityLogService = activityLogService;
    }

    public async Task<Result<WorkspaceDto>> HandleAsync(
        RestoreWorkspaceCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.MissingUserContext);
        }

        var workspace = await _workspaceRepository.GetTrashedByIdAsync(
            request.WorkspaceId,
            cancellationToken);

        if (workspace is null)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);
        }

        if (workspace.OwnerId != currentUserId)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceOwnerOnly);
        }

        var affectedMembers = await _workspaceMemberRepository.ListByWorkspaceAsync(
            request.WorkspaceId,
            cancellationToken);

        try
        {
            var now = _clock.UtcNow;

            workspace.RestoreFromTrash(currentUserId, now);
            _workspaceRepository.Update(workspace);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    workspace.Id,
                    currentUserId,
                    ActivityAction.Restore,
                    ActivityEntityType.Workspace,
                    workspace.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã khôi phục workspace '{workspace.Name}' từ Trash."),
                cancellationToken);

            await InvalidateWorkspaceCachesAsync(
                workspace.Id,
                affectedMembers.Select(x => x.UserId).Append(currentUserId),
                cancellationToken);

            return Result.Success(new WorkspaceDto(
                workspace.Id,
                workspace.Name,
                workspace.Description,
                workspace.AvatarUrl,
                workspace.Visibility,
                workspace.OwnerId,
                workspace.LastModifiedBy,
                workspace.CreatedDate,
                workspace.UpdatedDate,
                WorkspaceRole.Owner,
                CanRead: true,
                CanWrite: true,
                CanManageMembers: true));
        }
        catch (DomainException ex)
        {
            return Result.Failure<WorkspaceDto>(new Error(
                "Workspace.RestoreFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }

    private async Task InvalidateWorkspaceCachesAsync(
        Guid workspaceId,
        IEnumerable<Guid> userIds,
        CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(
            WorkspaceCacheKeys.Detail(_cacheKeyFactory, workspaceId),
            cancellationToken);

        await _cache.RemoveAsync(
            WorkspaceCacheKeys.Members(_cacheKeyFactory, workspaceId),
            cancellationToken);

        foreach (var userId in userIds.Distinct())
        {
            await _cache.RemoveAsync(
                WorkspaceCacheKeys.Access(_cacheKeyFactory, workspaceId, userId),
                cancellationToken);

            await _cache.SetAsync(
                WorkspaceCacheKeys.UserListVersion(_cacheKeyFactory, userId),
                Guid.NewGuid().ToString("N"),
                cancellationToken: cancellationToken);
        }
    }
}
