namespace AmenoLink.Configurations;

internal record HubConfig(
    string Id,
    HubConfig.CacheOption[] CacheOptions
)
{
    internal const string DefaultId = "__default__";

    internal record CacheOption(
        string Id,
        int? SlidingExpirationInSeconds,
        int? AbsoluteExpirationInSeconds
    );
}
