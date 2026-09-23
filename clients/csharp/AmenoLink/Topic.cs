namespace AmenoLink;

public class Topic<T>(string name) : ITopic<T>, ITopicSubscriber
{
    public string Name { get; } = name;
    private bool disposed;
    private readonly HashSet<Func<TopicMessage<T>, Task>> handlers = [];
    private readonly Lock lockObject = new();

    public void Subscribe(Func<TopicMessage<T>, Task> handler)
    {
        EnsureNotDisposed();
        lock (lockObject)
            handlers.Add(handler);

        AmenoLinkClient.Connection.TopicManager.SubscribeTopic(this);
    }

    public async Task Publish(T value, Message? previous = null)
    {
        EnsureNotDisposed();

        var message = new TopicMessage<T>(
            Id: Ulid.NewUlid().ToString(),
            CreatedAt: DateTime.UtcNow.ToString("o"),
            Topic: Name,
            Type: "TopicMessage",
            Payload: value,
            Previous: previous,
            AppName: AmenoLinkClient.Settings.AppName
        );

        string url = $"{AmenoLinkClient.Settings.OriginUrl}/api/topic/publish";
        await AmenoLinkClient.PostJson<object?>(url, message);
    }

    public void Dispose()
    {
        EnsureNotDisposed();
        disposed = true;

        lock (lockObject)
            handlers.Clear();

        AmenoLinkClient.Connection.TopicManager.UnsubscribeTopic(this);
        GC.SuppressFinalize(this);
    }

    void ITopicSubscriber.DispatchMessage(TopicMessage<object> message)
    {
        if (disposed)
            return;

        T? typedPayload = default;
        if (message.Payload is not null)
        {
            if (message.Payload is T payload)
                typedPayload = payload;
            else
            {
                var serialized = System.Text.Json.JsonSerializer.Serialize(message.Payload, AmenoLinkClient.JsonOptions);
                typedPayload = System.Text.Json.JsonSerializer.Deserialize<T>(serialized, AmenoLinkClient.JsonOptions);
            }
        }

        var typedMessage = new TopicMessage<T>(
            Id: message.Id,
            CreatedAt: message.CreatedAt,
            Topic: message.Topic,
            Type: message.Type,
            Payload: typedPayload,
            Previous: message.Previous,
            AppName: message.AppName
        );

        List<Func<TopicMessage<T>, Task>> targets;
        lock (lockObject)
            targets = [.. handlers];

        foreach (var handler in targets)
            _ = handler(typedMessage);
    }

    private void EnsureNotDisposed()
    {
        if (disposed)
            throw new AmenoException($"O tópico '{Name}' já foi descartado (disposed).");
    }
}
