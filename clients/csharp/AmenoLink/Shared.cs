using System.Net.Http.Json;
using System.Text.Json;

namespace AmenoLink;

public class AmenoException : Exception
{
    public AmenoException(string message) : base(message)
    {
    }

    public AmenoException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public class ClientSetup
{
    public string OriginUrl { get; set; } = "http://localhost:13545";
    public string AppName { get; set; } = string.Empty;
}

public static class AmenoLinkClient
{
    public static ClientSetup Settings { get; } = new();
    public static ResourceManager Resources { get; } = new();
    internal static ConnectionManager ConnectionManager { get; } = new();
    public static IConnection Connection { get; } = new Connection(ConnectionManager);


    private static readonly HttpClient HttpClient = new();
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Setup(string? originUrl = null, string? appName = null)
    {
        if (originUrl != null)
            Settings.OriginUrl = originUrl.TrimEnd('/');

        if (appName != null)
            Settings.AppName = appName;
    }

    public static ActionRouter Actions { get; } = new();
    public static ActionContext ActionContext => ActionServer.ActionContext;

    public static IAction Action(string name)
    {
        Resources.Actions.Add(name);
        return new ActionClient(name);
    }

    public static ICache Cache(string group)
    {
        Resources.Caches.Add(group);
        return new Cache(group);
    }

    public static ITopic<T> Topic<T>(string name)
    {
        Resources.Topics.Add(name);
        return new Topic<T>(name);
    }

    public static Task EnsureReady() => Resources.EnsureReady();

    internal static async Task<T> PostJson<T>(string url, object data)
    {
        var response = await HttpClient.PostAsJsonAsync(url, data, JsonOptions);
        var content = await response.Content.ReadAsStringAsync();

        if (string.IsNullOrWhiteSpace(content))
            return default!;

        return JsonSerializer.Deserialize<T>(content, JsonOptions)!;
    }
}
