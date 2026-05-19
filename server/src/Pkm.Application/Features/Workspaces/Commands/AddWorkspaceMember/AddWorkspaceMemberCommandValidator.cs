using System.Text.RegularExpressions;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;

public sealed class AddWorkspaceMemberCommandValidator
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public IReadOnlyList<string> Validate(AddWorkspaceMemberCommand command)
    {
        var errors = new List<string>();

        if (command.WorkspaceId == Guid.Empty)
        {
            errors.Add("WorkspaceId không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(command.Email))
        {
            errors.Add("Email không được để trống.");
        }
        else if (!EmailRegex.IsMatch(command.Email.Trim()))
        {
            errors.Add("Email không đúng định dạng.");
        }

        if (!Enum.IsDefined(typeof(WorkspaceRole), command.Role))
        {
            errors.Add("Vai trò không hợp lệ.");
        }

        return errors;
    }
}