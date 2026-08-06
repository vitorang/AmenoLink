using AmenoLink.Interfaces.Hub;
using AmenoLink.Interfaces.TopicManager;
using Microsoft.AspNetCore.SignalR;

namespace AmenoLink.Hubs;

internal partial class MainHub(IHubService hubService, ITopicManager topicManager) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var appName = httpContext?.Request.Query["appName"].ToString();
        if (string.IsNullOrWhiteSpace(appName))
            appName = Context.ConnectionId;

        var client = new HubClient
        {
            ConnectionId = Context.ConnectionId,
            AppName = appName
        };

        hubService.Add(client);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        hubService.Remove(Context.ConnectionId, out _);
        await base.OnDisconnectedAsync(exception);
    }
}
