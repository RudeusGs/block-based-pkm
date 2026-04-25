using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Notifications;

public static class NotificationErrors
{
    public static readonly Error MissingUserContext = new(
        "Notification.MissingUserContext",
        "Không xác định được người dùng hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error NotificationNotFound = new(
        "Notification.NotFound",
        "Không tìm thấy thông báo.",
        ResultStatus.NotFound);

    public static readonly Error NotificationForbidden = new(
        "Notification.Forbidden",
        "Không có quyền thao tác thông báo này.",
        ResultStatus.Forbidden);

    public static Error InvalidListRequest(IReadOnlyList<string> details)
        => new(
            "Notification.InvalidListRequest",
            "Dữ liệu danh sách thông báo không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidReadRequest(IReadOnlyList<string> details)
        => new(
            "Notification.InvalidReadRequest",
            "Dữ liệu đánh dấu đã đọc không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidDeleteRequest(IReadOnlyList<string> details)
        => new(
            "Notification.InvalidDeleteRequest",
            "Dữ liệu xóa thông báo không hợp lệ.",
            ResultStatus.Validation,
            details);
}