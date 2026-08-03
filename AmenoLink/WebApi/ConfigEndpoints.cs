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
            var configs = ConfigPathProvider.LoadProgramConfigs();
            return Results.Ok(configs);
        });

        group.MapPost("/programs", (ProgramConfig[] configs, IProgramManager programManager) =>
        {
            ConfigPathProvider.SaveProgramConfigs(configs);
            programManager.LoadConfigurations();
            return Results.Ok();
        });

        group.MapGet("/cache", () =>
        {
            var configs = ConfigPathProvider.LoadCacheConfigs();
            return Results.Ok(configs);
        });

        group.MapPost("/cache", (CacheConfig[] configs, ICacheManager cacheManager) =>
        {
            ConfigPathProvider.SaveCacheConfigs(configs);
            cacheManager.LoadConfigurations();
            return Results.Ok();
        });

        group.MapGet("/select-executable", () =>
        {
            string? selectedFile = null;

            var thread = new Thread(() =>
            {
                using var openFileDialog = new OpenFileDialog
                {
                    Filter = "Executáveis e Scripts (*.exe;*.py)|*.exe;*.py|Todos os Arquivos (*.*)|*.*",
                    Title = "Selecionar Executável ou Script"
                };

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
