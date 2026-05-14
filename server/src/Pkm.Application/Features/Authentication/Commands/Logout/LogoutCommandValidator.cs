namespace Pkm.Application.Features.Authentication.Commands.Logout;

public sealed class LogoutCommandValidator
{
    public IReadOnlyList<string> Validate(LogoutCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            errors.Add("RefreshToken không được để trống.");
        }

        return errors;
    }
}