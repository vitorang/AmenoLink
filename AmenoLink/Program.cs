using AmenoLink.Interfaces.ProgramManager;
using AmenoLink.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace AmenoLink;

internal static class Program
{
    public static IServiceProvider ServiceProvider { get; private set; } = null!;

    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseUrls("http://localhost:13545");

        ConfigureServices(builder.Services);

        var app = builder.Build();
        app.UseCors("AllowLocalhostOrigins");
        app.MapApiEndpoints();
        app.MapConfigEndpoints();
        _ = app.RunAsync();

        ServiceProvider = app.Services;

        var processManager = ServiceProvider.GetRequiredService<IProgramManager>();
        processManager.LoadConfigurations();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        Application.Run(mainWindow);
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
        });

        services.AddSingleton<IProgramManager, ProgramManager.ProgramManager>();
        services.AddTransient<MainWindow>();
    }
}