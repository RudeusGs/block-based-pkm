namespace Pkm.Application.Features.Authentication.Commands.ForgotPassword;

public sealed class ForgotPasswordCommandValidator
{
    public IReadOnlyList<string> Validate(ForgotPasswordCommand request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Email))
        {
            errors.Add("Email không được để trống.");
        }
        else if (!request.Email.Contains('@', StringComparison.Ordinal) || !request.Email.Contains('.', StringComparison.Ordinal))
        {
            errors.Add("Email không đúng định dạng.");
        }

        return errors;
    }
}
