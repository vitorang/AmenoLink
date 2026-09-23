namespace AmenoLink;

internal class TopicManager(IConnectionManager connectionManager)
{
    private readonly Dictionary<string, HashSet<ITopicSubscriber>> topicMap = [];
    private readonly Lock lockObject = new();

    public void SubscribeTopic(ITopicSubscriber topicInstance)
    {
        string topicName = topicInstance.Name;
        if (string.IsNullOrEmpty(topicName))
            return;

        bool isTopicEmpty = false;
        lock (lockObject)
        {
            if (!topicMap.TryGetValue(topicName, out var topicSet))
            {
                topicSet = [];
                topicMap[topicName] = topicSet;
            }

            isTopicEmpty = topicSet.Count == 0;
            topicSet.Add(topicInstance);
        }

        if (isTopicEmpty && connectionManager.IsConnected)
            _ = connectionManager.Send("Topic.Subscribe", topicName);
    }

    public void UnsubscribeTopic(ITopicSubscriber topicInstance)
    {
        string topicName = topicInstance.Name;
        if (string.IsNullOrEmpty(topicName))
            return;

        bool shouldUnsubscribeOnServer = false;
        lock (lockObject)
        {
            if (!topicMap.TryGetValue(topicName, out var topicSet))
                return;

            topicSet.Remove(topicInstance);

            if (topicSet.Count == 0)
            {
                topicMap.Remove(topicName);
                shouldUnsubscribeOnServer = true;
            }
        }

        if (shouldUnsubscribeOnServer && connectionManager.IsConnected)
            _ = connectionManager.Send("Topic.Unsubscribe", topicName);
    }

    public void ResubscribeAll()
    {
        if (!connectionManager.IsConnected)
            return;

        List<string> topicsToResubscribe;
        lock (lockObject)
        {
            topicsToResubscribe = topicMap.Where(kvp => kvp.Value.Count > 0).Select(kvp => kvp.Key).ToList();
        }

        foreach (string topicName in topicsToResubscribe)
            _ = connectionManager.Send("Topic.Subscribe", topicName);
    }

    public void DispatchMessage(string topicName, TopicMessage<object> topicMessage)
    {
        List<ITopicSubscriber> targets;
        lock (lockObject)
        {
            if (!topicMap.TryGetValue(topicName, out var topicSet))
                return;

            targets = [.. topicSet];
        }

        foreach (var topicInstance in targets)
            topicInstance.DispatchMessage(topicMessage);
    }
}
