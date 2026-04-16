using server.Domain.Entities;

namespace server.Application.Models.Internal
{
    public sealed record PageAccessContext(Page Page, int UserId);

    public sealed record BlockAccessContext(Block Block, Page Page, int UserId);
}
