using System.Text.Json;
using Pkm.Application.Common.Abstractions.Caching;
using Pkm.Application.Features.Messaging.Models;
using Pkm.Application.Features.Workspaces;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Features.Messaging.Commands;

internal static class WorkspaceShareCommandHelpers
{
    private static readonly JsonSerializerOptions WorkspaceShareJsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static string SerializePayload(WorkspaceSharePayloadDto payload)
        => JsonSerializer.Serialize(payload, WorkspaceShareJsonOptions);

    public static WorkspaceSharePayloadDto? DeserializePayload(string? body)
    {
        if (string.IsNullOrWhiteSpace(body))
            return null;

        try
        {
            return JsonSerializer.Deserialize<WorkspaceSharePayloadDto>(body, WorkspaceShareJsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public static bool TryParseShareRole(string? rawRole, out WorkspaceRole role)
    {
        role = WorkspaceRole.Viewer;

        if (string.IsNullOrWhiteSpace(rawRole))
            return true;

        switch (rawRole.Trim().ToLowerInvariant())
        {
            case "member":
                role = WorkspaceRole.Member;
                return true;

            case "viewer":
                role = WorkspaceRole.Viewer;
                return true;

            default:
                return false;
        }
    }

    public static async Task InvalidateWorkspaceMembershipCachesAsync(
        IApplicationCache cache,
        ICacheKeyFactory cacheKeyFactory,
        Guid workspaceId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        await cache.RemoveAsync(
            WorkspaceCacheKeys.Members(cacheKeyFactory, workspaceId),
            cancellationToken);

        await cache.RemoveAsync(
            WorkspaceCacheKeys.Access(cacheKeyFactory, workspaceId, userId),
            cancellationToken);

        await cache.SetAsync(
            WorkspaceCacheKeys.UserListVersion(cacheKeyFactory, userId),
            Guid.NewGuid().ToString("N"),
            cancellationToken: cancellationToken);
    }
}
