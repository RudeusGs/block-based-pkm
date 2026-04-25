using Pkm.Application.Features.Recommendations.Models;
using Pkm.Domain.Recommendations;

namespace Pkm.Application.Abstractions.Persistence;

public interface ITaskRecommendationRepository
{
    Task<TaskRecommendation?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TaskRecommendation?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskRecommendation>> ListByUserAsync(
        Guid userId,
        Guid? workspaceId,
        StatusTaskRecommendation? status,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<int> CountByUserAsync(
        Guid userId,
        Guid? workspaceId,
        StatusTaskRecommendation? status,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskRecommendation>> ListPendingByUserAndWorkspaceAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingForTaskAsync(
        Guid userId,
        Guid taskId,
        CancellationToken cancellationToken = default);

    void Add(TaskRecommendation recommendation);

    void AddRange(IEnumerable<TaskRecommendation> recommendations);

    void Update(TaskRecommendation recommendation);
}