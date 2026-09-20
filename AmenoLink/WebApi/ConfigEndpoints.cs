using AmenoLink.Dtos;
using AmenoLink.Dtos.Configuration;
using AmenoLink.Interfaces.Managers.Cache;
using AmenoLink.Interfaces.Managers.Configuration;
using AmenoLink.Interfaces.Managers.Program;
using AmenoLink.Interfaces.Managers.Spa;
using AmenoLink.Interfaces.Managers.Topic;
using AmenoLink.Managers.Configuration;
using AmenoLink.Managers.Program;
using AmenoLink.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AmenoLink.WebApi;

internal static class ConfigEndpoints
{
    public static IEndpointRouteBuilder MapConfigEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/config");

        #region General

        group.MapGet("/show-app", (MainWindow mainWindow) =>
        {
            mainWindow.Invoke(mainWindow.RestoreFromTray);
            return Results.Ok();
        });

        group.MapPost("/open-url", (OpenUrlRequest request) =>
        {
            if (string.IsNullOrWhiteSpace(request?.Url))
                return Results.BadRequest("URL é obrigatória");

            DialogUtils.OpenInBrowser(request.Url);
            return Results.Ok();
        });

        group.MapGet("/general", (IConfigurationManager configurationManager) =>
        {
            return Results.Ok(configurationManager.General);
        });

        group.MapPost("/general", (GeneralConfig config, IConfigurationManager configurationManager) =>
        {
            configurationManager.SaveGeneralConfig(config);
            configurationManager.LoadConfigurations();
            return Results.Ok();
        });

        group.MapGet("/general/packages/manifest", (string? type, string? currentPath, IProjectManager projectManager) =>
        {
            if (string.IsNullOrWhiteSpace(type))
                return Results.Ok((string?)null);

            string? filter = projectManager.GetManifestFilter(type);
            if (filter is null)
                return Results.Ok((string?)null);

            string title = "Selecionar Manifesto";
            string? selectedFile = DialogUtils.ShowOpenFileDialog(filter, title, currentPath);
            return Results.Ok(selectedFile);
        });

        group.MapGet("/general/packages/version", (string? manifestPath, string? type, IProjectManager projectManager) =>
        {
            if (string.IsNullOrWhiteSpace(manifestPath) || string.IsNullOrWhiteSpace(type))
                return Results.Ok(new PackageVersion(ManifestPath: manifestPath ?? string.Empty, ErrorReason: "Parâmetros inválidos"));

            var result = projectManager.GetPackageVersion(manifestPath, type);
            return Results.Ok(result);
        });

        group.MapGet("/general/packages/instructions", (IProjectManager projectManager) =>
        {
            var instructions = projectManager.GetInstallInstructions();
            return Results.Ok(instructions);
        });

        #endregion

        #region Programs

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

        group.MapGet("/programs/select-executable", (string? currentPath) =>
        {
            string extensionsPattern = string.Join(";", Constants.SupportedExtensions.Select(ext => $"*{ext}"));
            string extensionsLabels = string.Join(", ", Constants.SupportedExtensions.Select(ext => ext.TrimStart('.').ToUpperInvariant()));
            string filter = $"Programas e Scripts ({extensionsLabels})|{extensionsPattern}";
            string title = "Selecionar Executável ou Script";

            string? selectedFile = DialogUtils.ShowOpenFileDialog(filter, title, currentPath);
            return Results.Ok(selectedFile);
        });

        #endregion

        #region Cache

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

        group.MapGet("/cache/subscribers", (string groupName, ICacheManager cacheManager) =>
        {
            var subscribers = cacheManager.ListSubscribers(groupName)
                .Select(client => new SubscribedClient(client.ConnectionId, client.AppName))
                .ToArray();
            return Results.Ok(subscribers);
        });

        #endregion

        #region Topics

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

        group.MapGet("/topic/recent", (string topicName, ITopicManager topicManager) =>
        {
            var recentMessages = topicManager.GetRecentMessages(topicName);
            return Results.Ok(recentMessages);
        });

        #endregion

        #region SPA

        group.MapGet("/spa", (ISpaManager spaManager) =>
        {
            var configs = ConfigPathProvider.Spa.LoadConfigs();
            return Results.Ok(configs);
        });

        group.MapPost("/spa", (SpaConfig[] configs, ISpaManager spaManager) =>
        {
            ConfigPathProvider.Spa.SaveConfigs(configs);
            spaManager.LoadConfigurations();
            return Results.Ok();
        });

        group.MapGet("/spa/select-folder", (string? currentPath) =>
        {
            string title = "Selecionar Diretório do SPA";
            string? selectedFolder = DialogUtils.ShowFolderBrowserDialog(title, currentPath);
            return Results.Ok(selectedFolder);
        });

        #endregion

        return routes;
    }
}
