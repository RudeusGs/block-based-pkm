namespace Pkm.Application.Abstractions.Realtime;

public interface IBlockEditLeaseService
{
    Task<BlockLeaseResult> AcquireAsync(
        Guid blockId,
        Guid pageId,
        Guid userId,
        string connectionId,
        string? holderDisplayName = null,
        CancellationToken cancellationToken = default);

    Task<BlockLeaseResult> RenewAsync(
        Guid blockId,
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task ReleaseAsync(
        Guid blockId,
        Guid userId,
        string connectionId,
        CancellationToken cancellationToken = default);

    Task ReleaseAllForConnectionAsync(
        string connectionId,
        CancellationToken cancellationToken = default);

    Task<BlockLeaseInfo?> GetCurrentAsync(
        Guid blockId,
        CancellationToken cancellationToken = default);
}