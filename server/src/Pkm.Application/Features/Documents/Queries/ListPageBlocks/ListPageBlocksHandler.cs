using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Common.Results;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Pages.Policies;

namespace Pkm.Application.Features.Documents.Queries.ListPageBlocks;

public sealed class ListPageBlocksHandler
{
    private readonly ICurrentUser _currentUser;
    private readonly IPageRepository _pageRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;

    public ListPageBlocksHandler(
        ICurrentUser currentUser,
        IPageRepository pageRepository,
        IBlockRepository blockRepository,
        IPageAccessEvaluator pageAccessEvaluator)
    {
        _currentUser = currentUser;
        _pageRepository = pageRepository;
        _blockRepository = blockRepository;
        _pageAccessEvaluator = pageAccessEvaluator;
    }

    public async Task<Result<PageDocumentDto>> HandleAsync(
        ListPageBlocksQuery request,
        CancellationToken cancellationToken)
    {
        if (request.PageId == Guid.Empty)
            return Result.Failure<PageDocumentDto>(DocumentErrors.InvalidPageId);

        if (!_currentUser.TryGetUserId(out var currentUserId))
            return Result.Failure<PageDocumentDto>(DocumentErrors.MissingUserContext);

        var access = await _pageAccessEvaluator.EvaluateAsync(
            request.PageId,
            currentUserId,
            cancellationToken);

        if (!access.Exists)
            return Result.Failure<PageDocumentDto>(DocumentErrors.PageNotFound);

        if (!access.CanRead)
            return Result.Failure<PageDocumentDto>(DocumentErrors.PageForbidden);

        var page = await _pageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (page is null)
            return Result.Failure<PageDocumentDto>(DocumentErrors.PageNotFound);

        var blocks = await _blockRepository.ListByPageAsync(request.PageId, cancellationToken);

        return Result.Success(new PageDocumentDto(
            page.Id,
            page.CurrentRevision,
            blocks.Select(x => x.ToDto()).ToArray()));
    }
}