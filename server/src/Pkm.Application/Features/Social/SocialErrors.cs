using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Social;

public static class SocialErrors
{
    public static readonly Error MissingUserContext = new(
        "Social.MissingUserContext",
        "Không xác định được người dùng hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error UserNotFound = new(
        "Social.UserNotFound",
        "Không tìm thấy người dùng.",
        ResultStatus.NotFound);

    public static readonly Error CannotFriendYourself = new(
        "Social.CannotFriendYourself",
        "Không thể kết bạn với chính mình.",
        ResultStatus.Unprocessable);

    public static readonly Error AlreadyFriends = new(
        "Social.AlreadyFriends",
        "Hai người đã là bạn bè rồi.",
        ResultStatus.Conflict);

    public static readonly Error FriendRequestAlreadyPending = new(
        "Social.FriendRequestAlreadyPending",
        "Lời mời kết bạn đang chờ xử lý.",
        ResultStatus.Conflict);

    public static readonly Error FriendRequestNotFound = new(
        "Social.FriendRequestNotFound",
        "Không tìm thấy lời mời kết bạn.",
        ResultStatus.NotFound);

    public static readonly Error FriendRequestForbidden = new(
        "Social.FriendRequestForbidden",
        "Bạn không có quyền xử lý lời mời kết bạn này.",
        ResultStatus.Forbidden);

    public static readonly Error FriendshipNotFound = new(
        "Social.FriendshipNotFound",
        "Hai người chưa phải bạn bè.",
        ResultStatus.NotFound);

    public static Error InvalidRequest(IReadOnlyList<string> details)
        => new(
            "Social.InvalidRequest",
            "Dữ liệu mạng xã hội không hợp lệ.",
            ResultStatus.Validation,
            details);
}
