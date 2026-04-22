using Microsoft.Extensions.Logging;
using Pkm.Application.Abstractions.Realtime;
using StackExchange.Redis;

namespace Pkm.Infrastructure.Realtime.Locks;

internal sealed class FallbackBlockEditLeaseService : IBlockEditLeaseService
{
    private readonly RedisBlockEditLeaseService _primary;
    private readonly InMemoryBlockEditLeaseService _fallback;
    private readonly ILogger<FallbackBlockEditLeaseService> _logger;

    public FallbackBlockEditLeaseService(
        RedisBlockEditLeaseService primary,
        InMemoryBlockEditLeaseService fallback,
        ILogger<FallbackBlockEditLeaseService> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<BlockLeaseResult> AcquireAsync(
        Guid blockId,
        Guid pageId,
        Guid userId,
        string connectionId,
        string? holderDisplayName = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _primary.AcquireAsync(
                blockId,
                pageId,
                userId,
                connectionId,
                holderDisplayName,
                cancellationToken);

            if (result.Granted)
            {
                await _fallback.AcquireAsync(
                    blockId,
                    pageId,
                    userId,
                    connectionId,
                    holderDisplayName,
                    cancellationToken);
            }

            return result;
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis unavailable during block lease ACQUIRE for block {BlockId}, connection {ConnectionId}. Falling back to in-memory lease service.",
                blockId,
                connectionId);

            return await _fallback.AcquireAsync(
                blockId,
                pageId,
                userId,
                connectionId,
                holderDisplayName,
                cancellationToken);
        }
    }

    public async Task<BlockLeaseResult> RenewAsync(
        Guid blockId,
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var result = await _primary.RenewAsync(
                blockId,
                userId,
                connectionId,
                cancellationToken);

            if (result.Granted)
            {
                await _fallback.RenewAsync(
                    blockId,
                    userId,
                    connectionId,
                    cancellationToken);
            }

            return result;
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis unavailable during block lease RENEW for block {BlockId}, connection {ConnectionId}. Falling back to in-memory lease service.",
                blockId,
                connectionId);

            return await _fallback.RenewAsync(
                blockId,
                userId,
                connectionId,
                cancellationToken);
        }
    }

    public async Task ReleaseAsync(
        Guid blockId,
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _primary.ReleaseAsync(blockId, userId, connectionId, cancellationToken);
            await _fallback.ReleaseAsync(blockId, userId, connectionId, cancellationToken);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis unavailable during block lease RELEASE for block {BlockId}, connection {ConnectionId}. Falling back to in-memory lease service.",
                blockId,
                connectionId);

            await _fallback.ReleaseAsync(blockId, userId, connectionId, cancellationToken);
        }
    }

    public async Task ReleaseAllForConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _primary.ReleaseAllForConnectionAsync(connectionId, cancellationToken);
            await _fallback.ReleaseAllForConnectionAsync(connectionId, cancellationToken);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis unavailable during block lease RELEASE-ALL for connection {ConnectionId}. Falling back to in-memory lease service.",
                connectionId);

            await _fallback.ReleaseAllForConnectionAsync(connectionId, cancellationToken);
        }
    }

    public async Task<BlockLeaseInfo?> GetCurrentAsync(
        Guid blockId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.GetCurrentAsync(blockId, cancellationToken);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis unavailable during block lease GET for block {BlockId}. Falling back to in-memory lease service.",
                blockId);

            return await _fallback.GetCurrentAsync(blockId, cancellationToken);
        }
    }

    private static bool IsRedisFailure(Exception ex)
        => ex is RedisConnectionException
           or RedisTimeoutException
           or RedisServerException
           or TimeoutException
           or ObjectDisposedException;
}