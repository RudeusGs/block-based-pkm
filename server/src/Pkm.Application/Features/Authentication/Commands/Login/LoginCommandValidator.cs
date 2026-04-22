namespace Pkm.Application.Features.Authentication.Commands.Login;

public sealed class LoginCommandValidator
{
    public IReadOnlyList<string> Validate(LoginCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.UserName))
        {
            errors.Add("Tên đăng nhập không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors.Add("Mật khẩu không được để trống.");
        }

        return errors;
    }
}
