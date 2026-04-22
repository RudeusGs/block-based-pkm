using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Abstractions.Persistence;

public interface ITaskAssigneeRepository
{
    Task<TaskAssignee?> GetByTaskAndUserAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskAssigneeReadModel>> ListByTaskAsync(
        Guid taskId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, IReadOnlyList<Guid>>> ListUserIdsByTaskIdsAsync(
        IReadOnlyCollection<Guid> taskIds,
        CancellationToken cancellationToken = default);

    void Add(TaskAssignee assignee);

    void Remove(TaskAssignee assignee);
}