using Pkm.Application.Common.UseCases;
using Pkm.Application.Features.Social.Models;

namespace Pkm.Application.Features.Social.Queries;

public sealed record ListFriendsQuery(int PageNumber, int PageSize)
    : IQuery<FriendPagedResultDto>;
