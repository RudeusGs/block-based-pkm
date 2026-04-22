using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Authentication;

public static class AuthenticationErrors
{
    public static readonly Error InvalidCredentials = new(
        "Auth.InvalidCredentials",
        "Thông tin đăng nhập không hợp lệ.",
        ResultStatus.Unauthorized);

    public static readonly Error MissingUserContext = new(
        "Auth.MissingUserContext",
        "Không xác định được người dùng hiện tại.",
        ResultStatus.Unauthorized);

    public static readonly Error DuplicateUserName = new(
        "Auth.DuplicateUserName",
        "Tên đăng nhập đã tồn tại.",
        ResultStatus.Conflict);

    public static readonly Error DuplicateEmail = new(
        "Auth.DuplicateEmail",
        "Email đã được sử dụng.",
        ResultStatus.Conflict);

    public static readonly Error InvalidRegisterData = new(
        "Auth.InvalidRegisterData",
        "Dữ liệu đăng ký không hợp lệ.",
        ResultStatus.Unprocessable);

    public static readonly Error InvalidUserId = new(
        "Auth.InvalidUserId",
        "UserId không hợp lệ.",
        ResultStatus.Validation);

    public static readonly Error UserNotFound = new(
        "Auth.UserNotFound",
        "Không tìm thấy người dùng.",
        ResultStatus.NotFound);

    public static Error InvalidLoginRequest(IReadOnlyList<string> details)
        => new(
            "Auth.InvalidLoginRequest",
            "Dữ liệu đăng nhập không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidRegisterRequest(IReadOnlyList<string> details)
        => new(
            "Auth.InvalidRegisterRequest",
            "Dữ liệu đăng ký không hợp lệ.",
            ResultStatus.Validation,
            details);
}
