using Pkm.Application.Features.Tasks.Models;
using Pkm.Domain.Tasks;

namespace Pkm.Application.Abstractions.Persistence;

public interface IWorkTaskRepository
{
    Task<WorkTask?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<TaskAccessReadModel?> GetAccessContextAsync(
        Guid taskId,
        Guid userId,
        CancellationToken cancellationToken = default);
}