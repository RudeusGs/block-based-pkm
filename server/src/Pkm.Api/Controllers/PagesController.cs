using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Pages;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Pages;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Features.Documents.Queries.GetPagePresence;
using Pkm.Application.Features.Pages.Commands.CreatePage;
using Pkm.Application.Features.Pages.Commands.DeletePage;
using Pkm.Application.Features.Pages.Commands.UpdatePageMetadata;
using Pkm.Application.Features.Pages.Queries.GetPage;
using Pkm.Application.Features.Pages.Queries.ListSubPages;
using Pkm.Application.Features.Pages.Queries.ListWorkspacePages;
using Pkm.Application.Features.Pages.Queries.SearchPages;

namespace Pkm.Api.Controllers;

[Authorize]
public sealed class PagesController : BaseController
{
    private readonly CreatePageHandler _createPageHandler;
    private readonly UpdatePageMetadataHandler _updatePageMetadataHandler;
    private readonly DeletePageHandler _deletePageHandler;
    private readonly GetPageHandler _getPageHandler;
    private readonly GetPagePresenceHandler _getPagePresenceHandler;
    private readonly ListWorkspacePagesHandler _listWorkspacePagesHandler;
    private readonly ListSubPagesHandler _listSubPagesHandler;
    private readonly SearchPagesHandler _searchPagesHandler;

    public PagesController(
        ICurrentUser currentUser,
        CreatePageHandler createPageHandler,
        UpdatePageMetadataHandler updatePageMetadataHandler,
        DeletePageHandler deletePageHandler,
        GetPageHandler getPageHandler,
        GetPagePresenceHandler getPagePresenceHandler,
        ListWorkspacePagesHandler listWorkspacePagesHandler,
        ListSubPagesHandler listSubPagesHandler,
        SearchPagesHandler searchPagesHandler)
        : base(currentUser)
    {
        _createPageHandler = createPageHandler;
        _updatePageMetadataHandler = updatePageMetadataHandler;
        _deletePageHandler = deletePageHandler;
        _getPageHandler = getPageHandler;
        _getPagePresenceHandler = getPagePresenceHandler;
        _listWorkspacePagesHandler = listWorkspacePagesHandler;
        _listSubPagesHandler = listSubPagesHandler;
        _searchPagesHandler = searchPagesHandler;
    }

    [HttpPost("api/v1/workspaces/{workspaceId:guid}/pages")]
    [ProducesResponseType(typeof(ApiResult<PageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<PageResponse>>> Create(
        [FromRoute] Guid workspaceId,
        [FromBody] CreatePageRequest request,
        CancellationToken cancellationToken)
    {
        var command = new CreatePageCommand(
            workspaceId,
            request.Title,
            request.ParentPageId,
            request.Icon,
            request.CoverImage);

        var result = await _createPageHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPatch("api/v1/pages/{pageId:guid}")]
    [ProducesResponseType(typeof(ApiResult<PageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<PageResponse>>> UpdateMetadata(
        [FromRoute] Guid pageId,
        [FromBody] UpdatePageMetadataRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdatePageMetadataCommand(
            pageId,
            request.Title,
            request.Icon,
            request.CoverImage);

        var result = await _updatePageMetadataHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpDelete("api/v1/pages/{pageId:guid}")]
    [ProducesResponseType(typeof(ApiResult), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult>> Delete(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await _deletePageHandler.HandleAsync(
            new DeletePageCommand(pageId),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("api/v1/pages/{pageId:guid}")]
    [ProducesResponseType(typeof(ApiResult<PageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PageResponse>>> GetById(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await _getPageHandler.HandleAsync(
            new GetPageQuery(pageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/pages/{pageId:guid}/presence")]
    [ProducesResponseType(typeof(ApiResult<PagePresenceResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PagePresenceResponse>>> GetPresence(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await _getPagePresenceHandler.HandleAsync(
            new GetPagePresenceQuery(pageId),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/workspaces/{workspaceId:guid}/pages")]
    [ProducesResponseType(typeof(ApiResult<PagePagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PagePagedResultResponse>>> ListByWorkspace(
        [FromRoute] Guid workspaceId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _listWorkspacePagesHandler.HandleAsync(
            new ListWorkspacePagesQuery(workspaceId, pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }

    [HttpGet("api/v1/pages/{pageId:guid}/subpages")]
    [ProducesResponseType(typeof(ApiResult<IReadOnlyList<PageResponse>>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<IReadOnlyList<PageResponse>>>> GetSubPages(
        [FromRoute] Guid pageId,
        CancellationToken cancellationToken)
    {
        var result = await _listSubPagesHandler.HandleAsync(
            new ListSubPagesQuery(pageId),
            cancellationToken);

        return HandleResult(result, x => (IReadOnlyList<PageResponse>)x.Select(y => y.ToResponse()).ToArray());
    }

    [HttpGet("api/v1/workspaces/{workspaceId:guid}/pages:search")]
    [ProducesResponseType(typeof(ApiResult<PagePagedResultResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    public async Task<ActionResult<ApiResult<PagePagedResultResponse>>> Search(
        [FromRoute] Guid workspaceId,
        [FromQuery] string keyword,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _searchPagesHandler.HandleAsync(
            new SearchPagesQuery(workspaceId, keyword, pageNumber, pageSize),
            cancellationToken);

        return HandleResult(result, x => x.ToResponse());
    }
}