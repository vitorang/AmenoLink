namespace AmenoLink.Dtos;

public record ActionRequest(
    string Id,
    string Route,
    object? Payload
);
