using Microsoft.AspNetCore.SignalR;

namespace AmenoLink.Hubs;

internal partial class MainHub
{
    [HubMethodName("Topic.Subscribe")]
    public async Task<bool> SubscribeToTopic(string name)
    {
        if (!topicManager.Exists(name))
            return false;

        await Groups.AddToGroupAsync(Context.ConnectionId, hubService.TopicChannel(name));
        var client = hubService.Get(Context.ConnectionId);
        client.Topics.Add(name);
        return true;
    }

    [HubMethodName("Topic.Unsubscribe")]
    public async Task UnsubscribeFromTopic(string name)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, hubService.TopicChannel(name));
        var client = hubService.Get(Context.ConnectionId);
        client.Topics.Remove(name);
    }
}
