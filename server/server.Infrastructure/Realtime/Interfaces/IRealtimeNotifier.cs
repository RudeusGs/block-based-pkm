namespace server.Infrastructure.Realtime.Interfaces
{
    public interface IRealtimeNotifier
    {
        Task SendToWorkspaceAsync(int workspaceId, string eventName, object payload);
    }
}