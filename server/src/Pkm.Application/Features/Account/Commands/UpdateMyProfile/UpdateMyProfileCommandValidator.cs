namespace Pkm.Application.Features.Account.Commands.UpdateMyProfile;

public sealed class UpdateMyProfileCommandValidator
{
    public IReadOnlyList<string> Validate(UpdateMyProfileCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.FullName))
        {
            errors.Add("Họ tên không được để trống.");
        }

        return errors;
    }
}
