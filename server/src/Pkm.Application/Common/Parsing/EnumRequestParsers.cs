using Pkm.Domain.Recommendations;
using Pkm.Domain.Tasks;
using Pkm.Domain.Workspaces;

namespace Pkm.Application.Common.Parsing;

public static class EnumRequestParsers
{
    public static bool TryParseWorkspaceRole(string? rawRole, out WorkspaceRole role)
    {
        role = default;

        if (string.IsNullOrWhiteSpace(rawRole))
            return false;

        return rawRole.Trim().ToLowerInvariant() switch
        {
            "owner" => Set(out role, WorkspaceRole.Owner),
            "manager" => Set(out role, WorkspaceRole.Manager),
            "member" => Set(out role, WorkspaceRole.Member),
            "viewer" => Set(out role, WorkspaceRole.Viewer),
            _ => false
        };
    }

    public static bool TryParseWorkspaceVisibility(
        string? rawVisibility,
        out WorkspaceVisibility? visibility)
    {
        visibility = null;

        if (rawVisibility is null)
            return true;

        if (string.IsNullOrWhiteSpace(rawVisibility))
            return false;

        return rawVisibility.Trim().ToLowerInvariant() switch
        {
            "private" => SetNullable(out visibility, WorkspaceVisibility.Private),
            "public" => SetNullable(out visibility, WorkspaceVisibility.Public),
            _ => false
        };
    }

    public static WorkspaceVisibility WorkspaceVisibilityOrDefault(WorkspaceVisibility? visibility)
        => visibility ?? WorkspaceVisibility.Private;

    public static bool TryParseTaskPriority(string? raw, out PriorityWorkTask priority)
    {
        priority = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return raw.Trim().ToLowerInvariant() switch
        {
            "low" => Set(out priority, PriorityWorkTask.Low),
            "medium" => Set(out priority, PriorityWorkTask.Medium),
            "high" => Set(out priority, PriorityWorkTask.High),
            _ => false
        };
    }

    public static bool TryParseNullableTaskPriority(
        string? raw,
        out PriorityWorkTask? priority)
    {
        priority = null;

        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (!TryParseTaskPriority(raw, out var parsed))
            return false;

        priority = parsed;
        return true;
    }

    public static bool TryParseTaskStatus(string? raw, out StatusWorkTask status)
    {
        status = default;

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return raw.Trim().ToLowerInvariant().Replace("-", "_") switch
        {
            "todo" or "to_do" => Set(out status, StatusWorkTask.ToDo),
            "doing" => Set(out status, StatusWorkTask.Doing),
            "done" => Set(out status, StatusWorkTask.Done),
            _ => false
        };
    }

    public static bool TryParseNullableTaskStatus(
        string? raw,
        out StatusWorkTask? status)
    {
        status = null;

        if (string.IsNullOrWhiteSpace(raw))
            return true;

        if (!TryParseTaskStatus(raw, out var parsed))
            return false;

        status = parsed;
        return true;
    }

    public static bool TryParseNullableRecommendationStatus(
        string? raw,
        out StatusTaskRecommendation? status)
    {
        status = null;

        if (string.IsNullOrWhiteSpace(raw))
            return true;

        return raw.Trim().ToLowerInvariant() switch
        {
            "pending" => SetNullable(out status, StatusTaskRecommendation.Pending),
            "accepted" => SetNullable(out status, StatusTaskRecommendation.Accepted),
            "rejected" => SetNullable(out status, StatusTaskRecommendation.Rejected),
            "completed" => SetNullable(out status, StatusTaskRecommendation.Completed),
            "expired" => SetNullable(out status, StatusTaskRecommendation.Expired),
            _ => false
        };
    }

    private static bool Set<T>(out T target, T value)
    {
        target = value;
        return true;
    }

    private static bool SetNullable<T>(out T? target, T value)
        where T : struct
    {
        target = value;
        return true;
    }
}
