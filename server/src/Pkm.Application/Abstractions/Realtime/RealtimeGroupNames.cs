namespace Pkm.Application.Abstractions.Realtime;

public static class RealtimeGroupNames
{
    public static string Workspace(Guid workspaceId) => $"workspace:{workspaceId:D}";
    public static string Page(Guid pageId) => $"page:{pageId:D}";
}