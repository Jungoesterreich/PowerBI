// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Api.Services;

namespace JoeSync.Api.Endpoints;

public static class DownloadsEndpoints
{
    public static IEndpointRouteBuilder MapDownloadsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/downloads").WithTags("Downloads");

        group.MapGet(
            "/",
            async (
                IDownloadsService service,
                int? siteId,
                DateTime? from,
                DateTime? to,
                string? schuljahr,
                string? ausgabe,
                string? fileType,
                string? searchTerm,
                string? searchPattern,
                int? top,
                CancellationToken cancellationToken) =>
            {
                var rows = await service.GetTopAsync(
                    siteId, from, to, schuljahr, ausgabe, fileType, searchTerm, searchPattern, top, cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetTopDownloads")
            .WithSummary("Top downloads aggregated by file URL")
            .WithDescription(
                "Meistgeladene Dateien, gruppiert nach Datei-URL. Parameter: siteId – "
                + "optionale Matomo-Site-ID; from/to – inklusive Datumsgrenzen (ISO 8601); "
                + "schuljahr/ausgabe – Editions-Filter (ausgabe hat Vorrang); fileType – "
                + "Dateityp (z. B. pdf); searchTerm – Freitextsuche; searchPattern – exakter "
                + "Suchmuster-Treffer; top – maximale Zeilenzahl (1–5000, Default 100).")
            .Produces<IReadOnlyList<DownloadRowDto>>();

        return app;
    }
}
