using Pkm.Domain.Files;

namespace Pkm.Application.Abstractions.Persistence;

public interface IStoredFileRepository
{
    Task<StoredFile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    void Add(StoredFile storedFile);
}
