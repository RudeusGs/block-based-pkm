namespace Pkm.Domain.Recommendations;

/// <summary>
/// State of a task interaction history session.
/// Valid flow: InProgress -> Completed / Abandoned / Skipped.
/// </summary>
public enum StatusUserTaskHistory
{
    InProgress = 1,
    Completed = 2,
    Abandoned = 3,
    Skipped = 4
}
