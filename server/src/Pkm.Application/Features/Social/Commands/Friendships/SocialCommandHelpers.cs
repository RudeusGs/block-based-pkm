using Pkm.Application.Common.Abstractions.Authentication;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Common.Abstractions.Realtime;
using Pkm.Application.Common.Abstractions.Time;
using Pkm.Application.Features.Social.Models;
using Pkm.Domain.Social;

namespace Pkm.Application.Features.Social.Commands;

internal static class SocialCommandHelpers
{
    private static readonly TimeSpan VersionTtl = TimeSpan.FromDays(7);

    public static async Task<FriendRequestDto> BuildFriendRequestDtoAsync(
        IUserRepository userRepository,
        FriendRequest request,
        Guid perspectiveUserId,
        CancellationToken cancellationToken)
    {
        var otherUserId = request.RequesterId == perspectiveUserId
            ? request.AddresseeId
            : request.RequesterId;

        var other = await userRepository.GetByIdAsync(otherUserId, cancellationToken)
            ?? await userRepository.GetByIdAsync(request.RequesterId, cancellationToken)
            ?? throw new InvalidOperationException("Cannot build friend request user summary.");

        return new FriendRequestDto(
            request.Id,
            request.RequesterId,
            request.AddresseeId,
            request.Status,
            new UserSummaryDto(other.Id, other.UserName, other.FullName, other.AvatarUrl),
            request.CreatedDate,
            request.RespondedAtUtc);
    }

    public static async Task InvalidateFriendListsAsync(
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        Guid userAId,
        Guid userBId,
        CancellationToken cancellationToken)
    {
        foreach (var userId in new[] { userAId, userBId }.Where(x => x != Guid.Empty).Distinct())
        {
            await cache.SetAsync(
                SocialCacheKeys.FriendListVersion(cacheKeyFactory, userId),
                Guid.NewGuid().ToString("N"),
                VersionTtl,
                cancellationToken);
        }
    }

    public static async Task PublishFriendEventAsync(
        ISocialRealtimePublisher realtimePublisher,
        IClock clock,
        Guid userId,
        string eventName,
        Guid actorId,
        object payload,
        CancellationToken cancellationToken)
    {
        await realtimePublisher.PublishToUserAsync(
            new SocialRealtimeEnvelope(eventName, userId, actorId, clock.UtcNow, payload),
            cancellationToken);
    }
}
