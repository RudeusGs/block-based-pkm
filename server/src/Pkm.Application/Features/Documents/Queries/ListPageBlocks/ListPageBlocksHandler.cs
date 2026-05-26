using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Pages.Policies;

namespace Pkm.Application.Features.Documents.Queries.ListPageBlocks;

public sealed class ListPageBlocksHandler : IQueryHandler<ListPageBlocksQuery, PageDocumentDto>
{
    private const int DefaultBlockPageSize = 50;
    private const int MaxBlockPageSize = 200;

    private readonly ICurrentUser _currentUser;
    private readonly IPageReadRepository _pageRepository;
    private readonly IBlockReadRepository _blockRepository;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;

    public ListPageBlocksHandler(
        ICurrentUser currentUser,
        IPageReadRepository pageRepository,
        IBlockReadRepository blockRepository,
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

        var pageEntity = await _pageRepository.GetByIdAsync(request.PageId, cancellationToken);
        if (pageEntity is null)
            return Result.Failure<PageDocumentDto>(DocumentErrors.PageNotFound);

        var page = PageRequest.Normalize(request.PageNumber, request.PageSize, DefaultBlockPageSize, MaxBlockPageSize);

        var blocks = await _blockRepository.ListByPageAsync(
            request.PageId,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        var totalCount = await _blockRepository.CountByPageAsync(
            request.PageId,
            cancellationToken);

        return Result.Success(new PageDocumentDto(
            pageEntity.Id,
            pageEntity.CurrentRevision,
            blocks.Select(x => x.ToDto()).ToArray(),
            page.PageNumber,
            page.PageSize,
            totalCount,
            PageRequest.CalculateTotalPages(totalCount, page.PageSize)));
    }
}
