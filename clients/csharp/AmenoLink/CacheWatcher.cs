namespace AmenoLink;

public class CacheWatcher(string group) : ICacheWatcher, ICacheWatcherSubscriber
{
    public string Group { get; } = group;
    private bool disposed;
    private readonly HashSet<Action<string, object?>> allHandlers = [];
    private readonly Dictionary<string, List<Action<object?>>> keyHandlers = [];
    private readonly Lock lockObject = new();

    public void All(Action<string, object?> handler)
    {
        EnsureNotDisposed();
        lock (lockObject)
            allHandlers.Add(handler);

        AmenoLinkClient.ConnectionManager.CacheManager.SubscribeWatcher(this);
    }

    public void Key<T>(string key, Action<T?> handler)
    {
        EnsureNotDisposed();
        lock (lockObject)
        {
            if (!keyHandlers.TryGetValue(key, out var list))
            {
                list = [];
                keyHandlers[key] = list;
            }

            list.Add(rawValue =>
            {
                if (rawValue is null)
                {
                    handler(default);
                    return;
                }

                if (rawValue is T typed)
                {
                    handler(typed);
                    return;
                }

                var serialized = System.Text.Json.JsonSerializer.Serialize(rawValue, AmenoLinkClient.JsonOptions);
                var deserialized = System.Text.Json.JsonSerializer.Deserialize<T>(serialized, AmenoLinkClient.JsonOptions);
                handler(deserialized);
            });
        }

        AmenoLinkClient.ConnectionManager.CacheManager.SubscribeWatcher(this);
    }

    public void Dispose()
    {
        EnsureNotDisposed();
        disposed = true;

        lock (lockObject)
        {
            allHandlers.Clear();
            keyHandlers.Clear();
        }

        AmenoLinkClient.ConnectionManager.CacheManager.UnsubscribeWatcher(this);
        GC.SuppressFinalize(this);
    }

    void ICacheWatcherSubscriber.DispatchValueChanged(string key, object? value)
    {
        if (disposed)
            return;

        List<Action<string, object?>> allTargets;
        List<Action<object?>> keyTargets = [];

        lock (lockObject)
        {
            allTargets = [.. allHandlers];
            if (keyHandlers.TryGetValue(key, out var list))
                keyTargets = [.. list];
        }

        foreach (var handler in allTargets)
            handler(key, value);

        foreach (var handler in keyTargets)
            handler(value);
    }

    private void EnsureNotDisposed()
    {
        if (disposed)
            throw new AmenoException($"O observador do grupo de cache '{Group}' já foi descartado (disposed).");
    }
}
