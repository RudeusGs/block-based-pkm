using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Files;

public static class FileErrors
{
    public static readonly Error MissingUserContext = new(
        "File.MissingUserContext",
        "Không xác định được người dùng hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error MissingFile = new(
        "File.MissingFile",
        "Vui lòng chọn file để upload.",
        ResultStatus.Validation);

    public static readonly Error EmptyFile = new(
        "File.EmptyFile",
        "File upload không được rỗng.",
        ResultStatus.Validation);

    public static readonly Error ImageTooLarge = new(
        "File.ImageTooLarge",
        "Ảnh quá lớn. Vui lòng chọn ảnh nhỏ hơn 8MB.",
        ResultStatus.Validation);

    public static readonly Error UnsupportedImageType = new(
        "File.UnsupportedImageType",
        "Chỉ hỗ trợ ảnh PNG, JPG, JPEG, WEBP hoặc GIF.",
        ResultStatus.Validation);

    public static Error UploadFailed(string message)
        => new(
            "File.UploadFailed",
            message,
            ResultStatus.Failure);
}
