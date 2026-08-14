using System.Collections.Concurrent;
using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Hubs;
using AmenoLink.Interfaces.Configurations;
using AmenoLink.Interfaces.Hub;
using AmenoLink.Interfaces.TopicManager;
using Message = AmenoLink.Dtos.Message;

namespace AmenoLink.TopicManager;

internal class TopicManager(IHubService hubService, IConfigurationManager configurationManager) : ITopicManager
{
    private TopicConfig[] topicConfigs = [];
    private readonly ConcurrentDictionary<string, ConcurrentQueue<TopicMessage>> recentMessagesByTopic = new(StringComparer.Ordinal);

    public void LoadConfigurations()
    {
        var newConfigs = ConfigPathProvider.Topic.LoadConfigs();
        RemoveUnusedTopics(newConfigs);
        topicConfigs = newConfigs;
    }

    public bool Exists(string topicName)
    {
        return topicConfigs.Any(c => c.Name == topicName);
    }

    public HubClient[] ListSubscribers(string topicName)
    {
        return hubService.ListSubscribers(topicName);
    }

    public async Task Publish(string topicName, TopicMessage message)
    {
        int depth = GetMessageDepth(message);
        if (depth > configurationManager.General.MaxMessageDepth)
            throw new InvalidOperationException($"A profundidade da mensagem ({depth}) excedeu o limite máximo configurado ({configurationManager.General.MaxMessageDepth}).");

        AddRecentMessage(topicName, message);

        await hubService.PublishToTopic(topicName, message);
    }

    public TopicMessage[] GetRecentMessages(string topicName)
    {
        if (!recentMessagesByTopic.TryGetValue(topicName, out var queue))
            return [];

        return queue.Reverse().ToArray();
    }

    private void AddRecentMessage(string topicName, TopicMessage message)
    {
        var queue = recentMessagesByTopic.GetOrAdd(topicName, _ => new ConcurrentQueue<TopicMessage>());
        queue.Enqueue(message);

        int maxHistorySize = configurationManager.General.MaxTopicHistorySize;
        for (int itemsToRemove = queue.Count - maxHistorySize; itemsToRemove > 0; itemsToRemove--)
        {
            if (!queue.TryDequeue(out _))
                break;
        }
    }

    private void RemoveUnusedTopics(TopicConfig[] newConfigs)
    {
        var newNames = newConfigs.Select(c => c.Name).ToHashSet();
        foreach (var oldConfig in topicConfigs)
        {
            if (!newNames.Contains(oldConfig.Name))
            {
                _ = hubService.RemoveTopicSubscribers(oldConfig.Name);
                recentMessagesByTopic.TryRemove(oldConfig.Name, out _);
            }
        }
    }

    private static int GetMessageDepth(Message? message)
    {
        int depth = 0;
        var current = message;
        while (current is not null)
        {
            depth++;
            current = current.Previous;
        }

        return depth;
    }
}
