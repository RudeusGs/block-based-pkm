using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Requests.Files;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Account;
using Pkm.Api.Contracts.Responses.Files;
using Pkm.Api.Contracts.Responses.Pages;
using Pkm.Api.Mapping;
using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Account.Models;
using Pkm.Application.Features.Files.Commands.UploadImage;
using Pkm.Application.Features.Files.Commands.UploadMyAvatarImage;
using Pkm.Application.Features.Files.Commands.UploadPageCoverImage;
using Pkm.Application.Features.Files.Models;
using Pkm.Application.Features.Pages.Models;

namespace Pkm.Api.Controllers;

[Authorize]
public sealed class FilesController : BaseController
{
    private const long MaxRequestBodySizeBytes = 10 * 1024 * 1024;

    public FilesController(
        ICurrentUser currentUser,
        IUseCaseDispatcher dispatcher)
        : base(currentUser, dispatcher)
    {
    }

    [HttpPost("api/v1/files/images")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBodySizeBytes)]
    [ProducesResponseType(typeof(ApiResult<StoredFileResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 413)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<StoredFileResponse>>> UploadImage(
        [FromForm] UploadImageFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!FileUploadRequestMapper.HasValidFile(request.File))
            return BadRequest(FileUploadRequestMapper.CreateFileRequiredFailure<StoredFileResponse>());

        var file = request.File!;

        await using var stream = file.OpenReadStream();

        var command = new UploadImageCommand(
            file.FileName,
            FileUploadRequestMapper.ResolveContentType(file),
            file.Length,
            stream,
            request.Purpose);

        var result = await ExecuteAsync<UploadImageCommand, StoredFileDto>(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/me/avatar-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBodySizeBytes)]
    [ProducesResponseType(typeof(ApiResult<UserProfileResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 413)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<UserProfileResponse>>> UploadMyAvatarImage(
        [FromForm] UploadAvatarImageFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!FileUploadRequestMapper.HasValidFile(request.File))
            return BadRequest(FileUploadRequestMapper.CreateFileRequiredFailure<UserProfileResponse>());

        var file = request.File!;

        await using var stream = file.OpenReadStream();

        var command = new UploadMyAvatarImageCommand(
            file.FileName,
            FileUploadRequestMapper.ResolveContentType(file),
            file.Length,
            stream);

        var result = await ExecuteAsync<UploadMyAvatarImageCommand, UserProfileDto>(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    [HttpPost("api/v1/pages/{pageId:guid}/cover-image")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxRequestBodySizeBytes)]
    [ProducesResponseType(typeof(ApiResult<PageResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult), 400)]
    [ProducesResponseType(typeof(ApiResult), 401)]
    [ProducesResponseType(typeof(ApiResult), 403)]
    [ProducesResponseType(typeof(ApiResult), 404)]
    [ProducesResponseType(typeof(ApiResult), 413)]
    [ProducesResponseType(typeof(ApiResult), 422)]
    public async Task<ActionResult<ApiResult<PageResponse>>> UploadPageCoverImage(
        [FromRoute] Guid pageId,
        [FromForm] UploadPageCoverImageFormRequest request,
        CancellationToken cancellationToken)
    {
        if (!FileUploadRequestMapper.HasValidFile(request.File))
            return BadRequest(FileUploadRequestMapper.CreateFileRequiredFailure<PageResponse>());

        var file = request.File!;

        await using var stream = file.OpenReadStream();

        var command = new UploadPageCoverImageCommand(
            pageId,
            request.ExpectedRevision,
            file.FileName,
            FileUploadRequestMapper.ResolveContentType(file),
            file.Length,
            stream);

        var result = await ExecuteAsync<UploadPageCoverImageCommand, PageDto>(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

}
