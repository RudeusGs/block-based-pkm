using Pkm.Application.Abstractions.Persistence;
using Pkm.Domain.Pages;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class PageRevisionRepository : IPageRevisionRepository
{
    private readonly DataContext _dbContext;

    public PageRevisionRepository(DataContext dbContext)
    {
        _dbContext = dbContext;
    }

    public void Add(PageRevision revision)
        => _dbContext.PageRevisions.Add(revision);
}