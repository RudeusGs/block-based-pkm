namespace Pkm.Domain.Recommendations;

/// <summary>
/// Trạng thái của một session lịch sử tương tác task.
/// Flow hiện tại: InProgress -> Completed / Abandoned / Skipped
/// </summary>
public enum StatusUserTaskHistory
{
    InProgress = 1,
    Completed = 2,
    Abandoned = 3,
    Skipped = 4
}