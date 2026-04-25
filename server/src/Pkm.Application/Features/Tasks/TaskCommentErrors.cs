using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Tasks;

public static class TaskCommentErrors
{
    public static readonly Error CommentNotFound = new(
        "TaskComment.NotFound",
        "Không tìm thấy bình luận.",
        ResultStatus.NotFound);

    public static readonly Error ParentCommentNotFound = new(
        "TaskComment.ParentNotFound",
        "Không tìm thấy bình luận cha.",
        ResultStatus.NotFound);

    public static readonly Error CommentForbidden = new(
        "TaskComment.Forbidden",
        "Không có quyền thao tác bình luận này.",
        ResultStatus.Forbidden);

    public static readonly Error CommentCreateForbidden = new(
        "TaskComment.CreateForbidden",
        "Không có quyền bình luận trong task này.",
        ResultStatus.Forbidden);

    public static readonly Error CommentReadForbidden = new(
        "TaskComment.ReadForbidden",
        "Không có quyền xem bình luận của task này.",
        ResultStatus.Forbidden);

    public static readonly Error CommentModerateForbidden = new(
        "TaskComment.ModerateForbidden",
        "Không có quyền kiểm duyệt bình luận này.",
        ResultStatus.Forbidden);

    public static Error InvalidCreateRequest(IReadOnlyList<string> details)
        => new(
            "TaskComment.InvalidCreateRequest",
            "Dữ liệu tạo bình luận không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidUpdateRequest(IReadOnlyList<string> details)
        => new(
            "TaskComment.InvalidUpdateRequest",
            "Dữ liệu cập nhật bình luận không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidDeleteRequest(IReadOnlyList<string> details)
        => new(
            "TaskComment.InvalidDeleteRequest",
            "Dữ liệu xóa bình luận không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidRestoreRequest(IReadOnlyList<string> details)
        => new(
            "TaskComment.InvalidRestoreRequest",
            "Dữ liệu khôi phục bình luận không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidListRequest(IReadOnlyList<string> details)
        => new(
            "TaskComment.InvalidListRequest",
            "Dữ liệu danh sách bình luận không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error OperationFailed(string message)
        => new(
            "TaskComment.OperationFailed",
            message,
            ResultStatus.Unprocessable);
}