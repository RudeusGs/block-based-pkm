using Pkm.Application.Common.UseCases;

namespace Pkm.Application.Features.Authentication.Queries.GetUserRoles;

public sealed record GetUserRolesQuery(Guid UserId) : IQuery<IEnumerable<string>>;
