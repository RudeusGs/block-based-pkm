using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Caching;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Common;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.UpdateWorkspace;

public sealed class UpdateWorkspaceHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IRedisCache _redisCache;
    private readonly IRedisKeyFactory _redisKeyFactory;
    private readonly UpdateWorkspaceCommandValidator _validator;

    public UpdateWorkspaceHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IUnitOfWork unitOfWork,
        IClock clock,
        IRedisCache redisCache,
        IRedisKeyFactory redisKeyFactory,
        UpdateWorkspaceCommandValidator validator)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _redisCache = redisCache;
        _redisKeyFactory = redisKeyFactory;
        _validator = validator;
    }

    public async Task<Result<WorkspaceDto>> HandleAsync(
        UpdateWorkspaceCommand request,
        CancellationToken cancellationToken)
    {
        var validationErrors = _validator.Validate(request);
        if (validationErrors.Count > 0)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.InvalidUpdateRequest(validationErrors));
        }

        if (!_currentUser.TryGetUserId(out var currentUserId))
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.MissingUserContext);
        }

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);
        }

        if (!access.CanUpdateWorkspace)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceForbidden);
        }

        var workspace = await _workspaceRepository.GetByIdAsync(request.WorkspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);
        }

        try
        {
            workspace.UpdateInformation(
                request.Name,
                request.Description,
                currentUserId,
                _clock.UtcNow);

            _workspaceRepository.Update(workspace);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _redisCache.RemoveAsync(
                WorkspaceCacheKeys.Detail(_redisKeyFactory, workspace.Id),
                cancellationToken);

            await InvalidateWorkspaceListVersionsAsync(workspace.Id, cancellationToken);

            var member = await _workspaceRepository.GetMemberAsync(
                request.WorkspaceId,
                currentUserId,
                cancellationToken);

            var dto = new WorkspaceDto(
                workspace.Id,
                workspace.Name,
                workspace.Description,
                workspace.OwnerId,
                workspace.LastModifiedBy,
                workspace.CreatedDate,
                workspace.UpdatedDate,
                member?.Role ?? (workspace.OwnerId == currentUserId ? WorkspaceRole.Owner : null),
                access.CanRead,
                access.CanWrite,
                access.CanManageMembers);

            return Result.Success(dto);
        }
        catch (DomainException ex)
        {
            return Result.Failure<WorkspaceDto>(new Error(
                "Workspace.UpdateFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }

    private async Task InvalidateWorkspaceListVersionsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var members = await _workspaceMemberRepository.ListReadModelsByWorkspaceAsync(
            workspaceId,
            cancellationToken);

        foreach (var userId in members.Select(x => x.UserId).Distinct())
        {
            var versionKey = WorkspaceCacheKeys.UserListVersion(_redisKeyFactory, userId);
            await _redisCache.SetAsync(
                versionKey,
                Guid.NewGuid().ToString("N"),
                cancellationToken: cancellationToken);
        }
    }
}