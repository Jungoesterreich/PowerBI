// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core;
using JoeSync.Core.Contracts;
using JoeSync.Core.Data;
using JoeSync.Core.Data.Repositories;
using JoeSync.Importers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = Host.CreateApplicationBuilder(args);

    builder.Services.AddSerilog(config =>
        config.WriteTo.Console());

    builder.Services.AddJoeSyncCore(builder.Configuration);
    builder.Services.AddJoeSyncImporters(builder.Configuration);

    using var host = builder.Build();

    // Apply pending EF Core migrations on startup
    using (var scope = host.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<JoeSyncDbContext>();
        Log.Information("Applying pending database migrations...");
        await db.Database.MigrateAsync();
        Log.Information("Database migrations applied");
    }

    var command = ParseCommand(args);

    // Apply date range override from CLI args or interactive prompt
    ApplyDateRange(command, builder.Configuration);

    switch (command)
    {
        case ListCommand:
            ListJobs(host.Services);
            break;

        case RunJobCommand runJob:
            var exitCode = await RunJobAsync(
                host.Services, runJob.JobName, CancellationToken.None);
            Environment.ExitCode = exitCode;
            break;

        default:
            var selectedJob = PromptJobSelection(host.Services);
            PromptDateRange(builder.Configuration);
            if (selectedJob is not null)
            {
                Environment.ExitCode = await RunJobAsync(
                    host.Services, selectedJob, CancellationToken.None);
            }
            else
            {
                Environment.ExitCode = await RunAllJobsAsync(
                    host.Services, CancellationToken.None);
            }

            break;
    }
}
catch (Exception ex)
{
    Log.Fatal(ex, "JoeSync Console terminated unexpectedly");
    Environment.ExitCode = 1;
}
finally
{
    Log.CloseAndFlush();
}

return;

static CliCommand ParseCommand(string[] args)
{
    string? from = null;
    string? to = null;
    string? jobName = null;
    var isList = false;

    for (var i = 0; i < args.Length; i++)
    {
        if (args[i] == "--list")
        {
            isList = true;
        }

        if (args[i] == "--job" && i + 1 < args.Length)
        {
            jobName = args[i + 1];
        }

        if (args[i] == "--from" && i + 1 < args.Length)
        {
            from = args[i + 1];
        }

        if (args[i] == "--to" && i + 1 < args.Length)
        {
            to = args[i + 1];
        }
    }

    if (isList)
    {
        return new ListCommand();
    }

    if (jobName is not null)
    {
        return new RunJobCommand(jobName) { From = from, To = to };
    }

    return new UnknownCommand();
}

static void ApplyDateRange(CliCommand command, IConfigurationManager configuration)
{
    if (command is RunJobCommand { From: not null } runJob)
    {
        configuration["Sync:OverrideStartDate"] = runJob.From;

        if (runJob.To is not null)
        {
            configuration["Sync:OverrideEndDate"] = runJob.To;
        }

        Log.Information("Date range override: {From} to {To}",
            runJob.From, runJob.To ?? "yesterday");
    }
}

static void PromptDateRange(IConfigurationManager configuration)
{
    Console.WriteLine();
    Console.Write("Date range (leave empty for delta sync, or enter start date yyyy-MM-dd): ");
    var fromInput = Console.ReadLine()?.Trim();

    if (string.IsNullOrEmpty(fromInput))
    {
        return;
    }

    if (!DateOnly.TryParse(fromInput, out _))
    {
        Log.Warning("Invalid date format, using delta sync");
        return;
    }

    configuration["Sync:OverrideStartDate"] = fromInput;

    Console.Write("End date (leave empty for yesterday): ");
    var toInput = Console.ReadLine()?.Trim();

    if (!string.IsNullOrEmpty(toInput) && DateOnly.TryParse(toInput, out _))
    {
        configuration["Sync:OverrideEndDate"] = toInput;
    }

    Log.Information("Date range override: {From} to {To}",
        fromInput, string.IsNullOrEmpty(toInput) ? "yesterday" : toInput);
}

static void ListJobs(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var jobs = scope.ServiceProvider.GetServices<ISyncJob>().ToList();

    if (jobs.Count == 0)
    {
        Log.Information("No sync jobs registered");
        return;
    }

    Log.Information("Available sync jobs:");
    foreach (var job in jobs)
    {
        Log.Information("  - {JobName}", job.JobName);
    }
}

static async Task<int> RunJobAsync(
    IServiceProvider services,
    string jobName,
    CancellationToken cancellationToken)
{
    using var scope = services.CreateScope();
    var jobs = scope.ServiceProvider.GetServices<ISyncJob>().ToList();

    var job = jobs.FirstOrDefault(j =>
        string.Equals(j.JobName, jobName, StringComparison.OrdinalIgnoreCase));

    if (job is null)
    {
        Log.Error("Job {JobName} not found. Use --list to see available jobs", jobName);
        return 1;
    }

    var syncLogRepo = scope.ServiceProvider
        .GetRequiredService<ISyncLogRepository>();

    var log = await syncLogRepo.LogStartAsync(job.JobName, cancellationToken);

    try
    {
        Log.Information("Executing job {JobName}", job.JobName);

        var result = await job.ExecuteAsync(cancellationToken);

        if (result.Success)
        {
            await syncLogRepo.LogSuccessAsync(
                log, result.RowsAffected, cancellationToken);
            Log.Information(
                "Job {JobName} completed. {RowsAffected} rows",
                job.JobName,
                result.RowsAffected);
            return 0;
        }

        await syncLogRepo.LogFailureAsync(
            log, result.ErrorMessage ?? "Unknown error", cancellationToken);
        Log.Error(
            "Job {JobName} failed: {ErrorMessage}",
            job.JobName,
            result.ErrorMessage);
        return 1;
    }
    catch (Exception ex)
    {
        await syncLogRepo.LogFailureAsync(
            log, ex.Message, cancellationToken);
        Log.Error(ex, "Job {JobName} threw an exception", job.JobName);
        return 1;
    }
}

static string? PromptJobSelection(IServiceProvider services)
{
    using var scope = services.CreateScope();
    var jobs = scope.ServiceProvider.GetServices<ISyncJob>().ToList();

    if (jobs.Count == 0)
    {
        Log.Warning("No sync jobs registered");
        return null;
    }

    Console.WriteLine();
    Console.WriteLine("Available sync jobs:");
    Console.WriteLine("  [0] Run ALL jobs");

    for (var i = 0; i < jobs.Count; i++)
    {
        Console.WriteLine($"  [{i + 1}] {jobs[i].JobName}");
    }

    Console.WriteLine();
    Console.Write("Select job [0-{0}]: ", jobs.Count);

    var input = Console.ReadLine()?.Trim();

    if (!int.TryParse(input, out var selection) || selection < 0 || selection > jobs.Count)
    {
        Log.Error("Invalid selection");
        return null;
    }

    if (selection == 0)
    {
        return null;
    }

    return jobs[selection - 1].JobName;
}

static async Task<int> RunAllJobsAsync(
    IServiceProvider services,
    CancellationToken cancellationToken)
{
    using var scope = services.CreateScope();
    var jobs = scope.ServiceProvider.GetServices<ISyncJob>().ToList();

    Log.Information("Running all {Count} sync jobs", jobs.Count);

    var failed = false;

    foreach (var job in jobs)
    {
        var syncLogRepo = scope.ServiceProvider
            .GetRequiredService<ISyncLogRepository>();

        var log = await syncLogRepo.LogStartAsync(job.JobName, cancellationToken);

        try
        {
            Log.Information("Executing job {JobName}", job.JobName);

            var result = await job.ExecuteAsync(cancellationToken);

            if (result.Success)
            {
                await syncLogRepo.LogSuccessAsync(
                    log, result.RowsAffected, cancellationToken);
                Log.Information(
                    "Job {JobName} completed. {RowsAffected} rows",
                    job.JobName, result.RowsAffected);
            }
            else
            {
                await syncLogRepo.LogFailureAsync(
                    log, result.ErrorMessage ?? "Unknown error", cancellationToken);
                Log.Error(
                    "Job {JobName} failed: {ErrorMessage}",
                    job.JobName, result.ErrorMessage);
                failed = true;
            }
        }
        catch (Exception ex)
        {
            await syncLogRepo.LogFailureAsync(
                log, ex.Message, cancellationToken);
            Log.Error(ex, "Job {JobName} threw an exception", job.JobName);
            failed = true;
        }
    }

    return failed ? 1 : 0;
}

internal abstract record CliCommand;
internal sealed record ListCommand : CliCommand;
internal sealed record RunJobCommand : CliCommand
{
    public RunJobCommand(string JobName)
    {
        this.JobName = JobName;
    }

    public string JobName { get; init; }

    public string? From { get; init; }

    public string? To { get; init; }
}

internal sealed record UnknownCommand : CliCommand;
