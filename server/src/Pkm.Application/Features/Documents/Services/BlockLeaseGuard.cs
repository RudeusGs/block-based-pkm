using Pkm.Application.Abstractions.Realtime;
using Pkm.Application.Common.Results;

namespace Pkm.Application.Features.Documents.Services;

public static class BlockLeaseGuard
{
    public static Error? ValidateWriteLease(
        BlockLeaseInfo? lease,
        Guid currentUserId,
        string? editorSessionId)
    {
        if (string.IsNullOrWhiteSpace(editorSessionId))
            return DocumentErrors.InvalidEditorSessionId;

        if (lease is null)
            return DocumentErrors.LeaseRequired;

        if (lease.UserId != currentUserId)
            return DocumentErrors.LeaseConflict;

        if (!string.Equals(lease.ConnectionId, editorSessionId, StringComparison.Ordinal))
            return DocumentErrors.LeaseSessionMismatch;

        return null;
    }
}