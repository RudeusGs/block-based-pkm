using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Files.Services;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Models;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Audit;
using Pkm.Domain.SharedKernel;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Files.Commands.UploadWorkspaceAvatarImage;

public sealed class UploadWorkspaceAvatarImageHandler : ICommandHandler<UploadWorkspaceAvatarImageCommand, WorkspaceDto>
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IWorkspaceMemberRepository _workspaceMemberRepository;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IFileUploadApplicationService _fileUploadApplicationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IApplicationCache _cache;
    private readonly ICacheKeyFactory _cacheKeyFactory;
    private readonly IActivityLogService _activityLogService;

    public UploadWorkspaceAvatarImageHandler(
        ICurrentUser currentUser,
        IWorkspaceRepository workspaceRepository,
        IWorkspaceMemberRepository workspaceMemberRepository,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IFileUploadApplicationService fileUploadApplicationService,
        IUnitOfWork unitOfWork,
        IClock clock,
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _workspaceRepository = workspaceRepository;
        _workspaceMemberRepository = workspaceMemberRepository;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _fileUploadApplicationService = fileUploadApplicationService;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _cache = cache;
        _cacheKeyFactory = cacheKeyFactory;
        _activityLogService = activityLogService;
    }

    public async Task<Result<WorkspaceDto>> HandleAsync(
        UploadWorkspaceAvatarImageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.MissingUserContext);

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);

        if (!access.CanUpdateWorkspace)
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceForbidden);

        var workspace = await _workspaceRepository.GetByIdAsync(
            request.WorkspaceId,
            cancellationToken);

        if (workspace is null)
            return Result.Failure<WorkspaceDto>(WorkspaceErrors.WorkspaceNotFound);

        var uploadResult = await _fileUploadApplicationService.UploadImageAsync(
            new UploadImageInput(
                currentUserId,
                request.FileName,
                request.ContentType,
                request.SizeBytes,
                request.Content,
                "workspace-avatar"),
            cancellationToken);

        if (uploadResult.IsFailure)
            return Result.Failure<WorkspaceDto>(uploadResult.Error);

        try
        {
            workspace.UpdateAvatar(
                uploadResult.Value.PublicUrl,
                currentUserId,
                _clock.UtcNow);

            _workspaceRepository.Update(workspace);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    workspace.Id,
                    currentUserId,
                    ActivityAction.Update,
                    ActivityEntityType.Workspace,
                    workspace.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã cập nhật ảnh đại diện workspace '{workspace.Name}'."),
                cancellationToken);

            await _cache.RemoveAsync(
                WorkspaceCacheKeys.Detail(_cacheKeyFactory, workspace.Id),
                cancellationToken);

            await InvalidateWorkspaceListVersionsAsync(workspace.Id, cancellationToken);

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
                access.Role ?? (workspace.OwnerId == currentUserId ? WorkspaceRole.Owner : null),
                access.CanRead,
                access.CanWrite,
                access.CanManageMembers));
        }
        catch (DomainException ex)
        {
            return Result.Failure<WorkspaceDto>(new Error(
                "Workspace.UpdateAvatarFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }

    private async Task InvalidateWorkspaceListVersionsAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var members = await _workspaceMemberRepository.ListByWorkspaceAsync(
            workspaceId,
            cancellationToken);

        foreach (var userId in members.Select(x => x.UserId).Distinct())
        {
            var versionKey = WorkspaceCacheKeys.UserListVersion(_cacheKeyFactory, userId);
            await _cache.SetAsync(
                versionKey,
                Guid.NewGuid().ToString("N"),
                cancellationToken: cancellationToken);
        }
    }
}
