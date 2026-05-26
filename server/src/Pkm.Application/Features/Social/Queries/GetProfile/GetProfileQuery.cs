using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;

namespace Pkm.Application.Features.Social.Queries;

public sealed record GetProfileQuery(
    Guid UserId,
    int WorkspacePageNumber = 1,
    int WorkspacePageSize = 20)
    : IQuery<UserProfilePageDto>;
