using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Tasks;

public static class TaskErrors
{
    public static readonly Error MissingUserContext = new(
        "Task.MissingUserContext",
        "Không xác định được người dùng hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error TaskNotFound = new(
        "Task.NotFound",
        "Không tìm thấy task.",
        ResultStatus.NotFound);

    public static readonly Error TaskForbidden = new(
        "Task.Forbidden",
        "Không có quyền thao tác task này.",
        ResultStatus.Forbidden);

    public static readonly Error PageNotFound = new(
        "Task.PageNotFound",
        "Không tìm thấy page.",
        ResultStatus.NotFound);

    public static readonly Error PageArchived = new(
        "Task.PageArchived",
        "Không thể thao tác task trên page đã archive.",
        ResultStatus.Unprocessable);

    public static readonly Error PageDifferentWorkspace = new(
        "Task.PageDifferentWorkspace",
        "Page không thuộc cùng workspace với task.",
        ResultStatus.Unprocessable);

    public static readonly Error AssigneeAlreadyExists = new(
        "Task.AssigneeAlreadyExists",
        "Người dùng đã được giao vào task này.",
        ResultStatus.Conflict);

    public static readonly Error AssigneeNotFound = new(
        "Task.AssigneeNotFound",
        "Người dùng chưa được giao vào task này.",
        ResultStatus.NotFound);

    public static readonly Error AssigneeNotInWorkspace = new(
        "Task.AssigneeNotInWorkspace",
        "Người dùng không thuộc workspace của task.",
        ResultStatus.Unprocessable);

    public static readonly Error InvalidStatus = new(
        "Task.InvalidStatus",
        "Trạng thái task không hợp lệ.",
        ResultStatus.Validation);

    public static readonly Error InvalidPriority = new(
        "Task.InvalidPriority",
        "Độ ưu tiên task không hợp lệ.",
        ResultStatus.Validation);

    public static readonly Error InvalidKeyword = new(
        "Task.InvalidKeyword",
        "Từ khóa tìm kiếm không hợp lệ.",
        ResultStatus.Validation);

    public static Error InvalidTaskId(Guid taskId)
        => new(
            "Task.InvalidTaskId",
            $"TaskId '{taskId}' không hợp lệ.",
            ResultStatus.Validation);

    public static Error InvalidPageId(Guid pageId)
        => new(
            "Task.InvalidPageId",
            $"PageId '{pageId}' không hợp lệ.",
            ResultStatus.Validation);

    public static Error InvalidWorkspaceId(Guid workspaceId)
        => new(
            "Task.InvalidWorkspaceId",
            $"WorkspaceId '{workspaceId}' không hợp lệ.",
            ResultStatus.Validation);

    public static Error InvalidAssigneeUserId(Guid userId)
        => new(
            "Task.InvalidAssigneeUserId",
            $"UserId '{userId}' không hợp lệ.",
            ResultStatus.Validation);

    public static Error InvalidCreateRequest(IReadOnlyList<string> details)
        => new(
            "Task.InvalidCreateRequest",
            "Dữ liệu tạo task không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidUpdateRequest(IReadOnlyList<string> details)
        => new(
            "Task.InvalidUpdateRequest",
            "Dữ liệu cập nhật task không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidAssignRequest(IReadOnlyList<string> details)
        => new(
            "Task.InvalidAssignRequest",
            "Dữ liệu giao task không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidStatusChangeRequest(IReadOnlyList<string> details)
        => new(
            "Task.InvalidStatusChangeRequest",
            "Dữ liệu đổi trạng thái task không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidListRequest(IReadOnlyList<string> details)
        => new(
            "Task.InvalidListRequest",
            "Bộ lọc danh sách task không hợp lệ.",
            ResultStatus.Validation,
            details);
}