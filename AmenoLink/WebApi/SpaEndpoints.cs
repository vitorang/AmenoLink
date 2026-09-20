using AmenoLink.Interfaces.Managers.Spa;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.StaticFiles;

namespace AmenoLink.WebApi;

internal static class SpaEndpoints
{
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    public static IEndpointRouteBuilder MapSpaEndpoints(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/spa/{route}/{*path}", async (string route, string? path, ISpaManager spaManager, HttpContext context) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                if (!context.Request.Path.Value?.EndsWith('/') ?? false)
                    return Results.Redirect($"/spa/{route}/", permanent: true);
            }

            string relativePath = path ?? string.Empty;
            string? resolvedFilePath = spaManager.ResolveFile(route, relativePath, out bool isIndexFallback);

            if (resolvedFilePath is null)
                return Results.NotFound();

            if (isIndexFallback)
            {
                string? html = await spaManager.GetIndexHtml(route, resolvedFilePath);
                if (html is null)
                    return Results.NotFound();

                return Results.Content(html, "text/html", System.Text.Encoding.UTF8);
            }

            if (!ContentTypeProvider.TryGetContentType(resolvedFilePath, out string? contentType))
                contentType = "application/octet-stream";

            return Results.File(resolvedFilePath, contentType);
        });

        return routes;
    }
}
