using AmenoLink.Interfaces.Hub;
using AmenoLink.Interfaces.Managers.Cache;
using AmenoLink.Interfaces.Managers.Topic;
using Microsoft.AspNetCore.SignalR;

namespace AmenoLink.Hubs;

internal class AppHub(
    IHubService hubService,
    ITopicManager topicManager,
    ICacheManager cacheManager
) : Hub
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

    [HubMethodName("Cache.Subscribe")]
    public async Task<bool> SubscribeToCache(string groupName)
    {
        if (!cacheManager.Exists(groupName))
            return false;

        await Groups.AddToGroupAsync(Context.ConnectionId, hubService.CacheChannel(groupName));
        var client = hubService.Get(Context.ConnectionId);
        client.CacheGroups.TryAdd(groupName, 0);
        return true;
    }

    [HubMethodName("Cache.Unsubscribe")]
    public async Task UnsubscribeFromCache(string groupName)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, hubService.CacheChannel(groupName));
        var client = hubService.Get(Context.ConnectionId);
        client.CacheGroups.TryRemove(groupName, out _);
    }
}
