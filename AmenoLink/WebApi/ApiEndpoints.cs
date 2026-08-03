using System.Text.Json;
using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Interfaces.Caching;
using AmenoLink.Interfaces.ProgramManager;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        group.MapGet("/cache", (string groupKey, string key, ICacheManager cacheManager) =>
        {
            try
            {
                var value = cacheManager.Get(groupKey, key);
                return Results.Ok(value);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/cache", ([FromQuery] string groupKey, [FromQuery] string key, [FromBody] JsonElement value, ICacheManager cacheManager) =>
        {
            try
            {
                cacheManager.Set(groupKey, key, value);
                return Results.Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/cache", (string groupKey, string key, ICacheManager cacheManager) =>
        {
            try
            {
                cacheManager.Delete(groupKey, key);
                return Results.Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/cache/all", (string groupKey, ICacheManager cacheManager) =>
        {
            try
            {
                var entries = cacheManager.All(groupKey);
                return Results.Ok(entries);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/cache/all", (string groupKey, ICacheManager cacheManager) =>
        {
            try
            {
                cacheManager.Clear(groupKey);
                return Results.Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return routes;
    }
}
