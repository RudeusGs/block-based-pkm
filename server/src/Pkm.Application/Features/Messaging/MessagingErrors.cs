using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Messaging;

public static class MessagingErrors
{
    public static readonly Error MissingUserContext = new(
        "Messaging.MissingUserContext",
        "Không xác định được người dùng hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error RecipientNotFound = new(
        "Messaging.RecipientNotFound",
        "Không tìm thấy người nhận.",
        ResultStatus.NotFound);

    public static readonly Error CannotMessageYourself = new(
        "Messaging.CannotMessageYourself",
        "Không thể nhắn tin cho chính mình.",
        ResultStatus.Unprocessable);

    public static readonly Error FriendshipRequired = new(
        "Messaging.FriendshipRequired",
        "Chỉ bạn bè mới có thể nhắn tin riêng với nhau.",
        ResultStatus.Forbidden);

    public static readonly Error ConversationNotFound = new(
        "Messaging.ConversationNotFound",
        "Không tìm thấy cuộc trò chuyện.",
        ResultStatus.NotFound);

    public static readonly Error ConversationForbidden = new(
        "Messaging.ConversationForbidden",
        "Bạn không thuộc cuộc trò chuyện này.",
        ResultStatus.Forbidden);

    public static readonly Error EmptyMessage = new(
        "Messaging.EmptyMessage",
        "Tin nhắn không được để trống.",
        ResultStatus.Validation);

    public static Error InvalidRequest(IReadOnlyList<string> details)
        => new(
            "Messaging.InvalidRequest",
            "Dữ liệu nhắn tin không hợp lệ.",
            ResultStatus.Validation,
            details);
}
