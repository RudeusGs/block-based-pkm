using Pkm.Domain.Files;

namespace Pkm.Application.Common.Abstractions.Persistence;

public interface IStoredFileRepository
{
    Task<StoredFile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    void Add(StoredFile storedFile);
}
