using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pkm.Api.Contracts.Common;
using Pkm.Api.Contracts.Responses;
using Pkm.Api.Contracts.Responses.Account;
using Pkm.Api.Contracts.Responses.Files;
using Pkm.Api.Contracts.Responses.Pages;
using Pkm.Application.Abstractions.Authentication;
using Pkm.Application.Features.Files.Commands.UploadImage;
using Pkm.Application.Features.Files.Commands.UploadMyAvatarImage;
using Pkm.Application.Features.Files.Commands.UploadPageCoverImage;

namespace Pkm.Api.Controllers;

[Authorize]
public sealed class FilesController : BaseController
{
    private const long MaxRequestBodySizeBytes = 10 * 1024 * 1024;

    private readonly UploadImageHandler _uploadImageHandler;
    private readonly UploadMyAvatarImageHandler _uploadMyAvatarImageHandler;
    private readonly UploadPageCoverImageHandler _uploadPageCoverImageHandler;

    public FilesController(
        ICurrentUser currentUser,
        UploadImageHandler uploadImageHandler,
        UploadMyAvatarImageHandler uploadMyAvatarImageHandler,
        UploadPageCoverImageHandler uploadPageCoverImageHandler)
        : base(currentUser)
    {
        _uploadImageHandler = uploadImageHandler;
        _uploadMyAvatarImageHandler = uploadMyAvatarImageHandler;
        _uploadPageCoverImageHandler = uploadPageCoverImageHandler;
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
        if (!HasValidFile(request.File))
            return BadRequest(CreateFileRequiredFailure<StoredFileResponse>());

        var file = request.File!;

        await using var stream = file.OpenReadStream();

        var command = new UploadImageCommand(
            file.FileName,
            ResolveContentType(file),
            file.Length,
            stream,
            request.Purpose);

        var result = await _uploadImageHandler.HandleAsync(command, cancellationToken);
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
        if (!HasValidFile(request.File))
            return BadRequest(CreateFileRequiredFailure<UserProfileResponse>());

        var file = request.File!;

        await using var stream = file.OpenReadStream();

        var command = new UploadMyAvatarImageCommand(
            file.FileName,
            ResolveContentType(file),
            file.Length,
            stream);

        var result = await _uploadMyAvatarImageHandler.HandleAsync(command, cancellationToken);
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
        if (!HasValidFile(request.File))
            return BadRequest(CreateFileRequiredFailure<PageResponse>());

        var file = request.File!;

        await using var stream = file.OpenReadStream();

        var command = new UploadPageCoverImageCommand(
            pageId,
            request.ExpectedRevision,
            file.FileName,
            ResolveContentType(file),
            file.Length,
            stream);

        var result = await _uploadPageCoverImageHandler.HandleAsync(command, cancellationToken);
        return HandleResult(result, x => x.ToResponse());
    }

    private static bool HasValidFile(IFormFile? file)
        => file is not null && file.Length > 0;

    private static ApiResult<TResponse> CreateFileRequiredFailure<TResponse>()
        => ApiResult<TResponse>.Failure(
            message: "Vui lòng chọn file ảnh để upload.",
            error: new ApiError(
                Code: "File.Required",
                Type: "validation_error",
                Details: new[] { "Trường file là bắt buộc." }),
            statusCode: StatusCodes.Status400BadRequest);

    private static string ResolveContentType(IFormFile file)
        => string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;
}

public sealed class UploadImageFormRequest
{
    public IFormFile? File { get; init; }
    public string? Purpose { get; init; }
}

public sealed class UploadAvatarImageFormRequest
{
    public IFormFile? File { get; init; }
}

public sealed class UploadPageCoverImageFormRequest
{
    public IFormFile? File { get; init; }
    public long? ExpectedRevision { get; init; }
}
