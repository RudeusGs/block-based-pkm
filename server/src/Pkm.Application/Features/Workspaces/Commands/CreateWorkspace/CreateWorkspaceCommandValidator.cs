namespace Pkm.Application.Features.Workspaces.Commands.CreateWorkspace;

public sealed class CreateWorkspaceCommandValidator
{
    public IReadOnlyList<string> Validate(CreateWorkspaceCommand command)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(command.Name))
        {
            errors.Add("Tên workspace không được để trống.");
        }

        return errors;
    }
}