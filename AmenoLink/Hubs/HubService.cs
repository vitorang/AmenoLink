using AmenoLink.Dtos;
using AmenoLink.Interfaces.Hub;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Text.Json;

namespace AmenoLink.Hubs;

internal class HubService(IHubContext<AppHub> hubContext) : IHubService
{
    private const string TopicMessageEvent = "Topic.Message";
    private const string CacheValueChangedEvent = "Cache.ValueChanged";

    private readonly ConcurrentDictionary<string, HubClient> clients = new();

    public string TopicChannel(string name) => $"topic:{name}";

    public string CacheChannel(string groupName) => $"cache:{groupName}";

    public bool Add(HubClient client) => clients.TryAdd(client.ConnectionId, client);

    public bool Remove(string connectionId, out HubClient? client) => clients.TryRemove(connectionId, out client);

    public HubClient Get(string connectionId)
    {
        if (clients.TryGetValue(connectionId, out var client))
            return client;

        throw new KeyNotFoundException($"O cliente com ID de conexão '{connectionId}' não foi encontrado.");
    }

    public HubClient[] ListSubscribers(string topicName)
    {
        return clients.Values.Where(c => c.Topics.ContainsKey(topicName)).ToArray();
    }

    public HubClient[] ListCacheSubscribers(string groupName)
    {
        return clients.Values.Where(c => c.CacheGroups.ContainsKey(groupName)).ToArray();
    }

    public Task PublishToTopic(string name, TopicMessage message)
    {
        return hubContext.Clients.Group(TopicChannel(name)).SendAsync(TopicMessageEvent, name, message);
    }

    public Task PublishToCacheGroup(string groupName, string key, JsonElement? value)
    {
        return hubContext.Clients.Group(CacheChannel(groupName)).SendAsync(CacheValueChangedEvent, groupName, key, value);
    }

    public async Task RemoveTopicSubscribers(string name)
    {
        var subscribers = ListSubscribers(name);
        foreach (var client in subscribers)
        {
            client.Topics.TryRemove(name, out _);
            await hubContext.Groups.RemoveFromGroupAsync(client.ConnectionId, TopicChannel(name));
        }
    }

    public async Task RemoveCacheSubscribers(string groupName)
    {
        var subscribers = ListCacheSubscribers(groupName);
        foreach (var client in subscribers)
        {
            client.CacheGroups.TryRemove(groupName, out _);
            await hubContext.Groups.RemoveFromGroupAsync(client.ConnectionId, CacheChannel(groupName));
        }
    }
}
