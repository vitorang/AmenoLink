using System.Text.Json;

namespace AmenoLink;

public class ActionContext(ActionRequest<object?> request) : IActionContext
{
    public ActionRequest<object?> Request { get; } = request;

    public void Log(string message)
    {
        ActionServer.SendMessage(ActionServer.OnActionLogged, message);
    }
}

public class ActionRouter : IActionRouter
{
    private record RouteEntry(string Route, Func<object?, Task<string>> Handler);

    private readonly List<RouteEntry> routes = [];

    public void Add<T, R>(string route, Func<T, Task<R>> handler)
    {
        routes.Add(new RouteEntry(route, async rawPayload =>
        {
            T? input = DeserializePayload<T>(rawPayload);
            R result = await handler(input!);
            return JsonSerializer.Serialize(result, AmenoLinkClient.JsonOptions);
        }));
    }

    public void Add<T, R>(string route, Func<T, R> handler)
    {
        routes.Add(new RouteEntry(route, rawPayload =>
        {
            T? input = DeserializePayload<T>(rawPayload);
            R result = handler(input!);
            string json = JsonSerializer.Serialize(result, AmenoLinkClient.JsonOptions);
            return Task.FromResult(json);
        }));
    }

    public void Serve()
    {
        ActionServer.Serve(this);
    }

    internal async Task<string> Execute(ActionRequest<object?> request)
    {
        foreach (var entry in routes)
        {
            if (entry.Route == request.Route)
                return await entry.Handler(request.Payload);
        }

        throw new AmenoException($"Rota '{request.Route}' não encontrada");
    }

    private static T? DeserializePayload<T>(object? rawPayload)
    {
        if (rawPayload is null)
            return default;

        if (rawPayload is T typed)
            return typed;

        if (rawPayload is JsonElement jsonElement)
            return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), AmenoLinkClient.JsonOptions);

        string json = JsonSerializer.Serialize(rawPayload, AmenoLinkClient.JsonOptions);
        return JsonSerializer.Deserialize<T>(json, AmenoLinkClient.JsonOptions);
    }
}

public static class ActionServer
{
    public const string OnStartupSuccess = "[AmenoLink.StartupSuccess]";
    public const string OnActionSuccess = "[AmenoLink.ActionSuccess]";
    public const string OnActionError = "[AmenoLink.ActionError]";
    public const string OnActionLogged = "[AmenoLink.ActionLog]";

    private static readonly AsyncLocal<ActionContext?> CurrentActionContext = new();

    public static ActionContext ActionContext
    {
        get
        {
            var current = CurrentActionContext.Value;
            if (current == null)
                throw new InvalidOperationException("Nenhuma ação está em execução no momento.");

            return current;
        }
    }

    public static void Serve(ActionRouter router)
    {
        SendMessage(OnStartupSuccess, AmenoLinkClient.Settings.AppName);

        string? line;
        while ((line = Console.ReadLine()) != null)
        {
            string trimmedLine = line.Trim();
            if (string.IsNullOrEmpty(trimmedLine))
                continue;

            try
            {
                byte[] decodedBytes = Convert.FromBase64String(trimmedLine);
                string decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);
                var request = JsonSerializer.Deserialize<ActionRequest<object?>>(decodedJson, AmenoLinkClient.JsonOptions);

                if (request == null)
                    continue;

                var context = new ActionContext(request);
                CurrentActionContext.Value = context;

                string resultJson = router.Execute(request).GetAwaiter().GetResult();
                SendMessage(OnActionSuccess, resultJson);
            }
            catch (Exception exception)
            {
                SendMessage(OnActionError, exception.Message);
            }
            finally
            {
                CurrentActionContext.Value = null;
            }
        }
    }

    internal static void SendMessage(string prefix, string message)
    {
        byte[] payloadBytes = System.Text.Encoding.UTF8.GetBytes(message);
        string payload = Convert.ToBase64String(payloadBytes);
        Console.WriteLine($"{prefix}{payload}");
    }
}
