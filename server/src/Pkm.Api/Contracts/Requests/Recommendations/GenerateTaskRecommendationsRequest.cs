namespace Pkm.Api.Contracts.Requests.Recommendations;

public sealed record GenerateTaskRecommendationsRequest(
    Guid? PageId = null,
    bool Force = false);