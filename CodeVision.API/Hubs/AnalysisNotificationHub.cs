using Microsoft.AspNetCore.SignalR;

namespace CodeVision.API.Hubs;

public class AnalysisNotificationHub : Hub
{
    public async Task JoinAnalysisGroup(string analysisId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"analysis_{analysisId}");
    }

    public async Task LeaveAnalysisGroup(string analysisId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"analysis_{analysisId}");
    }

    public async Task JoinRepositoryGroup(string repositoryName)
    {
        // Repository ismindeki '/' karakterini '_' ile değiştir
        var groupName = $"repo_{repositoryName.Replace("/", "_")}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    public async Task LeaveRepositoryGroup(string repositoryName)
    {
        var groupName = $"repo_{repositoryName.Replace("/", "_")}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    public override async Task OnConnectedAsync()
    {
        // Tüm kullanıcıları genel gruba ekle
        await Groups.AddToGroupAsync(Context.ConnectionId, "all_users");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "all_users");
        await base.OnDisconnectedAsync(exception);
    }
}
