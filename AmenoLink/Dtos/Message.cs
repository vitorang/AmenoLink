namespace AmenoLink.Dtos;

public record Message(
    string Id = "",
    Message? Previous = null,
    string Type = "Message",
    DateTimeOffset CreatedAt = default
)
{
    public string Id { get; init; } = string.IsNullOrEmpty(Id) ? System.Ulid.NewUlid().ToString() : Id;
    public DateTimeOffset CreatedAt { get; init; } = CreatedAt == default ? DateTimeOffset.UtcNow : CreatedAt;
}