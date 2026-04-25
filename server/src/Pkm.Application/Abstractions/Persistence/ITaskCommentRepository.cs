using Pkm.Domain.Tasks;

namespace Pkm.Application.Abstractions.Persistence;

public interface ITaskCommentRepository
{
    Task<TaskComment?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TaskComment?> GetByIdIncludingDeletedAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TaskComment?> GetByIdForUpdateAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<TaskComment?> GetByIdForUpdateIncludingDeletedAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TaskComment>> ListByTaskAsync(
        Guid taskId,
        int pageNumber,
        int pageSize,
        bool includeDeleted = true,
        CancellationToken cancellationToken = default);

    Task<int> CountByTaskAsync(
        Guid taskId,
        bool includeDeleted = true,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsInTaskAsync(
        Guid commentId,
        Guid taskId,
        bool includeDeleted = false,
        CancellationToken cancellationToken = default);

    void Add(TaskComment comment);

    void Update(TaskComment comment);
}