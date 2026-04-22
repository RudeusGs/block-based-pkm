using Microsoft.Extensions.Logging;
using Pkm.Application.Abstractions.Realtime;
using StackExchange.Redis;

namespace Pkm.Infrastructure.Realtime.Presence;

internal sealed class FallbackPagePresenceService : IPagePresenceService
{
    private readonly RedisPagePresenceService _primary;
    private readonly InMemoryPagePresenceService _fallback;
    private readonly ILogger<FallbackPagePresenceService> _logger;

    public FallbackPagePresenceService(
        RedisPagePresenceService primary,
        InMemoryPagePresenceService fallback,
        ILogger<FallbackPagePresenceService> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<PagePresenceEntry> UpsertAsync(
        Guid pageId,
        Guid workspaceId,
        Guid userId,
        string? userName,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await _primary.UpsertAsync(
                pageId,
                workspaceId,
                userId,
                userName,
                connectionId,
                cancellationToken);

            await _fallback.UpsertAsync(
                pageId,
                workspaceId,
                userId,
                userName,
                connectionId,
                cancellationToken);

            return entry;
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis unavailable during page presence UPSERT for page {PageId}, connection {ConnectionId}. Falling back to in-memory presence.",
                pageId,
                connectionId);

            return await _fallback.UpsertAsync(
                pageId,
                workspaceId,
                userId,
                userName,
                connectionId,
                cancellationToken);
        }
    }

    public async Task<PagePresenceEntry?> RemoveConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var removed = await _primary.RemoveConnectionAsync(connectionId, cancellationToken);
            await _fallback.RemoveConnectionAsync(connectionId, cancellationToken);
            return removed;
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis unavailable during page presence REMOVE for connection {ConnectionId}. Falling back to in-memory presence.",
                connectionId);

            return await _fallback.RemoveConnectionAsync(connectionId, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<PagePresenceEntry>> GetActiveOnPageAsync(
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.GetActiveOnPageAsync(pageId, cancellationToken);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis unavailable during page presence GET for page {PageId}. Falling back to in-memory presence.",
                pageId);

            return await _fallback.GetActiveOnPageAsync(pageId, cancellationToken);
        }
    }

    private static bool IsRedisFailure(Exception ex)
        => ex is RedisConnectionException
           or RedisTimeoutException
           or RedisServerException
           or TimeoutException
           or ObjectDisposedException;
}