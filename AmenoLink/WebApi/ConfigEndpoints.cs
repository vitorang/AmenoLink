using AmenoLink.Configurations;
using AmenoLink.Interfaces.Caching;
using AmenoLink.Interfaces.ProgramManager;
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
