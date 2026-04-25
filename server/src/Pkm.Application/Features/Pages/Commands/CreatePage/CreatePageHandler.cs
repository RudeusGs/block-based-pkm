using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Common;
using Pkm.Domain.Pages;
using Pkm.Application.Features.Notifications;
using Pkm.Application.Features.Notifications.Services;
namespace Pkm.Application.Features.Pages.Commands.CreatePage;

public sealed class CreatePageHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPageRepository _pageRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly INotificationService _notificationService;
    public CreatePageHandler(
        ICurrentUser currentUser,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IPageAccessEvaluator pageAccessEvaluator,
        IPageRepository pageRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        INotificationService notificationService)
    {
        _currentUser = currentUser;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _pageAccessEvaluator = pageAccessEvaluator;
        _pageRepository = pageRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _notificationService = notificationService;
    }

    public async Task<Result<PageDto>> HandleAsync(
        CreatePageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WorkspaceId == Guid.Empty)
            return Result.Failure<PageDto>(WorkspaceErrors.InvalidWorkspaceId(request.WorkspaceId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PageDto>(PageErrors.MissingUserContext);

        var workspaceAccess = await _workspaceAccessEvaluator.EvaluateAsync(
            request.WorkspaceId,
            currentUserId,
            cancellationToken);

        if (!workspaceAccess.Exists)
            return Result.Failure<PageDto>(WorkspaceErrors.WorkspaceNotFound);

        if (request.ParentPageId.HasValue)
        {
            var parentPage = await _pageRepository.GetByIdAsync(
                request.ParentPageId.Value,
                cancellationToken);

            if (parentPage is null)
                return Result.Failure<PageDto>(PageErrors.ParentPageNotFound);

            if (parentPage.WorkspaceId != request.WorkspaceId)
                return Result.Failure<PageDto>(PageErrors.ParentPageDifferentWorkspace);

            var parentAccess = await _pageAccessEvaluator.EvaluateAsync(
                request.ParentPageId.Value,
                currentUserId,
                cancellationToken);

            if (!parentAccess.Exists)
                return Result.Failure<PageDto>(PageErrors.ParentPageNotFound);

            if (!parentAccess.CanCreateSubPage)
                return Result.Failure<PageDto>(PageErrors.PageForbidden);
        }
        else
        {
            if (!workspaceAccess.CanCreatePages)
                return Result.Failure<PageDto>(WorkspaceErrors.WorkspaceForbidden);
        }

        try
        {
            var now = _clock.UtcNow;

            var page = new Page(
                Guid.NewGuid(),
                request.WorkspaceId,
                request.Title,
                currentUserId,
                now,
                request.ParentPageId,
                request.Icon,
                request.CoverImage);

            _pageRepository.Add(page);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _notificationService.NotifyWorkspaceAsync(
                page.WorkspaceId,
                NotificationTemplates.PageCreated(
                    currentUserId,
                    _currentUser.UserName ?? "Có người",
                    page.WorkspaceId,
                    page.Id,
                    page.Title),
                excludeUserIds: new[] { currentUserId },
                cancellationToken);
            return Result.Success(page.ToDto());
        }
        catch (DomainException ex)
        {
            return Result.Failure<PageDto>(new Error(
                "Page.CreateFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }
}