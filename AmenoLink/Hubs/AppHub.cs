using AmenoLink.Interfaces.Hub;
using AmenoLink.Interfaces.TopicManager;
using Microsoft.AspNetCore.SignalR;

namespace AmenoLink.Hubs;

internal class AppHub(IHubService hubService, ITopicManager topicManager) : Hub
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

    [HubMethodName("Topic.Subscribe")]
    public async Task<bool> SubscribeToTopic(string name)
    {
        if (!topicManager.Exists(name))
            return false;

        await Groups.AddToGroupAsync(Context.ConnectionId, hubService.TopicChannel(name));
        var client = hubService.Get(Context.ConnectionId);
        client.Topics.TryAdd(name, 0);
        return true;
    }

    [HubMethodName("Topic.Unsubscribe")]
    public async Task UnsubscribeFromTopic(string name)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, hubService.TopicChannel(name));
        var client = hubService.Get(Context.ConnectionId);
        client.Topics.TryRemove(name, out _);
    }
}
