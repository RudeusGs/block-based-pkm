using System.Text.Json;
using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using StackExchange.Redis;

namespace Pkm.Infrastructure.Realtime.Presence;

public sealed class RedisPagePresenceService : IPagePresenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IClock _clock;
    private readonly TimeSpan _ttl;

    public RedisPagePresenceService(
        IConnectionMultiplexer connectionMultiplexer,
        IClock clock,
        IOptions<RealtimeOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _clock = clock;
        _ttl = TimeSpan.FromSeconds(Math.Max(5, options.Value.PresenceTtlSeconds));
    }

    private IDatabase Database => _connectionMultiplexer.GetDatabase();

    public async Task<PagePresenceEntry> UpsertAsync(
        Guid pageId,
        Guid workspaceId,
        Guid userId,
        string? userName,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var entry = new PagePresenceEntry(
            PageId: pageId,
            WorkspaceId: workspaceId,
            UserId: userId,
            UserName: userName,
            ConnectionId: connectionId,
            LastSeenUtc: _clock.UtcNow);

        var payload = JsonSerializer.Serialize(entry, JsonOptions);
        var pageKey = PresencePageKey(pageId);
        var connKey = PresenceConnectionKey(connectionId);
        var score = entry.LastSeenUtc.ToUnixTimeMilliseconds();

        await Database.StringSetAsync(connKey, payload, _ttl);
        await Database.SortedSetAddAsync(pageKey, connectionId, score);
        await Database.KeyExpireAsync(pageKey, _ttl + _ttl);

        await CleanupExpiredAsync(pageId);

        return entry;
    }

    public async Task<PagePresenceEntry?> RemoveConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var connKey = PresenceConnectionKey(connectionId);
        var raw = await Database.StringGetAsync(connKey);

        PagePresenceEntry? removed = null;

        if (raw.HasValue)
        {
            removed = JsonSerializer.Deserialize<PagePresenceEntry>(raw!, JsonOptions);

            if (removed is not null)
            {
                await Database.SortedSetRemoveAsync(
                    PresencePageKey(removed.PageId),
                    connectionId);
            }
        }

        await Database.KeyDeleteAsync(connKey);

        return removed;
    }

    public async Task<IReadOnlyList<PagePresenceEntry>> GetActiveOnPageAsync(
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        await CleanupExpiredAsync(pageId);

        var threshold = (_clock.UtcNow - _ttl).ToUnixTimeMilliseconds();

        var members = await Database.SortedSetRangeByScoreAsync(
            PresencePageKey(pageId),
            start: threshold,
            stop: double.PositiveInfinity,
            exclude: Exclude.None,
            order: Order.Descending);

        if (members.Length == 0)
            return Array.Empty<PagePresenceEntry>();

        var keys = members
            .Select(m => (RedisKey)PresenceConnectionKey(m.ToString()!))
            .ToArray();

        var payloads = await Database.StringGetAsync(keys);

        var result = new List<PagePresenceEntry>(payloads.Length);

        foreach (var payload in payloads)
        {
            if (!payload.HasValue)
                continue;

            var entry = JsonSerializer.Deserialize<PagePresenceEntry>(payload!, JsonOptions);
            if (entry is null || entry.PageId != pageId)
                continue;

            result.Add(entry);
        }

        return result;
    }

    private async Task CleanupExpiredAsync(Guid pageId)
    {
        var threshold = (_clock.UtcNow - _ttl).ToUnixTimeMilliseconds();

        await Database.SortedSetRemoveRangeByScoreAsync(
            PresencePageKey(pageId),
            double.NegativeInfinity,
            threshold - 1);
    }

    private static string PresencePageKey(Guid pageId)
        => $"pkm:realtime:presence:page:{pageId:D}";

    private static string PresenceConnectionKey(string connectionId)
        => $"pkm:realtime:presence:conn:{connectionId}";
}