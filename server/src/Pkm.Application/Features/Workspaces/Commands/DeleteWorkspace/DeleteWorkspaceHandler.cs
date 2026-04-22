using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Workspaces.Commands.DeleteWorkspace;

public sealed class DeleteWorkspaceHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;

    public DeleteWorkspaceHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
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

        var affectedMembers = await _workspaceMemberRepository.ListReadModelsByWorkspaceAsync(
            request.WorkspaceId,
            cancellationToken);

        workspace.SoftDelete(_clock.UtcNow);
        _workspaceRepository.Update(workspace);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

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