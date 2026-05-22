using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Domain.Files;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class StoredFileRepository : IStoredFileRepository
{
    private readonly DataContext _dataContext;

    public StoredFileRepository(DataContext dataContext)
    {
        _dataContext = dataContext;
    }

    public async Task<StoredFile?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => await _dataContext.StoredFiles
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public void Add(StoredFile storedFile)
        => _dataContext.StoredFiles.Add(storedFile);
}
