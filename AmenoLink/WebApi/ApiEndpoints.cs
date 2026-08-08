using System.Text.Json;
using AmenoLink.Configurations;
using AmenoLink.Dtos;
using AmenoLink.Interfaces.Caching;
using AmenoLink.Interfaces.ProgramManager;
using AmenoLink.Interfaces.TopicManager;
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

        group.MapGet("/cache", (string groupName, string key, ICacheManager cacheManager) =>
        {
            try
            {
                var value = cacheManager.Get(groupName, key);
                return Results.Ok(value);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/cache", ([FromQuery] string groupName, [FromQuery] string key, [FromBody] JsonElement value, ICacheManager cacheManager) =>
        {
            try
            {
                cacheManager.Set(groupName, key, value);
                return Results.Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/cache", (string groupName, string key, ICacheManager cacheManager) =>
        {
            try
            {
                cacheManager.Delete(groupName, key);
                return Results.Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapGet("/cache/all", (string groupName, ICacheManager cacheManager) =>
        {
            try
            {
                var entries = cacheManager.All(groupName);
                return Results.Ok(entries);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapDelete("/cache/all", (string groupName, ICacheManager cacheManager) =>
        {
            try
            {
                cacheManager.Clear(groupName);
                return Results.Ok();
            }
            catch (KeyNotFoundException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        group.MapPost("/topic/publish", async (TopicMessage message, ITopicManager topicManager) =>
        {
            try
            {
                await topicManager.Publish(message.Topic, message);
                return Results.Ok();
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(ex.Message);
            }
        });

        return routes;
    }
}
