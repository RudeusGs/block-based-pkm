namespace Pkm.Domain.Audit;

/// <summary>
/// Entity type affected by an activity log entry.
/// </summary>
public enum ActivityEntityType
{
    Workspace = 1,
    WorkspaceMember = 2,
    Page = 3,
    WorkTask = 4,
    TaskComment = 5,
    TaskAssignee = 6,
    User = 7,
    UserPreference = 8,
    RealtimeSession = 9,
    Block = 10
}
