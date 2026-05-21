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
    private readonly IWorkspacePresenceService _workspacePresenceService;
    private readonly IPagePresenceService _pagePresenceService;
    private readonly IBlockEditLeaseService _blockEditLeaseService;
    private readonly IDocumentRealtimePublisher _realtimePublisher;
    private readonly IClock _clock;

    public CollaborationHub(
        IWorkspaceAccessEvaluator workspaceAccessEvaluator,
        IPageAccessEvaluator pageAccessEvaluator,
        IDocumentAccessEvaluator documentAccessEvaluator,
        IWorkspacePresenceService workspacePresenceService,
        IPagePresenceService pagePresenceService,
        IBlockEditLeaseService blockEditLeaseService,
        IDocumentRealtimePublisher realtimePublisher,
        IClock clock)
    {
        _workspaceAccessEvaluator = workspaceAccessEvaluator;
        _pageAccessEvaluator = pageAccessEvaluator;
        _documentAccessEvaluator = documentAccessEvaluator;
        _workspacePresenceService = workspacePresenceService;
        _pagePresenceService = pagePresenceService;
        _blockEditLeaseService = blockEditLeaseService;
        _realtimePublisher = realtimePublisher;
        _clock = clock;
    }

    public async Task<WorkspaceJoinAck> JoinWorkspace(Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
            throw new HubException("WorkspaceId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            workspaceId,
            userId,
            Context.ConnectionAborted);

        EnsureExists(access.Exists, "Workspace không tồn tại.");
        EnsureAllowed(access.CanReadWorkspace, "Bạn không có quyền tham gia workspace này.");

        var groupName = RealtimeGroupNames.Workspace(workspaceId);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName,
            Context.ConnectionAborted);

        await _workspacePresenceService.UpsertAsync(
            workspaceId,
            userId,
            GetCurrentUserDisplayName(),
            Context.ConnectionId,
            Context.ConnectionAborted);

        var snapshot = await BuildWorkspacePresenceDtoAsync(
            workspaceId,
            Context.ConnectionAborted);

        await PublishWorkspacePresenceChangedAsync(
            workspaceId,
            userId,
            snapshot,
            Context.ConnectionAborted);

        return new WorkspaceJoinAck(
            workspaceId,
            groupName,
            snapshot);
    }

    public async Task LeaveWorkspace(Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
            return;

        var removed = await _workspacePresenceService.RemoveConnectionFromWorkspaceAsync(
            workspaceId,
            Context.ConnectionId,
            Context.ConnectionAborted);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroupNames.Workspace(workspaceId),
            Context.ConnectionAborted);

        if (removed is null)
            return;

        var userId = GetOptionalUserId() ?? removed.UserId;

        var snapshot = await BuildWorkspacePresenceDtoAsync(
            workspaceId,
            Context.ConnectionAborted);

        await PublishWorkspacePresenceChangedAsync(
            workspaceId,
            userId,
            snapshot,
            Context.ConnectionAborted);
    }

    public async Task<WorkspacePresenceDto> HeartbeatWorkspace(Guid workspaceId)
    {
        if (workspaceId == Guid.Empty)
            throw new HubException("WorkspaceId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _workspaceAccessEvaluator.EvaluateAsync(
            workspaceId,
            userId,
            Context.ConnectionAborted);

        EnsureExists(access.Exists, "Workspace không tồn tại.");
        EnsureAllowed(access.CanReadWorkspace, "Bạn không có quyền truy cập workspace này.");

        await _workspacePresenceService.UpsertAsync(
            workspaceId,
            userId,
            GetCurrentUserDisplayName(),
            Context.ConnectionId,
            Context.ConnectionAborted);

        return await BuildWorkspacePresenceDtoAsync(
            workspaceId,
            Context.ConnectionAborted);
    }

    public async Task<PageJoinAck> JoinPage(Guid pageId)
    {
        if (pageId == Guid.Empty)
            throw new HubException("PageId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _pageAccessEvaluator.EvaluateAsync(
            pageId,
            userId,
            Context.ConnectionAborted);

        EnsureExists(access.Exists, "Page không tồn tại.");
        EnsureAllowed(access.CanReadDocument, "Bạn không có quyền đọc page này.");

        await JoinWorkspace(access.WorkspaceId);

        var groupName = RealtimeGroupNames.Page(pageId);

        await Groups.AddToGroupAsync(
            Context.ConnectionId,
            groupName,
            Context.ConnectionAborted);

        await _pagePresenceService.UpsertAsync(
            access.WorkspaceId,
            pageId,
            userId,
            GetCurrentUserDisplayName(),
            Context.ConnectionId,
            Context.ConnectionAborted);

        var snapshot = await BuildPagePresenceDtoAsync(
            access.WorkspaceId,
            pageId,
            Context.ConnectionAborted);

        await PublishPagePresenceChangedAsync(
            access.WorkspaceId,
            pageId,
            userId,
            snapshot,
            Context.ConnectionAborted);

        return new PageJoinAck(
            access.WorkspaceId,
            pageId,
            groupName,
            snapshot);
    }

    public async Task LeavePage(Guid pageId)
    {
        if (pageId == Guid.Empty)
            return;

        var removed = await _pagePresenceService.RemoveConnectionAsync(
            Context.ConnectionId,
            Context.ConnectionAborted);

        await Groups.RemoveFromGroupAsync(
            Context.ConnectionId,
            RealtimeGroupNames.Page(pageId),
            Context.ConnectionAborted);

        var releasedLeases = await _blockEditLeaseService.ReleaseAllForConnectionAsync(
            Context.ConnectionId,
            Context.ConnectionAborted);

        if (removed is null)
            return;

        var userId = GetOptionalUserId() ?? removed.UserId;

        await PublishReleasedLeasesAsync(
            releasedLeases,
            removed.WorkspaceId,
            userId,
            Context.ConnectionAborted);

        var snapshot = await BuildPagePresenceDtoAsync(
            removed.WorkspaceId,
            removed.PageId,
            Context.ConnectionAborted);

        await PublishPagePresenceChangedAsync(
            removed.WorkspaceId,
            removed.PageId,
            userId,
            snapshot,
            Context.ConnectionAborted);
    }

    public async Task<PagePresenceDto> HeartbeatPage(Guid pageId)
    {
        if (pageId == Guid.Empty)
            throw new HubException("PageId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _pageAccessEvaluator.EvaluateAsync(
            pageId,
            userId,
            Context.ConnectionAborted);

        EnsureExists(access.Exists, "Page không tồn tại.");
        EnsureAllowed(access.CanReadDocument, "Bạn không có quyền đọc page này.");

        await _pagePresenceService.UpsertAsync(
            access.WorkspaceId,
            pageId,
            userId,
            GetCurrentUserDisplayName(),
            Context.ConnectionId,
            Context.ConnectionAborted);

        return await BuildPagePresenceDtoAsync(
            access.WorkspaceId,
            pageId,
            Context.ConnectionAborted);
    }

    public async Task SendCursor(PageCursorRequest request)
    {
        if (request.PageId == Guid.Empty)
            throw new HubException("PageId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _pageAccessEvaluator.EvaluateAsync(
            request.PageId,
            userId,
            Context.ConnectionAborted);

        EnsureExists(access.Exists, "Page không tồn tại.");
        EnsureAllowed(access.CanReadDocument, "Bạn không có quyền đọc page này.");

        var dto = new PageCursorDto(
            WorkspaceId: access.WorkspaceId,
            PageId: request.PageId,
            BlockId: request.BlockId,
            UserId: userId,
            UserName: GetCurrentUserDisplayName(),
            ConnectionId: Context.ConnectionId,
            AnchorKey: NormalizeSmallText(request.AnchorKey, 200),
            Offset: request.Offset,
            Color: NormalizeSmallText(request.Color, 30),
            OccurredAtUtc: _clock.UtcNow);

        await Clients
            .OthersInGroup(RealtimeGroupNames.Page(request.PageId))
            .SendAsync("PageCursorChanged", dto, Context.ConnectionAborted);
    }

    public async Task SendMousePointer(PageMousePointerRequest request)
    {
        if (request.PageId == Guid.Empty)
            throw new HubException("PageId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _pageAccessEvaluator.EvaluateAsync(
            request.PageId,
            userId,
            Context.ConnectionAborted);

        EnsureExists(access.Exists, "Page không tồn tại.");
        EnsureAllowed(access.CanReadDocument, "Bạn không có quyền đọc page này.");

        var safeX = Math.Clamp(request.X, 0, 100);
        var safeY = Math.Clamp(request.Y, 0, 100);

        var dto = new PageMousePointerDto(
            WorkspaceId: access.WorkspaceId,
            PageId: request.PageId,
            BlockId: request.BlockId,
            UserId: userId,
            UserName: GetCurrentUserDisplayName(),
            ConnectionId: Context.ConnectionId,
            X: safeX,
            Y: safeY,
            Color: NormalizeSmallText(request.Color, 30),
            IsLeaving: request.IsLeaving,
            OccurredAtUtc: _clock.UtcNow);

        await Clients
            .OthersInGroup(RealtimeGroupNames.Page(request.PageId))
            .SendAsync("PageMousePointerChanged", dto, Context.ConnectionAborted);
    }
    public async Task SendBlockDraft(BlockDraftRequest request)
    {
        if (request.PageId == Guid.Empty)
            throw new HubException("PageId không hợp lệ.");

        if (request.BlockId == Guid.Empty)
            throw new HubException("BlockId không hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.EditorSessionId))
            throw new HubException("EditorSessionId không hợp lệ.");

        if (request.BaseRevision < 0)
            throw new HubException("BaseRevision không hợp lệ.");

        if (request.ClientSequence < 0)
            throw new HubException("ClientSequence không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _documentAccessEvaluator.EvaluateByBlockAsync(
            request.BlockId,
            userId,
            Context.ConnectionAborted);

        EnsureExists(access.Exists, "Block không tồn tại.");
        EnsureAllowed(access.CanEditDocument, "Bạn không có quyền sửa block này.");

        if (access.PageId != request.PageId)
            throw new HubException("Block không thuộc page này.");

        var lease = await _blockEditLeaseService.GetCurrentAsync(
            request.BlockId,
            Context.ConnectionAborted);

        EnsureCurrentLeaseHolder(lease, userId, request.EditorSessionId);

        var dto = new BlockDraftDto(
            WorkspaceId: access.WorkspaceId,
            PageId: request.PageId,
            BlockId: request.BlockId,
            UserId: userId,
            UserName: GetCurrentUserDisplayName(),
            ConnectionId: Context.ConnectionId,
            EditorSessionId: request.EditorSessionId,
            BaseRevision: request.BaseRevision,
            ClientSequence: request.ClientSequence,
            Type: NormalizeSmallText(request.Type, 50),
            TextContent: request.TextContent,
            PropsJson: request.PropsJson,
            OccurredAtUtc: _clock.UtcNow);

        await Clients
            .OthersInGroup(RealtimeGroupNames.Page(request.PageId))
            .SendAsync("BlockDraftChanged", dto, Context.ConnectionAborted);
    }

    public async Task SendBlockEditingState(BlockEditingStateRequest request)
    {
        if (request.PageId == Guid.Empty)
            throw new HubException("PageId không hợp lệ.");

        if (request.BlockId == Guid.Empty)
            throw new HubException("BlockId không hợp lệ.");

        if (string.IsNullOrWhiteSpace(request.EditorSessionId))
            throw new HubException("EditorSessionId không hợp lệ.");

        var userId = GetRequiredUserId();

        var access = await _documentAccessEvaluator.EvaluateByBlockAsync(
            request.BlockId,
            userId,
            Context.ConnectionAborted);

        EnsureExists(access.Exists, "Block không tồn tại.");
        EnsureAllowed(access.CanEditDocument, "Bạn không có quyền sửa block này.");

        if (access.PageId != request.PageId)
            throw new HubException("Block không thuộc page này.");

        var lease = await _blockEditLeaseService.GetCurrentAsync(
            request.BlockId,
            Context.ConnectionAborted);

        if (request.IsEditing)
        {
            EnsureCurrentLeaseHolder(lease, userId, request.EditorSessionId);
        }
        else if (lease is not null &&
                 lease.UserId == userId &&
                 string.Equals(lease.ConnectionId, request.EditorSessionId, StringComparison.Ordinal))
        {
            await _blockEditLeaseService.ReleaseAsync(
                request.BlockId,
                userId,
                request.EditorSessionId,
                Context.ConnectionAborted);
        }

        var dto = new BlockEditingStateDto(
            WorkspaceId: access.WorkspaceId,
            PageId: request.PageId,
            BlockId: request.BlockId,
            UserId: userId,
            UserName: GetCurrentUserDisplayName(),
            ConnectionId: Context.ConnectionId,
            EditorSessionId: request.EditorSessionId,
            IsEditing: request.IsEditing,
            OccurredAtUtc: _clock.UtcNow);

        await Clients
            .OthersInGroup(RealtimeGroupNames.Page(request.PageId))
            .SendAsync("BlockEditingStateChanged", dto, Context.ConnectionAborted);

        if (!request.IsEditing && lease is not null)
        {
            var leaseDto = new BlockLeaseDto(
                BlockId: request.BlockId,
                PageId: request.PageId,
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
                    PageId: request.PageId,
                    BlockId: request.BlockId,
                    ActorId: userId,
                    OccurredAtUtc: _clock.UtcNow,
                    Revision: null,
                    Payload: leaseDto),
                Context.ConnectionAborted);
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var removedWorkspaceEntries = await _workspacePresenceService.RemoveConnectionAsync(
            Context.ConnectionId,
            CancellationToken.None);

        var removedPageEntry = await _pagePresenceService.RemoveConnectionAsync(
            Context.ConnectionId,
            CancellationToken.None);

        var releasedLeases = await _blockEditLeaseService.ReleaseAllForConnectionAsync(
            Context.ConnectionId,
            CancellationToken.None);

        foreach (var removedWorkspaceEntry in removedWorkspaceEntries
                     .GroupBy(x => x.WorkspaceId)
                     .Select(x => x.First()))
        {
            var userId = GetOptionalUserId() ?? removedWorkspaceEntry.UserId;

            var snapshot = await BuildWorkspacePresenceDtoAsync(
                removedWorkspaceEntry.WorkspaceId,
                CancellationToken.None);

            await PublishWorkspacePresenceChangedAsync(
                removedWorkspaceEntry.WorkspaceId,
                userId,
                snapshot,
                CancellationToken.None);
        }

        if (removedPageEntry is not null)
        {
            var userId = GetOptionalUserId() ?? removedPageEntry.UserId;

            var snapshot = await BuildPagePresenceDtoAsync(
                removedPageEntry.WorkspaceId,
                removedPageEntry.PageId,
                CancellationToken.None);

            await PublishPagePresenceChangedAsync(
                removedPageEntry.WorkspaceId,
                removedPageEntry.PageId,
                userId,
                snapshot,
                CancellationToken.None);

            await PublishReleasedLeasesAsync(
                releasedLeases,
                removedPageEntry.WorkspaceId,
                userId,
                CancellationToken.None);
        }

        await base.OnDisconnectedAsync(exception);
    }

    private async Task PublishReleasedLeasesAsync(
        IReadOnlyList<BlockLeaseInfo> releasedLeases,
        Guid workspaceId,
        Guid actorId,
        CancellationToken cancellationToken)
    {
        foreach (var lease in releasedLeases)
        {
            var leaseDto = new BlockLeaseDto(
                BlockId: lease.BlockId,
                PageId: lease.PageId,
                Granted: true,
                Status: "released",
                HolderUserId: null,
                HolderDisplayName: null,
                ExpiresAtUtc: null,
                IsHeldByCurrentUser: false);

            await _realtimePublisher.PublishToPageAsync(
                new DocumentRealtimeEnvelope(
                    EventName: "BlockLeaseChanged",
                    WorkspaceId: workspaceId,
                    PageId: lease.PageId,
                    BlockId: lease.BlockId,
                    ActorId: actorId,
                    OccurredAtUtc: _clock.UtcNow,
                    Revision: null,
                    Payload: leaseDto),
                cancellationToken);

            var editingDto = new BlockEditingStateDto(
                WorkspaceId: workspaceId,
                PageId: lease.PageId,
                BlockId: lease.BlockId,
                UserId: lease.UserId,
                UserName: lease.HolderDisplayName,
                ConnectionId: lease.ConnectionId,
                EditorSessionId: lease.ConnectionId,
                IsEditing: false,
                OccurredAtUtc: _clock.UtcNow);

            await Clients
                .Group(RealtimeGroupNames.Page(lease.PageId))
                .SendAsync("BlockEditingStateChanged", editingDto, cancellationToken);
        }
    }

    private async Task<WorkspacePresenceDto> BuildWorkspacePresenceDtoAsync(
        Guid workspaceId,
        CancellationToken cancellationToken)
    {
        var entries = await _workspacePresenceService.GetActiveOnWorkspaceAsync(
            workspaceId,
            cancellationToken);

        var users = entries
            .GroupBy(x => new { x.UserId, x.UserName })
            .Select(group =>
            {
                var lastSeenUtc = group.Max(x => x.LastSeenUtc);

                return new WorkspacePresenceUserDto(
                    group.Key.UserId,
                    group.Key.UserName,
                    group.Count(),
                    lastSeenUtc);
            })
            .OrderByDescending(x => x.LastSeenUtc)
            .ToArray();

        return new WorkspacePresenceDto(
            workspaceId,
            users);
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

    private async Task PublishWorkspacePresenceChangedAsync(
        Guid workspaceId,
        Guid actorId,
        WorkspacePresenceDto snapshot,
        CancellationToken cancellationToken)
    {
        await _realtimePublisher.PublishToWorkspaceAsync(
            new DocumentRealtimeEnvelope(
                EventName: "WorkspacePresenceChanged",
                WorkspaceId: workspaceId,
                PageId: null,
                BlockId: null,
                ActorId: actorId,
                OccurredAtUtc: _clock.UtcNow,
                Revision: null,
                Payload: snapshot),
            cancellationToken);
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

    private static void EnsureCurrentLeaseHolder(
        BlockLeaseInfo? lease,
        Guid userId,
        string editorSessionId)
    {
        if (lease is null)
            throw new HubException("Bạn phải acquire edit lease trước khi gửi draft.");

        if (lease.UserId != userId)
            throw new HubException("Block đang được người khác chỉnh sửa.");

        if (!string.Equals(lease.ConnectionId, editorSessionId, StringComparison.Ordinal))
            throw new HubException("EditorSessionId không khớp với lease hiện tại.");
    }

    private static string? NormalizeSmallText(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var normalized = value.Trim();

        return normalized.Length <= maxLength
            ? normalized
            : normalized[..maxLength];
    }
}

