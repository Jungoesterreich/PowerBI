// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Api.Services;

namespace JoeSync.Api.Endpoints;

public static class PageviewsEndpoints
{
    // Shared description of the analytics filter parameters used by every
    // pageviews endpoint, kept in one place to avoid drift.
    private const string FilterDocs =
        "Parameter: siteId – optionale Matomo-Site-ID; from/to – inklusive "
        + "Datumsgrenzen (ISO 8601); schuljahr – Filter auf ein Schuljahr "
        + "(z. B. 2025/26); ausgabe – konkrete Ausgabe/Edition, hat Vorrang vor "
        + "schuljahr; searchTerm – Freitextsuche über Titel/URL/Suchmuster; "
        + "searchPattern – exakter Treffer auf ein Suchmuster (IDU/TO).";

    public static IEndpointRouteBuilder MapPageviewsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/pageviews").WithTags("Pageviews");

        group.MapGet(
            "/",
            async (
                IPageviewsService service,
                int? siteId,
                DateTime? from,
                DateTime? to,
                string? schuljahr,
                string? ausgabe,
                string? searchTerm,
                string? searchPattern,
                int? top,
                CancellationToken cancellationToken) =>
            {
                var rows = await service.GetTopAsync(
                    siteId, from, to, schuljahr, ausgabe, searchTerm, searchPattern, top, cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetTopPageviews")
            .WithSummary("Top pages aggregated by URL")
            .WithDescription(
                "Meistaufgerufene Seiten, gruppiert nach URL. " + FilterDocs
                + " top – maximale Zeilenzahl (1–5000, Default 100).")
            .Produces<IReadOnlyList<PageviewRowDto>>();

        group.MapGet(
            "/timeline",
            async (
                IPageviewsService service,
                int? siteId,
                DateTime? from,
                DateTime? to,
                string? schuljahr,
                string? ausgabe,
                string? searchTerm,
                string? searchPattern,
                CancellationToken cancellationToken) =>
            {
                var rows = await service.GetTimelineAsync(
                    siteId, from, to, schuljahr, ausgabe, searchTerm, searchPattern, cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetPageviewsTimeline")
            .WithSummary("Pageview hits per day")
            .WithDescription("Seitenaufrufe je Tag inkl. Unique-Visitor-Zahl. " + FilterDocs)
            .Produces<IReadOnlyList<PageviewTimelinePointDto>>();

        group.MapGet(
            "/by-edition",
            async (
                IPageviewsService service,
                int? siteId,
                DateTime? from,
                DateTime? to,
                string? schuljahr,
                string? searchTerm,
                string? searchPattern,
                CancellationToken cancellationToken) =>
            {
                var rows = await service.GetByEditionAsync(
                    siteId, from, to, schuljahr, searchTerm, searchPattern, cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetPageviewsByEdition")
            .WithSummary("Pageview hits grouped by edition / Ausgabe")
            .WithDescription("Seitenaufrufe gruppiert nach Edition/Ausgabe. " + FilterDocs)
            .Produces<IReadOnlyList<EditionAggregateDto>>();

        return app;
    }
}
