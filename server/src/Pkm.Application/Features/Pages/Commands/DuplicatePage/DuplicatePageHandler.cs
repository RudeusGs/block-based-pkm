using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Activity.Services;
using Pkm.Application.Features.Pages.Models;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Application.Features.Workspaces;
using Pkm.Application.Features.Workspaces.Policies;
using Pkm.Domain.Audit;
using Pkm.Domain.Blocks;
using Pkm.Domain.Common;
using Pkm.Domain.Pages;

namespace Pkm.Application.Features.Pages.Commands.DuplicatePage;

public sealed class DuplicatePageHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IPageRepository _pageRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClock _clock;
    private readonly IActivityLogService _activityLogService;

    public DuplicatePageHandler(
        ICurrentUser currentUser,
        IPageAccessEvaluator pageAccessEvaluator,
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IPageRepository pageRepository,
        IBlockRepository blockRepository,
        IUnitOfWork unitOfWork,
        IClock clock,
        IActivityLogService activityLogService)
    {
        _currentUser = currentUser;
        _pageAccessEvaluator = pageAccessEvaluator;
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _pageRepository = pageRepository;
        _blockRepository = blockRepository;
        _unitOfWork = unitOfWork;
        _clock = clock;
        _activityLogService = activityLogService;
    }

    public async Task<Result<PageDto>> HandleAsync(DuplicatePageCommand request, CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure<PageDto>(PageErrors.InvalidPageId(request.PageId));

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PageDto>(PageErrors.MissingUserContext);

        var pageAccess = await _pageAccessEvaluator.EvaluateAsync(request.PageId, currentUserId, cancellationToken);
        if (!pageAccess.Exists)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        if (!pageAccess.CanRead)
            return Result.Failure<PageDto>(PageErrors.PageForbidden);

        var source = await _pageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (source is null)
            return Result.Failure<PageDto>(PageErrors.PageNotFound);

        var workspaceAccess = await _workspaceAccessEvaluator.EvaluateAsync(source.WorkspaceId, currentUserId, cancellationToken);
        if (!workspaceAccess.Exists)
            return Result.Failure<PageDto>(WorkspaceErrors.WorkspaceNotFound);

        if (!workspaceAccess.CanCreatePages)
            return Result.Failure<PageDto>(WorkspaceErrors.WorkspaceForbidden);

        var now = _clock.UtcNow;

        try
        {
            var copy = new Page(
                Guid.NewGuid(),
                source.WorkspaceId,
                BuildCopyTitle(source.Title),
                currentUserId,
                now,
                source.ParentPageId,
                source.Icon,
                source.CoverImage);

            _pageRepository.Add(copy);

            var blocks = await _blockRepository.ListByPageAsync(source.Id, cancellationToken);
            DuplicateBlocks(blocks, copy.Id, currentUserId, now);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            await _activityLogService.RecordAsync(
                new ActivityLogRequest(
                    copy.WorkspaceId,
                    currentUserId,
                    ActivityAction.Create,
                    ActivityEntityType.Page,
                    copy.Id,
                    $"{_currentUser.UserName ?? "Có người"} đã duplicate page \"{source.Title}\".",
                    ActivityLogMetadata.Serialize(new
                    {
                        sourcePageId = source.Id,
                        duplicatedPageId = copy.Id,
                        title = copy.Title
                    })),
                cancellationToken);

            return Result.Success(copy.ToDto());
        }
        catch (DomainException ex)
        {
            return Result.Failure<PageDto>(new Error("Page.DuplicateFailed", ex.Message, ResultStatus.Unprocessable));
        }
    }

    private void DuplicateBlocks(IReadOnlyList<Block> blocks, Guid newPageId, Guid actorId, DateTimeOffset now)
    {
        var idMap = new Dictionary<Guid, Guid>();
        var remaining = blocks.ToList();
        var guard = 0;

        while (remaining.Count > 0 && guard < blocks.Count + 5)
        {
            guard++;

            var copiedThisRound = 0;

            foreach (var block in remaining.ToArray())
            {
                if (block.ParentBlockId.HasValue && !idMap.ContainsKey(block.ParentBlockId.Value))
                    continue;

                var newBlockId = Guid.NewGuid();
                idMap[block.Id] = newBlockId;

                _blockRepository.Add(new Block(
                    newBlockId,
                    newPageId,
                    BlockTypeCode.From(block.Type.Value),
                    block.OrderKey,
                    actorId,
                    now,
                    block.TextContent,
                    block.PropsJson,
                    block.ParentBlockId.HasValue ? idMap[block.ParentBlockId.Value] : null,
                    block.SchemaVersion));

                remaining.Remove(block);
                copiedThisRound++;
            }

            if (copiedThisRound == 0)
                break;
        }
    }

    private static string BuildCopyTitle(string title)
    {
        var normalized = string.IsNullOrWhiteSpace(title) ? "Untitled" : title.Trim();
        var copyTitle = $"{normalized} Copy";
        return copyTitle.Length <= 200 ? copyTitle : copyTitle[..200];
    }
}
