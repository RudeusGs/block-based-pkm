namespace Pkm.Domain.Recommendations;

/// <summary>
/// Trạng thái của một recommendation.
/// </summary>
public enum StatusTaskRecommendation
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Completed = 4,
    Expired = 5
}