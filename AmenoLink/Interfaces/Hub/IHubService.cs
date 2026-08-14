using AmenoLink.Dtos;
using AmenoLink.Hubs;

namespace AmenoLink.Interfaces.Hub;

internal interface IHubService
{
    string TopicChannel(string name);
    bool Add(HubClient client);
    bool Remove(string connectionId, out HubClient? client);
    HubClient Get(string connectionId);
    HubClient[] ListSubscribers(string topicName);
    Task PublishToTopic(string name, TopicMessage message);
    Task RemoveTopicSubscribers(string name);
}
