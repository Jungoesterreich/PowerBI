// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Api.Services;
using JoeSync.Api.Sync;
using Microsoft.AspNetCore.Mvc;

namespace JoeSync.Api.Endpoints;

public static class SyncEndpoints
{
    public static IEndpointRouteBuilder MapSyncEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sync").WithTags("Sync");

        group.MapGet(
            "/jobs",
            (SyncRunner runner) =>
                Results.Ok(runner.GetRegisteredJobNames().Select(n => new SyncJobDto(n))))
            .WithName("GetSyncJobs")
            .WithSummary("List registered sync jobs")
            .WithDescription("Namen aller im DI-Container registrierten Sync-Jobs.")
            .Produces<IEnumerable<SyncJobDto>>();

        group.MapGet(
            "/status",
            (SyncRunner runner) =>
            {
                var current = runner.Current;
                return Results.Ok(new SyncStatusDto(
                    runner.IsRunning,
                    current?.JobName,
                    current?.SyncLogId,
                    current?.StartedAt,
                    current?.OverrideFrom?.ToString("yyyy-MM-dd"),
                    current?.OverrideTo?.ToString("yyyy-MM-dd")));
            })
            .WithName("GetSyncStatus")
            .WithSummary("Current sync execution status")
            .WithDescription("Ob aktuell ein Sync läuft und – falls ja – welcher Job.")
            .Produces<SyncStatusDto>();

        group.MapGet(
            "/log",
            async (
                ISyncLogService service,
                string? jobName,
                int? limit,
                CancellationToken cancellationToken) =>
            {
                var rows = await service.GetRecentAsync(jobName, limit, cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetSyncLog")
            .WithSummary("Recent sync log entries")
            .WithDescription(
                "Letzte Einträge aus logging.SyncLog. Parameter: jobName – optionaler "
                + "Job-Filter; limit – maximale Zeilenzahl (1–200, Default 20).")
            .Produces<IReadOnlyList<SyncLogDto>>();

        group.MapGet(
            "/log/{id:int}",
            async (
                int id,
                ISyncLogService service,
                CancellationToken cancellationToken) =>
            {
                var row = await service.GetByIdAsync(id, cancellationToken);
                return row is null ? Results.NotFound() : Results.Ok(row);
            })
            .WithName("GetSyncLogEntry")
            .WithSummary("Single sync log entry")
            .WithDescription("Einzelner Sync-Log-Eintrag per ID. 404, wenn nicht vorhanden.")
            .Produces<SyncLogDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapPost(
            "/run/{jobName}",
            async (
                string jobName,
                [FromBody] RunRequestDto? body,
                SyncRunner runner,
                ILogger<SyncRunner> logger,
                CancellationToken cancellationToken) =>
            {
                var (from, to) = ParseDates(body);

                try
                {
                    var handle = await runner.StartJobAsync(jobName, from, to, cancellationToken);
                    return Results.Accepted(
                        $"/api/sync/log/{handle.Items[0].SyncLogId}",
                        new RunResponseDto(
                            [.. handle.Items.Select(i =>
                                new RunResponseItem(i.JobName, i.SyncLogId))]));
                }
                catch (SyncBusyException ex)
                {
                    logger.LogWarning("Sync rejected, already running: {Job}", ex.Current?.JobName);
                    return Results.Conflict(new
                    {
                        message = "Sync already running",
                        runningJob = ex.Current?.JobName,
                        syncLogId = ex.Current?.SyncLogId,
                    });
                }
                catch (SyncJobNotFoundException ex)
                {
                    return Results.NotFound(new { message = ex.Message });
                }
            })
            .WithName("RunSyncJob")
            .WithSummary("Start a single sync job")
            .WithDescription(
                "Startet einen einzelnen Sync-Job asynchron. jobName – Name aus "
                + "/api/sync/jobs. Optionaler Body mit from/to (ISO-Datum) überschreibt "
                + "den Standard-Zeitraum. 409, wenn bereits ein Sync läuft; 404 bei "
                + "unbekanntem Job.")
            .Produces<RunResponseDto>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status409Conflict);

        group.MapPost(
            "/run-all",
            async (
                [FromBody] RunRequestDto? body,
                SyncRunner runner,
                ILogger<SyncRunner> logger,
                CancellationToken cancellationToken) =>
            {
                var (from, to) = ParseDates(body);

                try
                {
                    var handle = await runner.StartAllJobsAsync(from, to, cancellationToken);
                    return Results.Accepted(
                        "/api/sync/status",
                        new RunResponseDto(
                            [.. handle.Items.Select(i =>
                                new RunResponseItem(i.JobName, i.SyncLogId))]));
                }
                catch (SyncBusyException ex)
                {
                    logger.LogWarning("Run-all rejected, already running: {Job}", ex.Current?.JobName);
                    return Results.Conflict(new
                    {
                        message = "Sync already running",
                        runningJob = ex.Current?.JobName,
                        syncLogId = ex.Current?.SyncLogId,
                    });
                }
            })
            .WithName("RunAllSyncJobs")
            .WithSummary("Start all registered sync jobs sequentially")
            .WithDescription(
                "Startet alle registrierten Sync-Jobs nacheinander. Optionaler Body mit "
                + "from/to (ISO-Datum) überschreibt den Standard-Zeitraum. 409, wenn "
                + "bereits ein Sync läuft.")
            .Produces<RunResponseDto>(StatusCodes.Status202Accepted)
            .Produces(StatusCodes.Status409Conflict);

        return app;
    }

    private static (DateOnly? From, DateOnly? To) ParseDates(RunRequestDto? body)
    {
        if (body is null)
        {
            return (null, null);
        }

        DateOnly? from = DateOnly.TryParse(body.From, out var f) ? f : null;
        DateOnly? to = DateOnly.TryParse(body.To, out var t) ? t : null;
        return (from, to);
    }
}
