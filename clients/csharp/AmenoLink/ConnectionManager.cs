using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;

namespace AmenoLink;

public enum ConnectionStatus
{
    Disconnected,
    Connecting,
    Connected
}

public class ConnectionManager : IConnectionManager
{
    private HubConnection? connection;
    public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;
    public bool IsConnected => Status == ConnectionStatus.Connected;

    internal TopicManager TopicManager { get; }
    internal CacheManager CacheManager { get; }

    private readonly HashSet<Action<ConnectionStatus>> statusListeners = [];
    private readonly Lock lockObject = new();

    public ConnectionManager()
    {
        TopicManager = new TopicManager(this);
        CacheManager = new CacheManager(this);
    }

    public async Task Connect(Action<ConnectionStatus>? onStatusChange = null, int maxAttempts = 5, double timeoutSeconds = 5.0)
    {
        if (onStatusChange != null)
        {
            lock (lockObject)
                statusListeners.Add(onStatusChange);
        }

        if (connection != null)
            return;

        UpdateStatus(ConnectionStatus.Connecting);

        string url = $"{AmenoLinkClient.Settings.OriginUrl}/app-hub";
        if (!string.IsNullOrEmpty(AmenoLinkClient.Settings.AppName))
        {
            string encodedAppName = Uri.EscapeDataString(AmenoLinkClient.Settings.AppName);
            url = $"{url}?appName={encodedAppName}";
        }

        var retryDelays = Enumerable.Repeat(TimeSpan.FromSeconds(2), maxAttempts).ToArray();

        connection = new HubConnectionBuilder()
            .WithUrl(url)
            .WithAutomaticReconnect(retryDelays)
            .AddJsonProtocol(options =>
            {
                options.PayloadSerializerOptions.PropertyNameCaseInsensitive = true;
                options.PayloadSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
            })
            .Build();

        connection.Reconnecting += _ =>
        {
            UpdateStatus(ConnectionStatus.Connecting);
            return Task.CompletedTask;
        };

        connection.Reconnected += _ =>
        {
            OnConnectionReconnected();
            return Task.CompletedTask;
        };

        connection.Closed += _ =>
        {
            OnConnectionClosed();
            return Task.CompletedTask;
        };

        connection.On<string, TopicMessage<object>>("Topic.Message", (topicName, message) =>
        {
            TopicManager.DispatchMessage(topicName, message);
        });

        connection.On<string, string, object?>("Cache.ValueChanged", (groupName, key, value) =>
        {
            CacheManager.DispatchValueChanged(groupName, key, value);
        });

        using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            await connection.StartAsync(cancellationTokenSource.Token);
            OnConnectionOpened();
        }
        catch
        {
            UpdateStatus(ConnectionStatus.Disconnected);
        }
    }

    public async Task Disconnect()
    {
        var currentConnection = connection;
        if (currentConnection != null)
        {
            await currentConnection.StopAsync();
            await currentConnection.DisposeAsync();
            connection = null;
        }

        UpdateStatus(ConnectionStatus.Disconnected);
    }

    private void OnConnectionOpened()
    {
        UpdateStatus(ConnectionStatus.Connected);
        TopicManager.ResubscribeAll();
        CacheManager.ResubscribeAll();
    }

    private void OnConnectionReconnected()
    {
        UpdateStatus(ConnectionStatus.Connected);
        TopicManager.ResubscribeAll();
        CacheManager.ResubscribeAll();
    }

    private void OnConnectionClosed()
    {
        UpdateStatus(ConnectionStatus.Disconnected);
    }

    private void UpdateStatus(ConnectionStatus newStatus)
    {
        lock (lockObject)
        {
            if (Status == newStatus)
                return;

            Status = newStatus;
        }

        List<Action<ConnectionStatus>> listeners;
        lock (lockObject)
            listeners = [.. statusListeners];

        foreach (var listener in listeners)
            listener(newStatus);
    }

    public async Task Send(string method, object? arg1 = null, CancellationToken cancellationToken = default)
    {
        var currentConnection = connection;
        if (currentConnection == null || !IsConnected)
            throw new AmenoException($"Não é possível enviar '{method}': cliente desconectado.");

        if (arg1 != null)
            await currentConnection.SendAsync(method, arg1, cancellationToken);
        else
            await currentConnection.SendAsync(method, cancellationToken);
    }

    void IConnectionManager.SubscribeTopic(ITopicSubscriber topicInstance) => TopicManager.SubscribeTopic(topicInstance);
    void IConnectionManager.UnsubscribeTopic(ITopicSubscriber topicInstance) => TopicManager.UnsubscribeTopic(topicInstance);
    void IConnectionManager.SubscribeWatcher(ICacheWatcherSubscriber watcherInstance) => CacheManager.SubscribeWatcher(watcherInstance);
    void IConnectionManager.UnsubscribeWatcher(ICacheWatcherSubscriber watcherInstance) => CacheManager.UnsubscribeWatcher(watcherInstance);
}
