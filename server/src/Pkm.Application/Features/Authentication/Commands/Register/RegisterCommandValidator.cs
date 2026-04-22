namespace Pkm.Application.Features.Authentication.Commands.Register;

public sealed class RegisterCommandValidator
{
    public IReadOnlyList<string> Validate(RegisterCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.UserName))
        {
            errors.Add("Tên đăng nhập không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.Add("Email không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            errors.Add("Họ tên không được để trống.");
        }

        if (string.IsNullOrWhiteSpace(command.Password))
        {
            errors.Add("Mật khẩu không được để trống.");
        }

        return errors;
    }
}
