using Microsoft.Extensions.Logging;
using Pkm.Application.Abstractions.Realtime;
using StackExchange.Redis;

namespace Pkm.Infrastructure.Realtime.Presence;

internal sealed class FallbackWorkspacePresenceService : IWorkspacePresenceService
{
    private readonly RedisWorkspacePresenceService _primary;
    private readonly InMemoryWorkspacePresenceService _fallback;
    private readonly ILogger<FallbackWorkspacePresenceService> _logger;

    public FallbackWorkspacePresenceService(
        RedisWorkspacePresenceService primary,
        InMemoryWorkspacePresenceService fallback,
        ILogger<FallbackWorkspacePresenceService> logger)
    {
        _primary = primary;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<WorkspacePresenceEntry> UpsertAsync(
        Guid workspaceId,
        Guid userId,
        string? userName,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var entry = await _primary.UpsertAsync(
                workspaceId,
                userId,
                userName,
                connectionId,
                cancellationToken);

            await _fallback.UpsertAsync(
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
                "Redis unavailable during workspace presence UPSERT for workspace {WorkspaceId}, connection {ConnectionId}. Falling back to in-memory presence.",
                workspaceId,
                connectionId);

            return await _fallback.UpsertAsync(
                workspaceId,
                userId,
                userName,
                connectionId,
                cancellationToken);
        }
    }

    public async Task<WorkspacePresenceEntry?> RemoveConnectionFromWorkspaceAsync(
        Guid workspaceId,
        string connectionId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var removed = await _primary.RemoveConnectionFromWorkspaceAsync(
                workspaceId,
                connectionId,
                cancellationToken);

            await _fallback.RemoveConnectionFromWorkspaceAsync(
                workspaceId,
                connectionId,
                cancellationToken);

            return removed;
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis unavailable during workspace presence REMOVE for workspace {WorkspaceId}, connection {ConnectionId}. Falling back to in-memory presence.",
                workspaceId,
                connectionId);

            return await _fallback.RemoveConnectionFromWorkspaceAsync(
                workspaceId,
                connectionId,
                cancellationToken);
        }
    }

    public async Task<IReadOnlyList<WorkspacePresenceEntry>> RemoveConnectionAsync(
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
                "Redis unavailable during workspace presence REMOVE for connection {ConnectionId}. Falling back to in-memory presence.",
                connectionId);

            return await _fallback.RemoveConnectionAsync(connectionId, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<WorkspacePresenceEntry>> GetActiveOnWorkspaceAsync(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _primary.GetActiveOnWorkspaceAsync(workspaceId, cancellationToken);
        }
        catch (Exception ex) when (IsRedisFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "Redis unavailable during workspace presence GET for workspace {WorkspaceId}. Falling back to in-memory presence.",
                workspaceId);

            return await _fallback.GetActiveOnWorkspaceAsync(workspaceId, cancellationToken);
        }
    }

    private static bool IsRedisFailure(Exception ex)
        => ex is RedisConnectionException
           or RedisTimeoutException
           or RedisServerException
           or TimeoutException
           or ObjectDisposedException;
}
