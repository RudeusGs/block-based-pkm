namespace Pkm.Domain.Recommendations;

/// <summary>
/// State of a task recommendation.
/// </summary>
public enum StatusTaskRecommendation
{
    Pending = 1,
    Accepted = 2,
    Rejected = 3,
    Completed = 4,
    Expired = 5
}
