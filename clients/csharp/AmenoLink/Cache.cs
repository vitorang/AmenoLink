using System.Text.Json;

namespace AmenoLink;

public class Cache(string group) : ICache
{
    public string Group { get; } = group;

    public ICacheWatcher Watch() => new CacheWatcher(Group);

    public async Task<T?> Get<T>(string key)
    {
        string url = BuildCacheUrl(key);
        return await Request<T?>("GET", url);
    }

    public async Task Set<T>(string key, T value)
    {
        string url = BuildCacheUrl(key);
        await Request<object?>("POST", url, value);
    }

    public async Task<T> GetOrCreate<T>(string key, Func<Task<T>> creator)
    {
        var cached = await Get<T>(key);
        if (cached is not null)
            return cached;

        var created = await creator();
        await Set(key, created);
        return created;
    }

    public async Task<T> GetOrCreate<T>(string key, Func<T> creator)
    {
        var cached = await Get<T>(key);
        if (cached is not null)
            return cached;

        var created = creator();
        await Set(key, created);
        return created;
    }

    public async Task<Dictionary<string, object?>> All()
    {
        string url = BuildCacheAllUrl();
        var result = await Request<Dictionary<string, object?>>("GET", url);
        return result ?? [];
    }

    public async Task Clear()
    {
        string url = BuildCacheAllUrl();
        await Request<object?>("DELETE", url);
    }

    public async Task Delete(string key)
    {
        string url = BuildCacheUrl(key);
        await Request<object?>("DELETE", url);
    }

    private string BuildCacheUrl(string key)
    {
        string encodedGroup = Uri.EscapeDataString(Group);
        string encodedKey = Uri.EscapeDataString(key);
        return $"{AmenoLinkClient.Settings.OriginUrl}/api/cache?groupName={encodedGroup}&key={encodedKey}";
    }

    private string BuildCacheAllUrl()
    {
        string encodedGroup = Uri.EscapeDataString(Group);
        return $"{AmenoLinkClient.Settings.OriginUrl}/api/cache/all?groupName={encodedGroup}";
    }

    private static async Task<TResult?> Request<TResult>(string method, string url, object? data = null)
    {
        try
        {
            using var request = new HttpRequestMessage(new HttpMethod(method), url);
            if (data is not null)
            {
                string json = JsonSerializer.Serialize(data, AmenoLinkClient.JsonOptions);
                request.Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");
            }

            using var client = new HttpClient();
            var response = await client.SendAsync(request);

            if (!response.IsSuccessStatusCode)
                throw new AmenoException($"Status HTTP inesperado: {(int)response.StatusCode}");

            string content = await response.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(content))
                return default;

            return JsonSerializer.Deserialize<TResult>(content, AmenoLinkClient.JsonOptions);
        }
        catch (AmenoException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new AmenoException($"Erro na operação de cache: {exception.Message}", exception);
        }
    }
}
