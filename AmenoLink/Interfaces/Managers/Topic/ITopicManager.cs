using AmenoLink.Dtos;
using AmenoLink.Hubs;

namespace AmenoLink.Interfaces.Managers.Topic;

internal interface ITopicManager
{
    void LoadConfigurations();
    bool Exists(string topicName);
    HubClient[] ListSubscribers(string topicName);
    Task Publish(string topicName, TopicMessage message);
    TopicMessage[] GetRecentMessages(string topicName);
}
