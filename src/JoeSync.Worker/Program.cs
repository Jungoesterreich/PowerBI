// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core;
using JoeSync.Core.Data;
using JoeSync.Importers;
using JoeSync.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/joesync-.log",
        rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

try
{
    Log.Information("JoeSync Worker starting up");

    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddWindowsService(options =>
        options.ServiceName = "JoeSyncWorker");

    builder.Services.AddSerilog(config =>
        config
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console()
            .WriteTo.File(
                "logs/joesync-.log",
                rollingInterval: RollingInterval.Day));

    builder.Services.AddJoeSyncCore(builder.Configuration);
    builder.Services.AddJoeSyncImporters(builder.Configuration);
    builder.Services.AddHostedService<SyncOrchestrator>();

    var host = builder.Build();

    // Apply pending EF Core migrations on startup
    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<JoeSyncDbContext>();
        Log.Information("Applying pending database migrations...");
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied");
    }

    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "JoeSync Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
