using Pkm.Domain.Pages;

namespace Pkm.Application.Abstractions.Persistence;

public interface IPageRevisionRepository
{
    void Add(PageRevision revision);
}