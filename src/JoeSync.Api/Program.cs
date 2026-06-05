// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using System.Reflection;
using JoeSync.Api.Endpoints;
using JoeSync.Api.Services;
using JoeSync.Api.Sync;
using JoeSync.Core;
using JoeSync.Importers;
using Microsoft.OpenApi;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/joesync-api-.log",
        rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("JoeSync API starting up");

    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddSerilog(config =>
        config
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console()
            .WriteTo.File(
                "logs/joesync-api-.log",
                rollingInterval: RollingInterval.Day));

    builder.Services.AddJoeSyncCore(builder.Configuration);
    builder.Services.AddJoeSyncImporters(builder.Configuration);
    builder.Services.AddSingleton<SyncRunner>();

    builder.Services.AddScoped<IEditionLookupService, EditionLookupService>();
    builder.Services.AddScoped<IPageviewsService, PageviewsService>();
    builder.Services.AddScoped<IDownloadsService, DownloadsService>();
    builder.Services.AddScoped<IMediaAudioService, MediaAudioService>();
    builder.Services.AddScoped<IBrandsService, BrandsService>();
    builder.Services.AddScoped<IDailyKpiService, DailyKpiService>();
    builder.Services.AddScoped<IDimensionsService, DimensionsService>();
    builder.Services.AddScoped<ISyncLogService, SyncLogService>();

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "JoeSync API",
            Version = "v1",
            Description = "Lese-API über die konsolidierten Matomo-KPIs in JOEDB_KPI "
                + "sowie Steuerung der Sync-Jobs. Power BI und das Web-Dashboard "
                + "konsumieren ausschließlich diese Endpunkte.",
            Contact = new OpenApiContact
            {
                Name = "Nehl-IT GmbH",
                Email = "office@nehl-it.com",
            },
        });

        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }
    });

    var corsOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(policy =>
        {
            if (corsOrigins.Length == 0)
            {
                policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
            }
            else
            {
                policy
                    .WithOrigins(corsOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            }
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    // Swagger is exposed in every environment so the API stays self-documenting
    // after the handover to JÖZV (the API is reachable only inside their network).
    app.UseSwagger();
    app.UseSwaggerUI();

    app.UseCors();

    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();

    app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
        .WithTags("System");

    app.MapDailyEndpoints();
    app.MapPageviewsEndpoints();
    app.MapDownloadsEndpoints();
    app.MapMediaAudioEndpoints();
    app.MapDimensionEndpoints();
    app.MapBrandEndpoints();
    app.MapSyncEndpoints();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "JoeSync API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
