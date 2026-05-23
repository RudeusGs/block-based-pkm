using System.Text.Json;
using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using StackExchange.Redis;

namespace Pkm.Infrastructure.Realtime.Presence;

public sealed class RedisWorkspacePresenceService : IWorkspacePresenceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IClock _clock;
    private readonly TimeSpan _ttl;

    public RedisWorkspacePresenceService(
        IConnectionMultiplexer connectionMultiplexer,
        IClock clock,
        IOptions<RealtimeOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _clock = clock;
        _ttl = TimeSpan.FromSeconds(Math.Max(5, options.Value.PresenceTtlSeconds));
    }

    private IDatabase Database => _connectionMultiplexer.GetDatabase();

    public async Task<WorkspacePresenceEntry> UpsertAsync(
        Guid workspaceId,
        Guid userId,
        string? userName,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var entry = new WorkspacePresenceEntry(
            WorkspaceId: workspaceId,
            UserId: userId,
            UserName: userName,
            ConnectionId: connectionId,
            LastSeenUtc: _clock.UtcNow);

        var payload = JsonSerializer.Serialize(entry, JsonOptions);
        var entryKey = PresenceEntryKey(workspaceId, connectionId);
        var workspaceKey = PresenceWorkspaceKey(workspaceId);
        var connectionIndexKey = PresenceConnectionIndexKey(connectionId);
        var score = entry.LastSeenUtc.ToUnixTimeMilliseconds();

        await Database.StringSetAsync(entryKey, payload, _ttl);
        await Database.SortedSetAddAsync(workspaceKey, connectionId, score);
        await Database.SetAddAsync(connectionIndexKey, workspaceId.ToString("D"));

        await Database.KeyExpireAsync(workspaceKey, _ttl + _ttl);
        await Database.KeyExpireAsync(connectionIndexKey, _ttl + _ttl);

        await CleanupExpiredAsync(workspaceId);

        return entry;
    }

    public async Task<WorkspacePresenceEntry?> RemoveConnectionFromWorkspaceAsync(
        Guid workspaceId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var entryKey = PresenceEntryKey(workspaceId, connectionId);
        var raw = await Database.StringGetAsync(entryKey);

        WorkspacePresenceEntry? removed = null;

        if (raw.HasValue)
        {
            removed = JsonSerializer.Deserialize<WorkspacePresenceEntry>(raw!, JsonOptions);
        }

        await Database.SortedSetRemoveAsync(PresenceWorkspaceKey(workspaceId), connectionId);
        await Database.SetRemoveAsync(PresenceConnectionIndexKey(connectionId), workspaceId.ToString("D"));
        await Database.KeyDeleteAsync(entryKey);

        return removed;
    }

    public async Task<IReadOnlyList<WorkspacePresenceEntry>> RemoveConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var connectionIndexKey = PresenceConnectionIndexKey(connectionId);
        var workspaceIds = await Database.SetMembersAsync(connectionIndexKey);

        if (workspaceIds.Length == 0)
            return Array.Empty<WorkspacePresenceEntry>();

        var removed = new List<WorkspacePresenceEntry>();

        foreach (var rawWorkspaceId in workspaceIds)
        {
            if (!Guid.TryParse(rawWorkspaceId.ToString(), out var workspaceId))
                continue;

            var entry = await RemoveConnectionFromWorkspaceAsync(
                workspaceId,
                connectionId,
                cancellationToken);

            if (entry is not null)
            {
                removed.Add(entry);
            }
        }

        await Database.KeyDeleteAsync(connectionIndexKey);

        return removed;
    }

    public async Task<IReadOnlyList<WorkspacePresenceEntry>> GetActiveOnWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        await CleanupExpiredAsync(workspaceId);

        var threshold = (_clock.UtcNow - _ttl).ToUnixTimeMilliseconds();

        var members = await Database.SortedSetRangeByScoreAsync(
            PresenceWorkspaceKey(workspaceId),
            start: threshold,
            stop: double.PositiveInfinity,
            exclude: Exclude.None,
            order: Order.Descending);

        if (members.Length == 0)
            return Array.Empty<WorkspacePresenceEntry>();

        var keys = members
            .Select(connectionId => (RedisKey)PresenceEntryKey(workspaceId, connectionId.ToString()!))
            .ToArray();

        var payloads = await Database.StringGetAsync(keys);

        var result = new List<WorkspacePresenceEntry>(payloads.Length);

        foreach (var payload in payloads)
        {
            if (!payload.HasValue)
                continue;

            var entry = JsonSerializer.Deserialize<WorkspacePresenceEntry>(payload!, JsonOptions);

            if (entry is null || entry.WorkspaceId != workspaceId)
                continue;

            result.Add(entry);
        }

        return result;
    }

    private async Task CleanupExpiredAsync(Guid workspaceId)
    {
        var threshold = (_clock.UtcNow - _ttl).ToUnixTimeMilliseconds();

        await Database.SortedSetRemoveRangeByScoreAsync(
            PresenceWorkspaceKey(workspaceId),
            double.NegativeInfinity,
            threshold - 1);
    }

    private static string PresenceWorkspaceKey(Guid workspaceId)
        => $"pkm:realtime:presence:workspace:{workspaceId:D}";

    private static string PresenceEntryKey(Guid workspaceId, string connectionId)
        => $"pkm:realtime:presence:workspace:{workspaceId:D}:conn:{connectionId}";

    private static string PresenceConnectionIndexKey(string connectionId)
        => $"pkm:realtime:presence:workspace:conn-index:{connectionId}";
}
