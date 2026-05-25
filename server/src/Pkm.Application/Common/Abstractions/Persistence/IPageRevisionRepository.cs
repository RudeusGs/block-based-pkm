using Pkm.Domain.Pages;

namespace Pkm.Application.Common.Abstractions.Persistence;

public interface IPageRevisionRepository
{
    void Add(PageRevision revision);
}
