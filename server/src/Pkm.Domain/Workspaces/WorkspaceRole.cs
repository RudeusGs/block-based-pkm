namespace Pkm.Domain.Workspaces;

public enum WorkspaceRole
{
    // Roles are ordered from highest to lowest authority.
    Owner = 1,
    Manager = 2,
    Member = 3,
    Viewer = 4
}
