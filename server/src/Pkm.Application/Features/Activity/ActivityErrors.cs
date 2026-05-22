using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Activity;

public static class ActivityErrors
{
    public static readonly Error MissingUserContext = new(
        "Activity.MissingUserContext",
        "Không xác định được người dùng hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error ActivityForbidden = new(
        "Activity.Forbidden",
        "Không có quyền xem activity log của workspace này.",
        ResultStatus.Forbidden);

    public static Error InvalidListRequest(IReadOnlyList<string> details)
        => new(
            "Activity.InvalidListRequest",
            "Dữ liệu lọc activity log không hợp lệ.",
            ResultStatus.Validation,
            details);
}
