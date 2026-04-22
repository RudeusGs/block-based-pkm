namespace Pkm.Domain.Workspaces;
public enum WorkspaceRole
{
    // Các vai trò trong workspace, được sắp xếp theo thứ tự quyền hạn giảm dần
    Owner = 1,
    Manager = 2,
    Member = 3,
    Viewer = 4
}

