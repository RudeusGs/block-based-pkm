namespace Pkm.Application.Features.Account.Commands.ChangeMyPassword;

public sealed class ChangeMyPasswordCommandValidator
{
    public IReadOnlyList<string> Validate(ChangeMyPasswordCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.CurrentPassword))
        {
            errors.Add("Mật khẩu hiện tại không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(command.NewPassword))
        {
            errors.Add("Mật khẩu mới không được để trống.");
        }

        return errors;
    }
}
