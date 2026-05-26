using Microsoft.EntityFrameworkCore;
using Pkm.Application.Common.Abstractions.Persistence;
using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Profiles;
using Pkm.Domain.Social;

namespace Pkm.Infrastructure.Persistence.Repositories;

internal sealed class UserProfileRepository : IUserProfileRepository
{
    private readonly DataContext _context;

    public UserProfileRepository(DataContext context)
    {
        _context = context;
    }

    public Task<UserProfilePage?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.UserProfilePages.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public Task<UserProfilePage?> GetByUserIdForUpdateAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.UserProfilePages.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

    public async Task<UserProfilePageDto?> GetProfileAsync(
        Guid viewedUserId,
        Guid viewerUserId,
        IReadOnlyList<ProfileWorkspaceDto> workspaces,
        int workspacePageNumber,
        int workspacePageSize,
        int workspaceTotalCount,
        int workspaceTotalPages,
        CancellationToken cancellationToken = default)
    {
        var data = await (
            from user in _context.Users.AsNoTracking()
            where user.Id == viewedUserId
            join profile in _context.UserProfilePages.AsNoTracking()
                on user.Id equals profile.UserId into profileGroup
            from profile in profileGroup.DefaultIfEmpty()
            select new
            {
                user.Id,
                user.UserName,
                user.FullName,
                user.AvatarUrl,
                Bio = profile != null ? profile.Bio : null,
                CoverImageUrl = profile != null ? profile.CoverImageUrl : null,
                CreatedDate = profile != null ? profile.CreatedDate : user.CreatedDate,
                UpdatedDate = profile != null ? profile.UpdatedDate : user.UpdatedDate
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data is null)
            return null;

        var friendCount = await _context.Friendships
            .AsNoTracking()
            .CountAsync(x => x.FirstUserId == viewedUserId || x.SecondUserId == viewedUserId, cancellationToken);

        var friendshipStatus = viewedUserId == viewerUserId
            ? "self"
            : await _context.Friendships.AsNoTracking().AnyAsync(x =>
                (x.FirstUserId == viewedUserId && x.SecondUserId == viewerUserId) ||
                (x.FirstUserId == viewerUserId && x.SecondUserId == viewedUserId), cancellationToken)
                ? "friends"
                : await _context.FriendRequests.AsNoTracking().AnyAsync(x =>
                    x.RequesterId == viewerUserId &&
                    x.AddresseeId == viewedUserId &&
                    x.Status == FriendRequestStatus.Pending, cancellationToken)
                    ? "request_sent"
                    : await _context.FriendRequests.AsNoTracking().AnyAsync(x =>
                        x.RequesterId == viewedUserId &&
                        x.AddresseeId == viewerUserId &&
                        x.Status == FriendRequestStatus.Pending, cancellationToken)
                        ? "request_received"
                        : "none";

        return new UserProfilePageDto(
            data.Id,
            data.UserName,
            data.FullName,
            data.AvatarUrl,
            data.Bio,
            data.CoverImageUrl,
            friendshipStatus,
            friendCount,
            workspaces,
            workspacePageNumber,
            workspacePageSize,
            workspaceTotalCount,
            workspaceTotalPages,
            data.CreatedDate,
            data.UpdatedDate);
    }

    public void Add(UserProfilePage profile) => _context.UserProfilePages.Add(profile);

    public void Update(UserProfilePage profile) => _context.UserProfilePages.Update(profile);
}
