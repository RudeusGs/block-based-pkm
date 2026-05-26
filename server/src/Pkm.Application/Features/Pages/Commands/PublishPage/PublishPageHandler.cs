using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Application.Features.Pages.Realtime;
using Pkm.Application.Features.Pages.Services;
using Pkm.Domain.Audit;
using Pkm.Domain.SharedKernel;

namespace Pkm.Application.Features.Pages.Commands.PublishPage;

public sealed class PublishPageHandler : ICommandHandler<PublishPageCommand, PagePublishDto>
{
    private const int MaxTokenGenerationAttempts = 5;

    private readonly ICurrentUser _currentUser;
    private readonly IPageReadRepository _pageReadRepository;
    private readonly IPageWriteRepository _pageWriteRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IPagePublishTokenGenerator _tokenGenerator;
    private readonly IPageRealtimePublisher _pageRealtimePublisher;
    private readonly IActivityLogService _activityLogService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;

    public PublishPageHandler(
        ICurrentUser currentUser,
        IPageReadRepository pageReadRepository,
        IPageWriteRepository pageWriteRepository,
        IPageAccessEvaluator pageAccessEvaluator,
        IPagePublishTokenGenerator tokenGenerator,
        IPageRealtimePublisher pageRealtimePublisher,
        IActivityLogService activityLogService,
        IUnitOfWork unitOfWork,
        IClock clock)
    {
        _currentUser = currentUser;
        _pageReadRepository = pageReadRepository;
        _pageWriteRepository = pageWriteRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
        _tokenGenerator = tokenGenerator;
        _pageRealtimePublisher = pageRealtimePublisher;
        _activityLogService = activityLogService;
        _unitOfWork = unitOfWork;
        _clock = clock;
    }

    public async Task<Result<PagePublishDto>> HandleAsync(
        PublishPageCommand request,
        CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure<PagePublishDto>(PageErrors.InvalidPageId(request.PageId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PagePublishDto>(PageErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(
            request.PageId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<PagePublishDto>(PageErrors.PageNotFound);

        if (!access.CanManagePage)
            return Result.Failure<PagePublishDto>(PageErrors.PageForbidden);

        var page = await _pageWriteRepository.GetByIdForUpdateAsync(
            request.PageId,
            cancellationToken);

        if (page is null)
            return Result.Failure<PagePublishDto>(PageErrors.PageNotFound);

        if (page.IsPublished && !string.IsNullOrWhiteSpace(page.PublicToken))
            return Result.Success(ToPublishDto(page));

        var token = await CreateUniqueTokenAsync(cancellationToken);
        if (token is null)
            return Result.Failure<PagePublishDto>(PageErrors.PublishTokenGenerationFailed);

        try
        {
            var now = _clock.UtcNow;
            page.Publish(token, currentUserId, now);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var pageDto = page.ToDto();

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    page.WorkspaceId,
                    currentUserId,
                    ActivityAction.Update,
                    ActivityEntityType.Page,
                    page.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã publish page \"{page.Title}\" bằng public link.",
                    ActivityLogMetadata.Serialize(new
                    {
                        pageId = page.Id,
                        isPublished = true,
                        publishedAt = page.PublishedAt
                    })),
                cancellationToken);

            await _pageRealtimePublisher.PublishWorkspacePageChangedAsync(
                PageRealtimeEventNames.MetadataUpdated,
                pageDto,
                currentUserId,
                now,
                cancellationToken);

            return Result.Success(ToPublishDto(page));
        }
        catch (DomainException ex)
        {
            return Result.Failure<PagePublishDto>(new Error(
                "Page.PublishFailed",
                ex.Message,
                ResultStatus.Unprocessable));
        }
    }

    private async Task<string?> CreateUniqueTokenAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < MaxTokenGenerationAttempts; attempt++)
        {
            var token = _tokenGenerator.CreateToken();
            var exists = await _pageReadRepository.ExistsByPublicTokenAsync(token, cancellationToken);

            if (!exists)
                return token;
        }

        return null;
    }

    private static PagePublishDto ToPublishDto(Pkm.Domain.Pages.Page page)
        => new(
            page.Id,
            page.IsPublished,
            page.PublicToken,
            page.PublishedAt,
            page.PublishedBy);
}
