using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Interfaces.Caching;
using AmenoLink.Interfaces.ProgramManager;
using AmenoLink.Interfaces.TopicManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AmenoLink.WebApi;

internal static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/config");

        group.MapGet("/programs", () =>
        {
            var configs = ConfigPathProvider.Program.LoadConfigs();
            return Results.Ok(configs);
        });

        group.MapPost("/programs", (ProgramConfig[] configs, IProgramManager programManager) =>
        {
            ConfigPathProvider.Program.SaveConfigs(configs);
            programManager.LoadConfigurations();
            return Results.Ok();
        });

        group.MapGet("/cache", () =>
        {
            var configs = ConfigPathProvider.Cache.LoadConfigs();
            return Results.Ok(configs);
        });

        group.MapPost("/cache", (CacheConfig[] configs, ICacheManager cacheManager) =>
        {
            ConfigPathProvider.Cache.SaveConfigs(configs);
            cacheManager.LoadConfigurations();
            return Results.Ok();
        });

        group.MapGet("/topics", () =>
        {
            var configs = ConfigPathProvider.Topic.LoadConfigs();
            return Results.Ok(configs);
        });

        group.MapPost("/topics", (TopicConfig[] configs, ITopicManager topicManager) =>
        {
            ConfigPathProvider.Topic.SaveConfigs(configs);
            topicManager.LoadConfigurations();
            return Results.Ok();
        });

        group.MapGet("/topic/subscribers", (string topicName, ITopicManager topicManager) =>
        {
            var subscribers = topicManager.ListSubscribers(topicName)
                .Select(client => new SubscribedClient(client.ConnectionId, client.AppName))
                .ToArray();
            return Results.Ok(subscribers);
        });

        group.MapGet("/select-executable", (string? currentPath) =>
        {
            string? selectedFile = null;

            var thread = new Thread(() =>
            {
                using var openFileDialog = new OpenFileDialog
                {
                    Filter = "Executáveis e Scripts (*.exe;*.py)|*.exe;*.py|Todos os Arquivos (*.*)|*.*",
                    Title = "Selecionar Executável ou Script"
                };

                if (!string.IsNullOrWhiteSpace(currentPath))
                {
                    string? directory = Path.GetDirectoryName(currentPath);
                    if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
                        openFileDialog.InitialDirectory = directory;
                    else if (Directory.Exists(currentPath))
                        openFileDialog.InitialDirectory = currentPath;
                }

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                    selectedFile = openFileDialog.FileName?.Replace('\\', '/');
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            return Results.Ok(selectedFile);
        });

        return routes;
    }
}
