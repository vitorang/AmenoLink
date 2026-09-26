namespace AmenoLink;

internal class Connection : IConnection
{
    private const int MaxReconnectAttempts = 5;
    private const double ConnectionTimeoutSeconds = 5.0;

    private readonly HashSet<Action<ConnectionStatus>> listeners = [];
    private readonly Lock lockObject = new();
    private readonly ConnectionManager connectionManager;

    internal Connection(ConnectionManager connectionManager)
    {
        this.connectionManager = connectionManager;
    }

    public void Subscribe(Action<ConnectionStatus> listener)
    {
        lock (lockObject)
            listeners.Add(listener);
    }

    public void Unsubscribe(Action<ConnectionStatus> listener)
    {
        lock (lockObject)
            listeners.Remove(listener);
    }

    public void UnsubscribeAll()
    {
        lock (lockObject)
            listeners.Clear();
    }

    private void OnConnectionChanged(ConnectionStatus status)
    {
        List<Action<ConnectionStatus>> targets;
        lock (lockObject)
            targets = [.. listeners];

        foreach (var listener in targets)
            listener(status);
    }

    public Task Connect()
    {
        return connectionManager.Connect(
            onStatusChange: OnConnectionChanged,
            maxAttempts: MaxReconnectAttempts,
            timeoutSeconds: ConnectionTimeoutSeconds
        );
    }

    public Task Disconnect()
    {
        return connectionManager.Disconnect();
    }
}
