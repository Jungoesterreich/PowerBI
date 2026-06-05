// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Api.Services;

namespace JoeSync.Api.Endpoints;

public static class DailyEndpoints
{
    public static IEndpointRouteBuilder MapDailyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/daily").WithTags("Daily");

        group.MapGet(
            "/",
            async (
                IDailyKpiService service,
                int? siteId,
                DateTime? from,
                DateTime? to,
                CancellationToken cancellationToken) =>
            {
                var rows = await service.GetTimeseriesAsync(siteId, from, to, cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetDailyTimeseries")
            .WithSummary("Daily KPI time series")
            .WithDescription(
                "Tageswerte (Visits, Pageviews, Downloads, Bounce-Rate u. a.) je Tag, "
                + "aufsteigend nach Datum. Parameter: siteId – optionale Matomo-Site-ID "
                + "(ohne Angabe alle Sites); from/to – inklusive Datumsgrenzen (ISO 8601).")
            .Produces<IReadOnlyList<DailyKpiDto>>();

        group.MapGet(
            "/summary",
            async (
                IDailyKpiService service,
                int? siteId,
                DateTime? from,
                DateTime? to,
                CancellationToken cancellationToken) =>
            {
                var summary = await service.GetSummaryAsync(siteId, from, to, cancellationToken);
                return Results.Ok(summary);
            })
            .WithName("GetDailySummary")
            .WithSummary("Aggregated KPI totals across the selected range")
            .WithDescription(
                "Summiert die Tageswerte über den gewählten Zeitraum zu einer Kennzahl. "
                + "Parameter: siteId – optionale Matomo-Site-ID; from/to – inklusive "
                + "Datumsgrenzen (ISO 8601).")
            .Produces<DailySummaryDto>();

        return app;
    }
}
