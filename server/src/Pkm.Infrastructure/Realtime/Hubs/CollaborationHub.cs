using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Abstractions.Time;
using Pkm.Application.Features.Documents.Models;
using Pkm.Application.Features.Documents.Policies;
using Pkm.Application.Features.Pages.Policies;
using Pkm.Application.Features.Workspaces.Policies;

namespace Pkm.Infrastructure.Realtime.Hubs;

[Authorize]
public sealed class CollaborationHub : Hub
{
    private readonly IWorkspaceAccessEvaluator _workspaceAccessEvaluator;
    private readonly IPageAccessEvaluator _pageAccessEvaluator;
    private readonly IDocumentAccessEvaluator _documentAccessEvaluator;
    private readonly IPagePresenceService _pagePresenceService;
    private readonly IBlockEditLeaseService _blockEditLeaseService;
    private readonly IDocumentRealtimePublisher _realtimePublisher;
    private readonly IClock _clock;

    public CollaborationHub(
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IPageAccessEvaluator pageAccessEvaluator,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IPagePresenceService pagePresenceService,
        IBlockEditLeaseService blockEditLeaseService,
        IDocumentRealtimePublisher realtimePublisher,
        IClock clock)
    {
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _pageAccessEvaluator = pageAccessEvaluator;
        _documentAccessEvaluator = documentAccessEvaluator;
        _pagePresenceService = pagePresenceService;
        _blockEditLeaseService = blockEditLeaseService;
        _realtimePublisher = realtimePublisher;
        _clock = clock;
    }

    public async Task<WorkspaceJoinAck> JoinWorkspace(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
            throw new HubException("WorkspaceId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            workspaceId,
            userId,
            cancellationToken);

        EnsureExists(access.Exists, "Workspace không tồn tại.");
        EnsureAllowed(access.CanReadWorkspace, "Bạn không có quyền tham gia workspace này.");

        var groupName = RealtimeGroupNames.Workspace(workspaceId);
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName, cancellationToken);

        return new WorkspaceJoinAck(workspaceId, groupName);
    }

    public async Task LeaveWorkspace(
        Guid workspaceId,
        CancellationToken cancellationToken = default)
    {
        if (workspaceId == Guid.Empty)
            return;

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroupNames.Workspace(workspaceId),
            cancellationToken);
    }

    public async Task<PageJoinAck> JoinPage(
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        if (pageId == Guid.Empty)
            throw new HubException("PageId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _pageAccessEvaluator.EvaluateAsync(
            pageId,
            userId,
            cancellationToken);

        EnsureExists(access.Exists, "Page không tồn tại.");
        EnsureAllowed(access.CanReadDocument, "Bạn không có quyền tham gia page này.");

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            RealtimeGroupNames.Workspace(access.WorkspaceId),
            cancellationToken);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            RealtimeGroupNames.Page(pageId),
            cancellationToken);

        await _pagePresenceService.UpsertAsync(
            pageId,
            access.WorkspaceId,
            userId,
            GetCurrentUserDisplayName(),
            Context.ConnectionId,
            cancellationToken);

        var snapshot = await BuildPagePresenceDtoAsync(
            access.WorkspaceId,
            pageId,
            cancellationToken);

        await PublishPagePresenceChangedAsync(
            access.WorkspaceId,
            pageId,
            userId,
            snapshot,
            cancellationToken);

        return new PageJoinAck(
            access.WorkspaceId,
            pageId,
            snapshot);
    }

    public async Task<PagePresenceDto> HeartbeatPage(
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        if (pageId == Guid.Empty)
            throw new HubException("PageId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _pageAccessEvaluator.EvaluateAsync(
            pageId,
            userId,
            cancellationToken);

        EnsureExists(access.Exists, "Page không tồn tại.");
        EnsureAllowed(access.CanReadDocument, "Bạn không có quyền truy cập page này.");

        await _pagePresenceService.UpsertAsync(
            pageId,
            access.WorkspaceId,
            userId,
            GetCurrentUserDisplayName(),
            Context.ConnectionId,
            cancellationToken);

        return await BuildPagePresenceDtoAsync(
            access.WorkspaceId,
            pageId,
            cancellationToken);
    }

    public async Task LeavePage(
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        if (pageId == Guid.Empty)
            return;

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroupNames.Page(pageId),
            cancellationToken);

        var removed = await _pagePresenceService.RemoveConnectionAsync(
            Context.ConnectionId,
            cancellationToken);

        await _blockEditLeaseService.ReleaseAllForConnectionAsync(
            Context.ConnectionId,
            cancellationToken);

        if (removed is null || removed.PageId != pageId)
            return;

        var userId = GetOptionalUserId() ?? removed.UserId;

        var snapshot = await BuildPagePresenceDtoAsync(
            removed.WorkspaceId,
            removed.PageId,
            cancellationToken);

        await PublishPagePresenceChangedAsync(
            removed.WorkspaceId,
            removed.PageId,
            userId,
            snapshot,
            cancellationToken);
    }

    public async Task<PagePresenceDto> GetPagePresence(
        Guid pageId,
        CancellationToken cancellationToken = default)
    {
        if (pageId == Guid.Empty)
            throw new HubException("PageId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _pageAccessEvaluator.EvaluateAsync(
            pageId,
            userId,
            cancellationToken);

        EnsureExists(access.Exists, "Page không tồn tại.");
        EnsureAllowed(access.CanReadDocument, "Bạn không có quyền xem presence của page này.");

        return await BuildPagePresenceDtoAsync(
            access.WorkspaceId,
            pageId,
            cancellationToken);
    }

    public async Task<BlockLeaseHubResponse> AcquireBlockLease(
        Guid blockId,
        string? holderDisplayName = null,
        CancellationToken cancellationToken = default)
    {
        if (blockId == Guid.Empty)
            throw new HubException("BlockId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _documentAccessEvaluator.EvaluateByBlockAsync(
            blockId,
            userId,
            cancellationToken);

        EnsureExists(access.Exists, "Block không tồn tại.");
        EnsureAllowed(access.CanAcquireLease, "Bạn không có quyền acquire lease của block này.");

        var result = await _blockEditLeaseService.AcquireAsync(
            blockId,
            access.PageId,
            userId,
            Context.ConnectionId,
            string.IsNullOrWhiteSpace(holderDisplayName)
                ? GetCurrentUserDisplayName()
                : holderDisplayName,
            cancellationToken);

        var lease = result.Granted ? result.Lease : result.CurrentHolder;

        if (result.Granted)
        {
            var payload = new BlockLeaseDto(
                BlockId: blockId,
                PageId: access.PageId,
                Granted: true,
                Status: "acquired",
                HolderUserId: lease?.UserId,
                HolderDisplayName: lease?.HolderDisplayName,
                ExpiresAtUtc: lease?.ExpiresAtUtc,
                IsHeldByCurrentUser: true);

            await _realtimePublisher.PublishToPageAsync(
                new DocumentRealtimeEnvelope(
                    EventName: "BlockLeaseChanged",
                    WorkspaceId: access.WorkspaceId,
                    PageId: access.PageId,
                    BlockId: blockId,
                    ActorId: userId,
                    OccurredAtUtc: _clock.UtcNow,
                    Revision: null,
                    Payload: payload),
                cancellationToken);

            return new BlockLeaseHubResponse(
                BlockId: blockId,
                PageId: access.PageId,
                Granted: true,
                Status: "acquired",
                HolderUserId: lease?.UserId,
                HolderDisplayName: lease?.HolderDisplayName,
                ExpiresAtUtc: lease?.ExpiresAtUtc);
        }

        return new BlockLeaseHubResponse(
            BlockId: blockId,
            PageId: access.PageId,
            Granted: false,
            Status: "conflict",
            HolderUserId: lease?.UserId,
            HolderDisplayName: lease?.HolderDisplayName,
            ExpiresAtUtc: lease?.ExpiresAtUtc);
    }

    public async Task<BlockLeaseHubResponse> RenewBlockLease(
        Guid blockId,
        CancellationToken cancellationToken = default)
    {
        if (blockId == Guid.Empty)
            throw new HubException("BlockId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _documentAccessEvaluator.EvaluateByBlockAsync(
            blockId,
            userId,
            cancellationToken);

        EnsureExists(access.Exists, "Block không tồn tại.");
        EnsureAllowed(access.CanAcquireLease, "Bạn không có quyền renew lease của block này.");

        var result = await _blockEditLeaseService.RenewAsync(
            blockId,
            userId,
            Context.ConnectionId,
            cancellationToken);

        var lease = result.Granted ? result.Lease : result.CurrentHolder;

        return new BlockLeaseHubResponse(
            BlockId: blockId,
            PageId: access.PageId,
            Granted: result.Granted,
            Status: result.Granted ? "renewed" : "conflict",
            HolderUserId: lease?.UserId,
            HolderDisplayName: lease?.HolderDisplayName,
            ExpiresAtUtc: lease?.ExpiresAtUtc);
    }

    public async Task ReleaseBlockLease(
        Guid blockId,
        CancellationToken cancellationToken = default)
    {
        if (blockId == Guid.Empty)
            throw new HubException("BlockId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _documentAccessEvaluator.EvaluateByBlockAsync(
            blockId,
            userId,
            cancellationToken);

        EnsureExists(access.Exists, "Block không tồn tại.");
        EnsureAllowed(access.CanAcquireLease, "Bạn không có quyền release lease của block này.");

        var leaseBeforeRelease = await _blockEditLeaseService.GetCurrentAsync(
            blockId,
            cancellationToken);

        await _blockEditLeaseService.ReleaseAsync(
            blockId,
            userId,
            Context.ConnectionId,
            cancellationToken);

        if (leaseBeforeRelease is not null &&
            leaseBeforeRelease.UserId == userId &&
            string.Equals(leaseBeforeRelease.ConnectionId, Context.ConnectionId, StringComparison.Ordinal))
        {
            var payload = new BlockLeaseDto(
                BlockId: blockId,
                PageId: access.PageId,
                Granted: true,
                Status: "released",
                HolderUserId: null,
                HolderDisplayName: null,
                ExpiresAtUtc: null,
                IsHeldByCurrentUser: false);

            await _realtimePublisher.PublishToPageAsync(
                new DocumentRealtimeEnvelope(
                    EventName: "BlockLeaseChanged",
                    WorkspaceId: access.WorkspaceId,
                    PageId: access.PageId,
                    BlockId: blockId,
                    ActorId: userId,
                    OccurredAtUtc: _clock.UtcNow,
                    Revision: null,
                    Payload: payload),
                cancellationToken);
        }
    }

    public async Task<BlockLeaseHubResponse> GetBlockLease(
        Guid blockId,
        CancellationToken cancellationToken = default)
    {
        if (blockId == Guid.Empty)
            throw new HubException("BlockId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _documentAccessEvaluator.EvaluateByBlockAsync(
            blockId,
            userId,
            cancellationToken);

        EnsureExists(access.Exists, "Block không tồn tại.");
        EnsureAllowed(access.CanReadDocument, "Bạn không có quyền xem lease của block này.");

        var lease = await _blockEditLeaseService.GetCurrentAsync(
            blockId,
            cancellationToken);

        if (lease is null)
        {
            return new BlockLeaseHubResponse(
                BlockId: blockId,
                PageId: access.PageId,
                Granted: false,
                Status: "not_held",
                HolderUserId: null,
                HolderDisplayName: null,
                ExpiresAtUtc: null);
        }

        return new BlockLeaseHubResponse(
            BlockId: blockId,
            PageId: access.PageId,
            Granted: lease.UserId == userId,
            Status: lease.UserId == userId ? "held_by_current_user" : "held_by_other_user",
            HolderUserId: lease.UserId,
            HolderDisplayName: lease.HolderDisplayName,
            ExpiresAtUtc: lease.ExpiresAtUtc);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var removed = await _pagePresenceService.RemoveConnectionAsync(
            Context.ConnectionId,
            CancellationToken.None);

        await _blockEditLeaseService.ReleaseAllForConnectionAsync(
            Context.ConnectionId,
            CancellationToken.None);

        if (removed is not null)
        {
            var userId = GetOptionalUserId() ?? removed.UserId;

            var snapshot = await BuildPagePresenceDtoAsync(
                removed.WorkspaceId,
                removed.PageId,
                CancellationToken.None);

            await PublishPagePresenceChangedAsync(
                removed.WorkspaceId,
                removed.PageId,
                userId,
                snapshot,
                CancellationToken.None);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task<PagePresenceDto> BuildPagePresenceDtoAsync(
        Guid workspaceId,
        Guid pageId,
        CancellationToken cancellationToken)
    {
        var entries = await _pagePresenceService.GetActiveOnPageAsync(
            pageId,
            cancellationToken);

        var users = entries
            .GroupBy(x => new { x.UserId, x.UserName })
            .Select(group =>
            {
                var lastSeenUtc = group.Max(x => x.LastSeenUtc);
                return new PagePresenceUserDto(
                    group.Key.UserId,
                    group.Key.UserName,
                    group.Count(),
                    lastSeenUtc);
            })
            .OrderByDescending(x => x.LastSeenUtc)
            .ToArray();

        return new PagePresenceDto(
            workspaceId,
            pageId,
            users);
    }

    private async Task PublishPagePresenceChangedAsync(
        Guid workspaceId,
        Guid pageId,
        Guid actorId,
        PagePresenceDto snapshot,
        CancellationToken cancellationToken)
    {
        await _realtimePublisher.PublishToPageAsync(
            new DocumentRealtimeEnvelope(
                EventName: "PagePresenceChanged",
                WorkspaceId: workspaceId,
                PageId: pageId,
                BlockId: null,
                ActorId: actorId,
                OccurredAtUtc: _clock.UtcNow,
                Revision: null,
                Payload: snapshot),
            cancellationToken);
    }

    private Guid GetRequiredUserId()
    {
        var rawUserId =
            Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            Context.User?.FindFirstValue("sub");

        if (!Guid.TryParse(rawUserId, out var userId) || userId == Guid.Empty)
            throw new HubException("Unauthorized");

        return userId;
    }

    private Guid? GetOptionalUserId()
    {
        var rawUserId =
            Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ??
            Context.User?.FindFirstValue("sub");

        return Guid.TryParse(rawUserId, out var userId) && userId != Guid.Empty
            ? userId
            : null;
    }

    private string? GetCurrentUserDisplayName()
    {
        return Context.User?.FindFirstValue(ClaimTypes.Name) ??
               Context.User?.FindFirstValue("name") ??
               Context.User?.FindFirstValue("preferred_username") ??
               Context.User?.Identity?.Name;
    }

    private static void EnsureExists(bool exists, string message)
    {
        if (!exists)
            throw new HubException(message);
    }

    private static void EnsureAllowed(bool allowed, string message)
    {
        if (!allowed)
            throw new HubException(message);
    }
}