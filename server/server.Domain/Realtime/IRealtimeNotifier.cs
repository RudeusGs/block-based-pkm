namespace server.Domain.Realtime;

public interface IRealtimeNotifier
{
    Task SendToWorkspaceAsync(int workspaceId, string eventName, object payload);
}
