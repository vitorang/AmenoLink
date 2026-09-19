namespace AmenoLink.Dtos.Configuration;

internal record CacheConfig(
    string GroupName,
    int InactivityExpirationInSeconds,
    int TotalExpirationInSeconds
);
