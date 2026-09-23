namespace AmenoLink;

internal class CacheManager(IConnectionManager connectionManager)
{
    private readonly Dictionary<string, HashSet<ICacheWatcherSubscriber>> cacheMap = [];
    private readonly Lock lockObject = new();

    public void SubscribeWatcher(ICacheWatcherSubscriber watcherInstance)
    {
        string groupName = watcherInstance.Group;
        if (string.IsNullOrEmpty(groupName))
            return;

        bool isCacheEmpty = false;
        lock (lockObject)
        {
            if (!cacheMap.TryGetValue(groupName, out var watcherSet))
            {
                watcherSet = [];
                cacheMap[groupName] = watcherSet;
            }

            isCacheEmpty = watcherSet.Count == 0;
            watcherSet.Add(watcherInstance);
        }

        if (isCacheEmpty && connectionManager.IsConnected)
            _ = connectionManager.Send("Cache.Subscribe", groupName);
    }

    public void UnsubscribeWatcher(ICacheWatcherSubscriber watcherInstance)
    {
        string groupName = watcherInstance.Group;
        if (string.IsNullOrEmpty(groupName))
            return;

        bool shouldUnsubscribeOnServer = false;
        lock (lockObject)
        {
            if (!cacheMap.TryGetValue(groupName, out var watcherSet))
                return;

            watcherSet.Remove(watcherInstance);

            if (watcherSet.Count == 0)
            {
                cacheMap.Remove(groupName);
                shouldUnsubscribeOnServer = true;
            }
        }

        if (shouldUnsubscribeOnServer && connectionManager.IsConnected)
            _ = connectionManager.Send("Cache.Unsubscribe", groupName);
    }

    public void ResubscribeAll()
    {
        if (!connectionManager.IsConnected)
            return;

        List<string> groupsToResubscribe;
        lock (lockObject)
        {
            groupsToResubscribe = cacheMap.Where(kvp => kvp.Value.Count > 0).Select(kvp => kvp.Key).ToList();
        }

        foreach (string groupName in groupsToResubscribe)
            _ = connectionManager.Send("Cache.Subscribe", groupName);
    }

    public void DispatchValueChanged(string groupName, string key, object? value)
    {
        List<ICacheWatcherSubscriber> targets;
        lock (lockObject)
        {
            if (!cacheMap.TryGetValue(groupName, out var watcherSet))
                return;

            targets = [.. watcherSet];
        }

        foreach (var watcherInstance in targets)
            watcherInstance.DispatchValueChanged(key, value);
    }
}
