using Pkm.Api.Contracts.Common;

namespace Pkm.Api.Mapping;

public static class FileUploadRequestMapper
{
    public static bool HasValidFile(IFormFile? file)
        => file is not null && file.Length > 0;

    public static ApiResult<TResponse> CreateFileRequiredFailure<TResponse>()
        => ApiResult<TResponse>.Failure(
            message: "Vui lòng chọn file ảnh để upload.",
            error: new ApiError(
                Code: "File.Required",
                Type: "validation_error",
                Details: new[] { "Trường file là bắt buộc." }),
            statusCode: StatusCodes.Status400BadRequest);

    public static string ResolveContentType(IFormFile file)
        => string.IsNullOrWhiteSpace(file.ContentType)
            ? "application/octet-stream"
            : file.ContentType;
}
