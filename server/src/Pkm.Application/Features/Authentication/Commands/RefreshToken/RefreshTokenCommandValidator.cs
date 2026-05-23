namespace Pkm.Application.Features.Authentication.Commands.RefreshToken;

public sealed class RefreshTokenCommandValidator
{
    public IReadOnlyList<string> Validate(RefreshTokenCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            errors.Add("RefreshToken không được để trống.");
        }

        return errors;
    }
}
