namespace Pkm.Domain.Audit;

/// <summary>
/// Action type recorded in the activity log.
/// </summary>
public enum ActivityAction
{
    Create = 1,
    Update = 2,
    Delete = 3,

    Archive = 4,
    Restore = 5,

    Move = 6,
    Assign = 7,
    Unassign = 8,

    Complete = 9,
    Reopen = 10,

    Login = 11,
    ChangePermissions = 12
}
