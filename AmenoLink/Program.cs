using AmenoLink.Hubs;
using AmenoLink.Interfaces.Hub;
using AmenoLink.Interfaces.Managers.Cache;
using AmenoLink.Interfaces.Managers.Configuration;
using AmenoLink.Interfaces.Managers.Program;
using AmenoLink.Interfaces.Managers.Topic;
using AmenoLink.Managers.Cache;
using AmenoLink.Managers.Configuration;
using AmenoLink.Managers.Program;
using AmenoLink.Managers.Topic;
using AmenoLink.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace AmenoLink;

internal static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromMilliseconds(500) };
            _ = client.GetAsync("http://localhost:13545/api/config/show-app").GetAwaiter().GetResult();
            return;
        }
        catch
        {
        }

        ApplicationConfiguration.Initialize();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://localhost:13545");
        builder.Services.AddSignalR();

        ConfigureServices(builder.Services);

        var app = builder.Build();
        app.UseCors("AllowLocalhostOrigins");
        app.MapApiEndpoints();
        app.MapConfigEndpoints();
        app.MapHub<AppHub>("/app-hub");

        var staticPath = ResolveStaticPath(builder.Environment);

        if (Directory.Exists(staticPath))
        {
            var fileProvider = new PhysicalFileProvider(staticPath);

            app.Map("/ameno-ui", spa =>
            {
                spa.UseDefaultFiles(new DefaultFilesOptions
                {
                    FileProvider = fileProvider
                });

                spa.UseStaticFiles(new StaticFileOptions
                {
                    FileProvider = fileProvider
                });

                spa.Run(async context =>
                {
                    context.Response.ContentType = "text/html";
                    await context.Response.SendFileAsync(Path.Combine(staticPath, "index.html"));
                });
            });
        }

        _ = app.RunAsync();

        ServiceProvider = app.Services;

        var configManager = ServiceProvider.GetRequiredService<IConfigurationManager>();
        configManager.LoadConfigurations();

        var programManager = ServiceProvider.GetRequiredService<IProgramManager>();
        programManager.LoadConfigurations();

        var cacheManager = ServiceProvider.GetRequiredService<ICacheManager>();
        cacheManager.LoadConfigurations();

        var topicManager = ServiceProvider.GetRequiredService<ITopicManager>();
        topicManager.LoadConfigurations();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        try
        {
            Application.Run(mainWindow);
        }
        finally
        {
            programManager.Dispose();
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy("AllowLocalhostOrigins", policy =>
            {
                policy.SetIsOriginAllowed(origin =>
                {
                    var host = new Uri(origin).Host;
                    return host == "localhost" || host == "127.0.0.1";
                })
                .AllowAnyHeader()
                .AllowAnyMethod();
            });
        });

        services.Configure<Microsoft.AspNetCore.Http.Json.JsonOptions>(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = Shared.JsonDefaults.Options.PropertyNamingPolicy;
            options.SerializerOptions.WriteIndented = Shared.JsonDefaults.Options.WriteIndented;
            options.SerializerOptions.DefaultIgnoreCondition = Shared.JsonDefaults.Options.DefaultIgnoreCondition;
            options.SerializerOptions.Encoder = Shared.JsonDefaults.Options.Encoder;
            options.SerializerOptions.TypeInfoResolver = Shared.JsonDefaults.Options.TypeInfoResolver;
        });

        services.AddSingleton<IConfigurationManager, ConfigurationManager>();
        services.AddSingleton<IHubService, HubService>();
        services.AddSingleton<IProgramManager, ProgramManager>();
        services.AddSingleton<ICacheManager, CacheManager>();
        services.AddSingleton<ITopicManager, TopicManager>();
        services.AddSingleton<MainWindow>();
    }

    private static string ResolveStaticPath(IWebHostEnvironment env)
    {
        var candidates = new[]
        {
            Path.Combine(env.ContentRootPath, "wwwroot", "browser"),
            Path.Combine(env.ContentRootPath, "wwwroot"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot", "browser"),
            Path.Combine(AppContext.BaseDirectory, "wwwroot"),
        };

        foreach (var path in candidates)
        {
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "index.html")))
            {
                return path;
            }
        }

        return env.WebRootPath ?? Path.Combine(AppContext.BaseDirectory, "wwwroot");
    }
}