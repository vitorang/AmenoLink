using System.Text.Json;
using AmenoLink.Dtos;
using AmenoLink.Hubs;

namespace AmenoLink.Interfaces.Hub;

internal interface IHubService
{
    string TopicChannel(string name);
    string CacheChannel(string groupName);
    bool Add(HubClient client);
    bool Remove(string connectionId, out HubClient? client);
    HubClient Get(string connectionId);
    HubClient[] ListSubscribers(string topicName);
    HubClient[] ListCacheSubscribers(string groupName);
    Task PublishToTopic(string name, TopicMessage message);
    Task PublishToCacheGroup(string groupName, string key, JsonElement? value);
    Task RemoveTopicSubscribers(string name);
    Task RemoveCacheSubscribers(string groupName);
}
