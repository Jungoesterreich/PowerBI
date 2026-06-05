// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Api.Services;

namespace JoeSync.Api.Endpoints;

public static class DimensionEndpoints
{
    public static IEndpointRouteBuilder MapDimensionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dim").WithTags("Dimensions");

        group.MapGet(
            "/ausgaben",
            async (
                IDimensionsService service,
                string? schuljahr,
                CancellationToken cancellationToken) =>
            {
                var rows = await service.GetAusgabenAsync(schuljahr, cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetAusgaben")
            .WithSummary("List Ausgaben (editions)")
            .WithDescription(
                "Liefert die bekannten Ausgaben/Editionen. Parameter: schuljahr – "
                + "optionaler Filter auf ein Schuljahr (z. B. 2025/26).")
            .Produces<IReadOnlyList<AusgabeDto>>();

        group.MapGet(
            "/schuljahre",
            async (IDimensionsService service, CancellationToken cancellationToken) =>
            {
                var rows = await service.GetSchuljahreAsync(cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetSchuljahre")
            .WithSummary("Distinct list of Schuljahre")
            .WithDescription("Eindeutige Liste aller Schuljahre, absteigend sortiert.")
            .Produces<IReadOnlyList<string>>();

        group.MapGet(
            "/sites",
            async (IDimensionsService service, CancellationToken cancellationToken) =>
            {
                var rows = await service.GetSitesAsync(cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetSites")
            .WithSummary("List sites that have synced data")
            .WithDescription("Alle Matomo-Sites, für die bereits Daten synchronisiert wurden.")
            .Produces<IReadOnlyList<SiteDto>>();

        group.MapGet(
            "/search-terms",
            async (
                IDimensionsService service,
                int? siteId,
                int? top,
                CancellationToken cancellationToken) =>
            {
                var rows = await service.GetSearchTermsAsync(siteId, top, cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetSearchTerms")
            .WithSummary("Top search patterns from action-detail enrichment")
            .WithDescription(
                "Häufigste Suchmuster aus der Action-Detail-Anreicherung. Parameter: "
                + "siteId – optionale Matomo-Site-ID; top – maximale Zeilenzahl.")
            .Produces<IReadOnlyList<SearchTermDto>>();

        return app;
    }
}
