using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Workspaces.Commands.AddWorkspaceMember;

public sealed class AddWorkspaceMemberCommandValidator
{
    public IReadOnlyList<string> Validate(AddWorkspaceMemberCommand command)
    {
        var errors = new List<string>();

        if (command.WorkspaceId == Guid.Empty)
        {
            errors.Add("WorkspaceId không hợp lệ.");
        }

        if (command.UserId == Guid.Empty)
        {
            errors.Add("UserId không hợp lệ.");
        }

        if (!Enum.IsDefined(typeof(WorkspaceRole), command.Role))
        {
            errors.Add("Vai trò không hợp lệ.");
        }

        return errors;
    }
}