namespace AmenoLink.Dtos;

public record TopicMessage(
    string Topic,
    object? Payload,
    string Id = "",
    Message? Previous = null,
    string Type = "TopicMessage",
    DateTimeOffset CreatedAt = default
) : Message(Id, Previous, Type, CreatedAt);