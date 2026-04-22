using System.Text.Json;
using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using StackExchange.Redis;

namespace Pkm.Infrastructure.Realtime.Locks;

public sealed class RedisBlockEditLeaseService : IBlockEditLeaseService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly IClock _clock;
    private readonly TimeSpan _ttl;

    public RedisBlockEditLeaseService(
        IConnectionMultiplexer connectionMultiplexer,
        IClock clock,
        IOptions<RealtimeOptions> options)
    {
        _connectionMultiplexer = connectionMultiplexer;
        _clock = clock;
        _ttl = TimeSpan.FromSeconds(Math.Max(5, options.Value.BlockLeaseTtlSeconds));
    }

    private IDatabase Database => _connectionMultiplexer.GetDatabase();

    public async Task<BlockLeaseResult> AcquireAsync(
        Guid blockId,
        Guid pageId,
        Guid userId,
        string connectionId,
        string? holderDisplayName = null,
        CancellationToken cancellationToken = default)
    {
        var existingBlockRaw = await Database.StringGetAsync(ConnectionLeaseKey(connectionId));
        if (existingBlockRaw.HasValue &&
            Guid.TryParse(existingBlockRaw.ToString(), out var existingBlockId) &&
            existingBlockId != blockId)
        {
            await ReleaseAsync(existingBlockId, userId, connectionId, cancellationToken);
        }

        var now = _clock.UtcNow;
        var lease = new BlockLeaseInfo(
            BlockId: blockId,
            PageId: pageId,
            UserId: userId,
            ConnectionId: connectionId,
            AcquiredAtUtc: now,
            ExpiresAtUtc: now.Add(_ttl),
            HolderDisplayName: holderDisplayName);

        var payload = JsonSerializer.Serialize(lease, JsonOptions);
        var granted = await Database.StringSetAsync(
            BlockLeaseKey(blockId),
            payload,
            _ttl,
            when: When.NotExists);

        if (granted)
        {
            await Database.StringSetAsync(
                ConnectionLeaseKey(connectionId),
                blockId.ToString("D"),
                _ttl);

            return new BlockLeaseResult(
                Granted: true,
                Lease: lease,
                CurrentHolder: lease);
        }

        var current = await GetCurrentAsync(blockId, cancellationToken);

        if (current is not null &&
            current.ConnectionId == connectionId &&
            current.UserId == userId)
        {
            await Database.StringSetAsync(BlockLeaseKey(blockId), payload, _ttl);
            await Database.StringSetAsync(ConnectionLeaseKey(connectionId), blockId.ToString("D"), _ttl);

            return new BlockLeaseResult(
                Granted: true,
                Lease: lease,
                CurrentHolder: lease);
        }

        return new BlockLeaseResult(
            Granted: false,
            Lease: null,
            CurrentHolder: current);
    }

    public async Task<BlockLeaseResult> RenewAsync(
        Guid blockId,
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var current = await GetCurrentAsync(blockId, cancellationToken);
        if (current is null)
        {
            return new BlockLeaseResult(
                Granted: false,
                Lease: null,
                CurrentHolder: null);
        }

        if (current.ConnectionId != connectionId || current.UserId != userId)
        {
            return new BlockLeaseResult(
                Granted: false,
                Lease: null,
                CurrentHolder: current);
        }

        var renewed = current with
        {
            ExpiresAtUtc = _clock.UtcNow.Add(_ttl)
        };

        var payload = JsonSerializer.Serialize(renewed, JsonOptions);

        await Database.StringSetAsync(BlockLeaseKey(blockId), payload, _ttl);
        await Database.StringSetAsync(ConnectionLeaseKey(connectionId), blockId.ToString("D"), _ttl);

        return new BlockLeaseResult(
            Granted: true,
            Lease: renewed,
            CurrentHolder: renewed);
    }

    public async Task ReleaseAsync(
        Guid blockId,
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var current = await GetCurrentAsync(blockId, cancellationToken);
        if (current is null)
            return;

        if (current.ConnectionId != connectionId || current.UserId != userId)
            return;

        await Database.KeyDeleteAsync(BlockLeaseKey(blockId));
        await Database.KeyDeleteAsync(ConnectionLeaseKey(connectionId));
    }

    public async Task ReleaseAllForConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        var raw = await Database.StringGetAsync(ConnectionLeaseKey(connectionId));
        if (!raw.HasValue)
            return;

        if (Guid.TryParse(raw.ToString(), out var blockId))
        {
            var current = await GetCurrentAsync(blockId, cancellationToken);
            if (current is not null && current.ConnectionId == connectionId)
            {
                await Database.KeyDeleteAsync(BlockLeaseKey(blockId));
            }
        }

        await Database.KeyDeleteAsync(ConnectionLeaseKey(connectionId));
    }

    public async Task<BlockLeaseInfo?> GetCurrentAsync(
        Guid blockId,
        CancellationToken cancellationToken = default)
    {
        var raw = await Database.StringGetAsync(BlockLeaseKey(blockId));
        if (!raw.HasValue)
            return null;

        return JsonSerializer.Deserialize<BlockLeaseInfo>(raw!, JsonOptions);
    }

    private static string BlockLeaseKey(Guid blockId)
        => $"pkm:realtime:lease:block:{blockId:D}";

    private static string ConnectionLeaseKey(string connectionId)
        => $"pkm:realtime:lease:conn:{connectionId}";
}