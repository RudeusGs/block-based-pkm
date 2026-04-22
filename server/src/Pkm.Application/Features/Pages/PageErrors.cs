using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Pages;

public static class PageErrors
{
    public static readonly Error MissingUserContext = new(
        "Page.MissingUserContext",
        "Không xác định được người dùng hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error PageNotFound = new(
        "Page.NotFound",
        "Không tìm thấy page.",
        ResultStatus.NotFound);

    public static readonly Error PageForbidden = new(
        "Page.Forbidden",
        "Không có quyền truy cập page này.",
        ResultStatus.Forbidden);

    public static readonly Error ParentPageNotFound = new(
        "Page.ParentPageNotFound",
        "Không tìm thấy parent page.",
        ResultStatus.NotFound);

    public static readonly Error ParentPageDifferentWorkspace = new(
        "Page.ParentPageDifferentWorkspace",
        "Parent page không thuộc cùng workspace.",
        ResultStatus.Unprocessable);

    public static readonly Error InvalidSearchKeyword = new(
        "Page.InvalidSearchKeyword",
        "Từ khóa tìm kiếm không hợp lệ.",
        ResultStatus.Validation);

    public static Error InvalidPageId(Guid pageId)
        => new(
            "Page.InvalidPageId",
            $"PageId '{pageId}' không hợp lệ.",
            ResultStatus.Validation);
}