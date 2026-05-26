using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;

namespace Pkm.Application.Features.Social.Queries;

public sealed record ListIncomingFriendRequestsQuery(int PageNumber, int PageSize)
    : IQuery<IReadOnlyList<FriendRequestDto>>;
