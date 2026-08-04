namespace AmenoLink.Configurations;

internal record CacheConfig(
    string GroupName,
    int InactivityExpirationInSeconds,
    int TotalExpirationInSeconds
);
