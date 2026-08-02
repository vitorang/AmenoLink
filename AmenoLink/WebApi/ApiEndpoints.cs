using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Interfaces.ProgramManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace AmenoLink.WebApi;

internal static class ApiEndpoints
{
    public static IEndpointRouteBuilder MapApiEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api");

        group.MapGet("/", () => "AmenoLink");

        group.MapPost("/request", async (ActionRequest request, IProgramManager processManager) =>
        {
            var response = await processManager.Execute(request);
            return Results.Ok(response);
        });

        group.MapPost("/queue", (ActionRequest request, IProgramManager processManager) =>
        {
            _ = Task.Run(() => processManager.Execute(request));
            return Results.Ok();
        });

        return routes;
    }
}
