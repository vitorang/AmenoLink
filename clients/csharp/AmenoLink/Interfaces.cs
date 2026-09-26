namespace AmenoLink;

public interface IAction
{
    string Name { get; }
    Task<T> Request<T>(object? payload = null);
    Task Queue(object? payload = null);
}

public interface ICacheWatcher : IDisposable
{
    string Group { get; }
    void All(Action<string, object?> handler);
    void Key<T>(string key, Action<T?> handler);
}

public interface ICache
{
    string Group { get; }
    ICacheWatcher Watch();
    Task<T?> Get<T>(string key);
    Task Set<T>(string key, T value);
    Task<T> GetOrCreate<T>(string key, Func<Task<T>> creator);
    Task<T> GetOrCreate<T>(string key, Func<T> creator);
    Task<Dictionary<string, object?>> All();
    Task Clear();
    Task Delete(string key);
}

public interface ITopic<T> : IDisposable
{
    string Name { get; }
    void Subscribe(Func<TopicMessage<T>, Task> handler);
    Task Publish(T value, Message? previous = null);
}

public interface IActionContext
{
    ActionRequest<object?> Request { get; }
    void Log(string message);
}

public interface IActionRouter
{
    void Add<T, R>(string route, Func<T, Task<R>> handler);
    void Add<T, R>(string route, Func<T, R> handler);
    void Serve();
}

public interface IConnection
{
    void Subscribe(Action<ConnectionStatus> listener);
    void Unsubscribe(Action<ConnectionStatus> listener);
    void UnsubscribeAll();
    Task Connect();
    Task Disconnect();
}

internal interface IConnectionManager
{
    bool IsConnected { get; }
    Task Send(string method, object? arg1 = null, CancellationToken cancellationToken = default);
    void SubscribeTopic(ITopicSubscriber topicInstance);
    void UnsubscribeTopic(ITopicSubscriber topicInstance);
    void SubscribeWatcher(ICacheWatcherSubscriber watcherInstance);
    void UnsubscribeWatcher(ICacheWatcherSubscriber watcherInstance);
}

internal interface ITopicSubscriber
{
    string Name { get; }
    void DispatchMessage(TopicMessage<object> message);
}

internal interface ICacheWatcherSubscriber
{
    string Group { get; }
    void DispatchValueChanged(string key, object? value);
}
