using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Recommendations;

public static class RecommendationErrors
{
    public static readonly Error MissingUserContext = new(
        "Recommendation.MissingUserContext",
        "Không xác định được user hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error WorkspaceNotFound = new(
        "Recommendation.WorkspaceNotFound",
        "Workspace không tồn tại hoặc bạn không có quyền truy cập.",
        ResultStatus.NotFound);

    public static readonly Error WorkspaceForbidden = new(
        "Recommendation.WorkspaceForbidden",
        "Bạn không có quyền xem recommendation trong workspace này.",
        ResultStatus.Forbidden);

    public static readonly Error RecommendationNotFound = new(
        "Recommendation.NotFound",
        "Không tìm thấy recommendation.",
        ResultStatus.NotFound);

    public static readonly Error RecommendationForbidden = new(
        "Recommendation.Forbidden",
        "Bạn không có quyền thao tác recommendation này.",
        ResultStatus.Forbidden);

    public static readonly Error TaskNotFound = new(
        "Recommendation.TaskNotFound",
        "Task được gợi ý không tồn tại.",
        ResultStatus.NotFound);

    public static readonly Error AlreadyHasActiveTasks = new(
        "Recommendation.AlreadyHasActiveTasks",
        "User đang có task active nên chưa cần gợi ý task mới.",
        ResultStatus.Conflict);

    public static readonly Error NotInRecommendationTime = new(
        "Recommendation.NotInRecommendationTime",
        "Hiện tại chưa nằm trong khung giờ/ngày được cấu hình để gợi ý task.",
        ResultStatus.Conflict);

    public static readonly Error AutoRecommendationDisabled = new(
        "Recommendation.AutoRecommendationDisabled",
        "Tính năng tự động gợi ý task đang bị tắt.",
        ResultStatus.Conflict);

    public static readonly Error Throttled = new(
        "Recommendation.Throttled",
        "Recommendation vừa được tạo gần đây. Vui lòng thử lại sau.",
        ResultStatus.Conflict);

    public static Error InvalidGenerateRequest(IReadOnlyList<string> details)
        => new(
            "Recommendation.InvalidGenerateRequest",
            "Dữ liệu tạo recommendation không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidListRequest(IReadOnlyList<string> details)
        => new(
            "Recommendation.InvalidListRequest",
            "Dữ liệu danh sách recommendation không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidPreferenceRequest(IReadOnlyList<string> details)
        => new(
            "Recommendation.InvalidPreferenceRequest",
            "Dữ liệu preference không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error OperationFailed(string message)
        => new(
            "Recommendation.OperationFailed",
            message,
            ResultStatus.Unprocessable);
}