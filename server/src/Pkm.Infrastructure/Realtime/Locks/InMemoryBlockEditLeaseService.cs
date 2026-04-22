using System.Collections.Concurrent;
using Microsoft.Extensions.Options;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;

namespace Pkm.Infrastructure.Realtime.Locks;

public sealed class InMemoryBlockEditLeaseService : IBlockEditLeaseService
{
    private readonly ConcurrentDictionary<Guid, BlockLeaseInfo> _leasesByBlock = new();
    private readonly ConcurrentDictionary<string, Guid> _blockByConnection = new(StringComparer.Ordinal);
    private readonly IClock _clock;
    private readonly TimeSpan _ttl;
    private readonly object _sync = new();

    public InMemoryBlockEditLeaseService(
        IClock clock,
        IOptions<RealtimeOptions> options)
    {
        _clock = clock;
        _ttl = TimeSpan.FromSeconds(Math.Max(5, options.Value.BlockLeaseTtlSeconds));
    }

    public Task<BlockLeaseResult> AcquireAsync(
        Guid blockId,
        Guid pageId,
        Guid userId,
        string connectionId,
        string? holderDisplayName = null,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            CleanupExpiredCore();

            if (_blockByConnection.TryGetValue(connectionId, out var existingBlockId) &&
                existingBlockId != blockId)
            {
                ReleaseCore(existingBlockId, userId, connectionId);
            }

            if (_leasesByBlock.TryGetValue(blockId, out var currentLease))
            {
                if (currentLease.ConnectionId != connectionId || currentLease.UserId != userId)
                {
                    return Task.FromResult(new BlockLeaseResult(
                        Granted: false,
                        Lease: null,
                        CurrentHolder: currentLease));
                }
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

            _leasesByBlock[blockId] = lease;
            _blockByConnection[connectionId] = blockId;

            return Task.FromResult(new BlockLeaseResult(
                Granted: true,
                Lease: lease,
                CurrentHolder: lease));
        }
    }

    public Task<BlockLeaseResult> RenewAsync(
        Guid blockId,
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            CleanupExpiredCore();

            if (!_leasesByBlock.TryGetValue(blockId, out var currentLease))
            {
                return Task.FromResult(new BlockLeaseResult(
                    Granted: false,
                    Lease: null,
                    CurrentHolder: null));
            }

            if (currentLease.ConnectionId != connectionId || currentLease.UserId != userId)
            {
                return Task.FromResult(new BlockLeaseResult(
                    Granted: false,
                    Lease: null,
                    CurrentHolder: currentLease));
            }

            var renewed = currentLease with
            {
                ExpiresAtUtc = _clock.UtcNow.Add(_ttl)
            };

            _leasesByBlock[blockId] = renewed;
            _blockByConnection[connectionId] = blockId;

            return Task.FromResult(new BlockLeaseResult(
                Granted: true,
                Lease: renewed,
                CurrentHolder: renewed));
        }
    }

    public Task ReleaseAsync(
        Guid blockId,
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            ReleaseCore(blockId, userId, connectionId);
            return Task.CompletedTask;
        }
    }

    public Task ReleaseAllForConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            if (_blockByConnection.TryGetValue(connectionId, out var blockId) &&
                _leasesByBlock.TryGetValue(blockId, out var currentLease) &&
                currentLease.ConnectionId == connectionId)
            {
                _leasesByBlock.TryRemove(blockId, out _);
            }

            _blockByConnection.TryRemove(connectionId, out _);
            return Task.CompletedTask;
        }
    }

    public Task<BlockLeaseInfo?> GetCurrentAsync(
        Guid blockId,
        CancellationToken cancellationToken = default)
    {
        lock (_sync)
        {
            CleanupExpiredCore();
            _leasesByBlock.TryGetValue(blockId, out var lease);
            return Task.FromResult<BlockLeaseInfo?>(lease);
        }
    }

    private void ReleaseCore(Guid blockId, Guid userId, string connectionId)
    {
        if (!_leasesByBlock.TryGetValue(blockId, out var currentLease))
            return;

        if (currentLease.ConnectionId != connectionId || currentLease.UserId != userId)
            return;

        _leasesByBlock.TryRemove(blockId, out _);
        _blockByConnection.TryRemove(connectionId, out _);
    }

    private void CleanupExpiredCore()
    {
        var now = _clock.UtcNow;

        foreach (var pair in _leasesByBlock)
        {
            if (pair.Value.ExpiresAtUtc <= now)
            {
                _leasesByBlock.TryRemove(pair.Key, out _);
                _blockByConnection.TryRemove(pair.Value.ConnectionId, out _);
            }
        }
    }
}