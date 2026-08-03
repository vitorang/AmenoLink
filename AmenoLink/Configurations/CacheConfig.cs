namespace AmenoLink.Configurations;

internal record CacheConfig(
    string GroupKey,
    int SlidingExpirationInSeconds,
    int AbsoluteExpirationInSeconds
);
