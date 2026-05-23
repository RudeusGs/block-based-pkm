using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Account;

public static class AccountErrors
{
    public static readonly Error InvalidCurrentPassword = new(
        "Account.InvalidCurrentPassword",
        "Mật khẩu hiện tại không đúng.",
        ResultStatus.Unauthorized);

    public static readonly Error NewPasswordSameAsCurrent = new(
        "Account.NewPasswordSameAsCurrent",
        "Mật khẩu mới phải khác mật khẩu hiện tại.",
        ResultStatus.Validation);

    public static readonly Error InvalidProfileData = new(
        "Account.InvalidProfileData",
        "Dữ liệu profile không hợp lệ.",
        ResultStatus.Unprocessable);

    public static Error InvalidUpdateProfileRequest(IReadOnlyList<string> details)
        => new(
            "Account.InvalidUpdateProfileRequest",
            "Dữ liệu cập nhật profile không hợp lệ.",
            ResultStatus.Validation,
            details);

    public static Error InvalidChangePasswordRequest(IReadOnlyList<string> details)
        => new(
            "Account.InvalidChangePasswordRequest",
            "Dữ liệu đổi mật khẩu không hợp lệ.",
            ResultStatus.Validation,
            details);
}
