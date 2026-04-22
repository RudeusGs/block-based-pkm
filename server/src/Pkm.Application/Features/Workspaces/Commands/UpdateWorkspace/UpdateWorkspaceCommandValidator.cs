namespace Pkm.Application.Features.Workspaces.Commands.UpdateWorkspace;

public sealed class UpdateWorkspaceCommandValidator
{
    public IReadOnlyList<string> Validate(UpdateWorkspaceCommand command)
    {
        var errors = new List<string>();

        if (command.WorkspaceId == Guid.Empty)
        {
            errors.Add("WorkspaceId không hợp lệ.");
        }

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Tên workspace không được để trống.");
        }

        return errors;
    }
}