namespace AmenoLink.Configurations;

internal record CacheConfig(
    string GroupKey,
    int InactivityExpirationInSeconds,
    int TotalExpirationInSeconds
);
