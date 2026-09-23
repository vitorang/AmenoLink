namespace AmenoLink;
 
public record Message(
    string Id,
    string CreatedAt,
    string Type,
    Message? Previous = null,
    string? AppName = null
);

public record ActionRequest<T>(
    string Id,
    string CreatedAt,
    string Route,
    string Type = "ActionRequest",
    T? Payload = default,
    Message? Previous = null,
    string? AppName = null
) : Message(Id, CreatedAt, Type, Previous, AppName);

public record ActionError(
    string Type,
    string Message
);

public record ActionResponse<T>(
    string Id,
    string CreatedAt,
    bool Success,
    string[] Logs,
    string Type = "ActionResponse",
    T? Result = default,
    ActionError? Error = null,
    Message? Previous = null,
    string? AppName = null
) : Message(Id, CreatedAt, Type, Previous, AppName);

public record TopicMessage<T>(
    string Id,
    string CreatedAt,
    string Topic,
    string Type = "TopicMessage",
    T? Payload = default,
    Message? Previous = null,
    string? AppName = null
) : Message(Id, CreatedAt, Type, Previous, AppName);

public record Resources(
    string[] Actions,
    string[] Caches,
    string[] Topics,
    string Version
);

public record CacheItem<T>(
    string Group,
    string Key,
    T? Value = default
);

public record CacheEvent<T>(
    string Group,
    string Key,
    T? Value = default
);
