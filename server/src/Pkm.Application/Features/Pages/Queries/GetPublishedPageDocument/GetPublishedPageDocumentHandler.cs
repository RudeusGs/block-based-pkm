using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Common.Pagination;
using Pkm.Application.Common.Results;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Application.Features.Pages.Queries.GetPublishedPageDocument;

public sealed class GetPublishedPageDocumentHandler : IQueryHandler<GetPublishedPageDocumentQuery, PublishedPageDocumentDto>
{
    private const int DefaultBlockPageSize = 200;
    private const int MaxBlockPageSize = 500;

    private readonly IPageReadRepository _pageRepository;
    private readonly IBlockReadRepository _blockRepository;

    public GetPublishedPageDocumentHandler(
        IPageReadRepository pageRepository,
        IBlockReadRepository blockRepository)
    {
        _pageRepository = pageRepository;
        _blockRepository = blockRepository;
    }

    public async Task<Result<PublishedPageDocumentDto>> HandleAsync(
        GetPublishedPageDocumentQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.PublicToken))
            return Result.Failure<PublishedPageDocumentDto>(PageErrors.PublishedPageNotFound);

        var pageEntity = await _pageRepository.GetPublishedByTokenAsync(
            request.PublicToken.Trim(),
            cancellationToken);

        if (pageEntity is null || pageEntity.PublishedAt is null)
            return Result.Failure<PublishedPageDocumentDto>(PageErrors.PublishedPageNotFound);

        var page = PageRequest.Normalize(
            request.PageNumber,
            request.PageSize,
            DefaultBlockPageSize,
            MaxBlockPageSize);

        var blocks = await _blockRepository.ListByPageAsync(
            pageEntity.Id,
            page.PageNumber,
            page.PageSize,
            cancellationToken);

        var totalCount = await _blockRepository.CountByPageAsync(
            pageEntity.Id,
            cancellationToken);

        return Result.Success(new PublishedPageDocumentDto(
            pageEntity.Id,
            pageEntity.Title,
            pageEntity.Icon,
            pageEntity.CoverImage,
            pageEntity.CurrentRevision,
            pageEntity.PublishedAt.Value,
            blocks.Select(x => x.ToDto()).ToArray(),
            page.PageNumber,
            page.PageSize,
            totalCount,
            PageRequest.CalculateTotalPages(totalCount, page.PageSize)));
    }
}
