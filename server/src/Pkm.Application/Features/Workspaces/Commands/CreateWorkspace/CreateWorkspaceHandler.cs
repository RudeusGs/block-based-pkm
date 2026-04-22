using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Domain.Common;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.CreateWorkspace;

public sealed class CreateWorkspaceHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly CreateWorkspaceCommandValidator _validator;

    public CreateWorkspaceHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        CreateWorkspaceCommandValidator validator)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _validator = validator;
    }

    public async Task<Result<WorkspaceDto>> HandleAsync(
        CreateWorkspaceCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.InvalidCreateRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.MissingUserContext);
        }

        try
        {
            var now = _clock.UtcNow;

            var workspace = new Workspace(
                Guid.NewGuid(),
                request.Name,
                currentUserId,
                now,
                request.Description);

            var ownerMember = WorkspaceMember.CreateOwner(workspace.Id, currentUserId, now);

            _workspaceRepository.Add(workspace);
            _workspaceMemberRepository.Add(ownerMember);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Invalidate user's workspace list cache
            var versionKey = WorkspaceCacheKeys.UserListVersion(_redisKeyFactory, currentUserId);
            await _redisCache.SetAsync(versionKey, Guid.NewGuid().ToString(), cancellationToken: cancellationToken);

            var dto = new WorkspaceDto(
                workspace.Id,
                workspace.Name,
                workspace.Description,
                workspace.OwnerId,
                workspace.LastModifiedBy,
                workspace.CreatedDate,
                workspace.UpdatedDate,
                WorkspaceRole.Owner,
                CanRead: true,
                CanWrite: true,
                CanManageMembers: true);

            return Result.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<WorkspaceDto>(new Error(
                "Workspace.CreateFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }
}