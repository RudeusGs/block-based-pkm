using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Tasks;
using Pkm.Application.Features.Recommendations.Models;
namespace Pkm.Application.Abstractions.Persistence;

public interface IWorkTaskRepository
{
    Task<WorkTask?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<WorkTask?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<WorkTaskDetailReadModel?> GetDetailAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkTaskListItemReadModel>> ListByWorkspaceAsync(
        Guid workspaceId,
        WorkTaskListFilter filter,
        CancellationToken cancellationToken = default);

    Task<int> CountByWorkspaceAsync(
        Guid workspaceId,
        WorkTaskListFilter filter,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkTaskListItemReadModel>> ListByPageAsync(
        Guid pageId,
        WorkTaskListFilter filter,
        CancellationToken cancellationToken = default);

    Task<int> CountByPageAsync(
        Guid pageId,
        WorkTaskListFilter filter,
        CancellationToken cancellationToken = default);

    Task<TaskAccessReadModel?> GetAccessContextAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<bool> HasActiveAssignedTaskAsync(
        Guid userId,
        Guid workspaceId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RecommendationCandidateReadModel>> ListRecommendationCandidatesAsync(
        Guid userId,
        Guid workspaceId,
        Guid? pageId,
        int take,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, RecommendationCandidateReadModel>> ListRecommendationTaskDetailsByIdsAsync(
        Guid userId,
        IReadOnlyCollection<Guid> taskIds,
        CancellationToken cancellationToken = default);
    void Add(WorkTask task);

    void Update(WorkTask task);
}