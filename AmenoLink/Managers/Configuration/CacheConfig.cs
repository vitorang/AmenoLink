namespace AmenoLink.Managers.Configuration;

internal record CacheConfig(
    string GroupName,
    int InactivityExpirationInSeconds,
    int TotalExpirationInSeconds
);
