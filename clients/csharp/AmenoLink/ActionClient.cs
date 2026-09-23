using System.Text.Json;

namespace AmenoLink;

public class ActionClient(string name) : IAction
{
    public string Name { get; } = name;

    public async Task<T> Request<T>(object? payload = null)
    {
        var requestDto = new ActionRequest<object?>(
            Id: Ulid.NewUlid().ToString(),
            CreatedAt: DateTime.UtcNow.ToString("o"),
            Route: Name,
            Type: "ActionRequest",
            Payload: payload,
            AppName: AmenoLinkClient.Settings.AppName
        );

        string url = $"{AmenoLinkClient.Settings.OriginUrl}/api/request";
        var responseElement = await AmenoLinkClient.PostJson<JsonElement>(url, requestDto);

        bool success = responseElement.TryGetProperty("success", out var successProperty) && successProperty.GetBoolean();
        if (!success)
        {
            string errorMessage = "Erro desconhecido ao executar ação.";

            if (responseElement.TryGetProperty("error", out var errorProperty) && errorProperty.ValueKind == JsonValueKind.Object)
            {
                if (errorProperty.TryGetProperty("message", out var messageProperty))
                    errorMessage = messageProperty.GetString() ?? errorMessage;
            }
            else if (responseElement.TryGetProperty("errorMessage", out var errorMessageProperty))
                errorMessage = errorMessageProperty.GetString() ?? errorMessage;

            throw new AmenoException(errorMessage);
        }

        if (responseElement.TryGetProperty("result", out var resultElement))
            return JsonSerializer.Deserialize<T>(resultElement.GetRawText(), AmenoLinkClient.JsonOptions)!;

        if (responseElement.TryGetProperty("response", out var responsePropertyLegacy))
            return JsonSerializer.Deserialize<T>(responsePropertyLegacy.GetRawText(), AmenoLinkClient.JsonOptions)!;

        return default!;
    }

    public async Task Queue(object? payload = null)
    {
        var requestDto = new ActionRequest<object?>(
            Id: Ulid.NewUlid().ToString(),
            CreatedAt: DateTime.UtcNow.ToString("o"),
            Route: Name,
            Type: "ActionRequest",
            Payload: payload,
            AppName: AmenoLinkClient.Settings.AppName
        );

        string url = $"{AmenoLinkClient.Settings.OriginUrl}/api/queue";
        await AmenoLinkClient.PostJson<object?>(url, requestDto);
    }
}
