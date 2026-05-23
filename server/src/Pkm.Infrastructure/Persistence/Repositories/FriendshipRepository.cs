using Microsoft.EntityFrameworkCore;
using Pkm.Application.Abstractions.Persistence;
using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Social;
using Pkm.Domain.Users;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class FriendshipRepository : IFriendshipRepository
{
    private readonly DataContext _context;

    public FriendshipRepository(DataContext context)
    {
        _context = context;
    }

    public Task<bool> AreFriendsAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default)
    {
        var pair = Friendship.OrderPair(userAId, userBId);
        return _context.Friendships
            .AsNoTracking()
            .AnyAsync(x => x.FirstUserId == pair.First && x.SecondUserId == pair.Second, cancellationToken);
    }

    public Task<Friendship?> GetFriendshipAsync(Guid userAId, Guid userBId, CancellationToken cancellationToken = default)
    {
        var pair = Friendship.OrderPair(userAId, userBId);
        return _context.Friendships
            .FirstOrDefaultAsync(x => x.FirstUserId == pair.First && x.SecondUserId == pair.Second, cancellationToken);
    }

    public Task<FriendRequest?> GetPendingRequestAsync(Guid requesterId, Guid addresseeId, CancellationToken cancellationToken = default)
    {
        return _context.FriendRequests
            .FirstOrDefaultAsync(
                x => x.RequesterId == requesterId &&
                     x.AddresseeId == addresseeId &&
                     x.Status == FriendRequestStatus.Pending,
                cancellationToken);
    }

    public Task<FriendRequest?> GetRequestByIdAsync(Guid requestId, CancellationToken cancellationToken = default)
    {
        return _context.FriendRequests
            .FirstOrDefaultAsync(x => x.Id == requestId, cancellationToken);
    }

    public async Task<IReadOnlyList<UserSearchResultDto>> SearchUsersAsync(
        Guid viewerUserId,
        string keyword,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var skip = (pageNumber - 1) * pageSize;
        keyword = (keyword ?? string.Empty).Trim();

        return await _context.Users
            .AsNoTracking()
            .Where(x => x.Status == UserStatus.Active)
            .Where(x => EF.Functions.ILike(x.FullName, $"%{keyword}%") || EF.Functions.ILike(x.UserName, $"%{keyword}%"))
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.UserName)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new UserSearchResultDto(
                x.Id,
                x.UserName,
                x.FullName,
                x.AvatarUrl,
                x.Id == viewerUserId,
                x.Id == viewerUserId
                    ? "self"
                    : _context.Friendships.AsNoTracking().Any(f =>
                        (f.FirstUserId == viewerUserId && f.SecondUserId == x.Id) ||
                        (f.SecondUserId == viewerUserId && f.FirstUserId == x.Id))
                        ? "friends"
                        : _context.FriendRequests.AsNoTracking().Any(r =>
                            r.RequesterId == viewerUserId &&
                            r.AddresseeId == x.Id &&
                            r.Status == FriendRequestStatus.Pending)
                            ? "request_sent"
                            : _context.FriendRequests.AsNoTracking().Any(r =>
                                r.RequesterId == x.Id &&
                                r.AddresseeId == viewerUserId &&
                                r.Status == FriendRequestStatus.Pending)
                                ? "request_received"
                                : "none"))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FriendDto>> ListFriendsAsync(
        Guid userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var skip = (pageNumber - 1) * pageSize;

        return await _context.Friendships
            .AsNoTracking()
            .Where(x => x.FirstUserId == userId || x.SecondUserId == userId)
            .Select(x => new
            {
                FriendUserId = x.FirstUserId == userId ? x.SecondUserId : x.FirstUserId,
                FriendsSinceUtc = x.CreatedDate
            })
            .Join(
                _context.Users.AsNoTracking(),
                friendship => friendship.FriendUserId,
                user => user.Id,
                (friendship, user) => new
                {
                    UserId = user.Id,
                    user.UserName,
                    user.FullName,
                    user.AvatarUrl,
                    friendship.FriendsSinceUtc
                })
            .OrderBy(x => x.FullName)
            .ThenBy(x => x.UserName)
            .Skip(skip)
            .Take(pageSize)
            .Select(x => new FriendDto(
                x.UserId,
                x.UserName,
                x.FullName,
                x.AvatarUrl,
                x.FriendsSinceUtc))
            .ToListAsync(cancellationToken);
    }

    public Task<int> CountFriendsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Friendships
            .AsNoTracking()
            .CountAsync(x => x.FirstUserId == userId || x.SecondUserId == userId, cancellationToken);
    }

    public Task<IReadOnlyList<FriendRequestDto>> ListIncomingRequestsAsync(Guid addresseeId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        => ListRequestsAsync(addresseeId, incoming: true, pageNumber, pageSize, cancellationToken);

    public Task<IReadOnlyList<FriendRequestDto>> ListOutgoingRequestsAsync(Guid requesterId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        => ListRequestsAsync(requesterId, incoming: false, pageNumber, pageSize, cancellationToken);

    public void AddRequest(FriendRequest request) => _context.FriendRequests.Add(request);

    public void AddFriendship(Friendship friendship) => _context.Friendships.Add(friendship);

    public void UpdateRequest(FriendRequest request) => _context.FriendRequests.Update(request);

    public void RemoveFriendship(Friendship friendship) => _context.Friendships.Remove(friendship);

    private async Task<IReadOnlyList<FriendRequestDto>> ListRequestsAsync(
        Guid userId,
        bool incoming,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken)
    {
        pageNumber = pageNumber <= 0 ? 1 : pageNumber;
        pageSize = pageSize <= 0 ? 20 : Math.Min(pageSize, 100);
        var skip = (pageNumber - 1) * pageSize;

        var query = _context.FriendRequests
            .AsNoTracking()
            .Where(x => x.Status == FriendRequestStatus.Pending);

        query = incoming
            ? query.Where(x => x.AddresseeId == userId)
            : query.Where(x => x.RequesterId == userId);

        return await query
            .OrderByDescending(x => x.CreatedDate)
            .Skip(skip)
            .Take(pageSize)
            .Join(
                _context.Users.AsNoTracking(),
                request => incoming ? request.RequesterId : request.AddresseeId,
                user => user.Id,
                (request, user) => new FriendRequestDto(
                    request.Id,
                    request.RequesterId,
                    request.AddresseeId,
                    request.Status,
                    new UserSummaryDto(user.Id, user.UserName, user.FullName, user.AvatarUrl),
                    request.CreatedDate,
                    request.RespondedAtUtc))
            .ToListAsync(cancellationToken);
    }
}
