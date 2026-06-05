// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Brands;
using JoeSync.Api.Dtos;
using JoeSync.Api.Services;

namespace JoeSync.Api.Endpoints;

public static class BrandEndpoints
{
    public static IEndpointRouteBuilder MapBrandEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/brands").WithTags("Brands");

        group.MapGet(
            "/",
            () => Results.Ok(JoeBrands.All.Select(b =>
                new BrandDto(b.Key, b.Label, b.UrlContains, b.UrlExcludes))))
            .WithName("GetBrands")
            .WithSummary("List configured Jungösterreich heft brands")
            .WithDescription("Liefert die konfigurierten Heft-Brands inkl. URL-Matching-Regeln.")
            .Produces<IEnumerable<BrandDto>>();

        group.MapGet(
            "/{key}/summary",
            async (
                string key,
                IBrandsService service,
                int? siteId,
                DateTime? from,
                DateTime? to,
                string? schuljahr,
                string? ausgabe,
                CancellationToken cancellationToken) =>
            {
                var summary = await service.GetSummaryAsync(
                    key, siteId, from, to, schuljahr, ausgabe, cancellationToken);
                return summary is null ? Results.NotFound() : Results.Ok(summary);
            })
            .WithName("GetBrandSummary")
            .WithSummary("Aggregated KPIs for one brand")
            .WithDescription(
                "Aggregierte Kennzahlen für eine Brand. key – Brand-Schlüssel aus "
                + "/api/brands; übrige Parameter siehe /api/daily. 404, wenn der "
                + "Brand-Schlüssel unbekannt ist.")
            .Produces<BrandSummaryDto>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet(
            "/{key}/pageviews",
            async (
                string key,
                IBrandsService service,
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
                var rows = await service.GetPageviewsAsync(
                    key, siteId, from, to, schuljahr, ausgabe, searchTerm, searchPattern, top, cancellationToken);
                return rows is null ? Results.NotFound() : Results.Ok(rows);
            })
            .WithName("GetBrandPageviews")
            .WithSummary("Top pageviews for one brand")
            .WithDescription(
                "Top-Seitenaufrufe für eine Brand. key – Brand-Schlüssel aus /api/brands. "
                + "404, wenn der Brand-Schlüssel unbekannt ist.")
            .Produces<IReadOnlyList<PageviewRowDto>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet(
            "/{key}/downloads",
            async (
                string key,
                IBrandsService service,
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
                var rows = await service.GetDownloadsAsync(
                    key, siteId, from, to, schuljahr, ausgabe, fileType, searchTerm, searchPattern, top, cancellationToken);
                return rows is null ? Results.NotFound() : Results.Ok(rows);
            })
            .WithName("GetBrandDownloads")
            .WithSummary("Top downloads for one brand")
            .WithDescription(
                "Top-Downloads für eine Brand. key – Brand-Schlüssel aus /api/brands. "
                + "404, wenn der Brand-Schlüssel unbekannt ist.")
            .Produces<IReadOnlyList<DownloadRowDto>>()
            .Produces(StatusCodes.Status404NotFound);

        group.MapGet(
            "/{key}/timeline",
            async (
                string key,
                IBrandsService service,
                int? siteId,
                DateTime? from,
                DateTime? to,
                string? schuljahr,
                string? ausgabe,
                CancellationToken cancellationToken) =>
            {
                var rows = await service.GetTimelineAsync(
                    key, siteId, from, to, schuljahr, ausgabe, cancellationToken);
                return rows is null ? Results.NotFound() : Results.Ok(rows);
            })
            .WithName("GetBrandTimeline")
            .WithSummary("Daily timeline of pageviews + downloads for one brand")
            .WithDescription(
                "Tagesverlauf von Seitenaufrufen und Downloads für eine Brand. key – "
                + "Brand-Schlüssel aus /api/brands. 404, wenn der Brand-Schlüssel "
                + "unbekannt ist.")
            .Produces<IReadOnlyList<BrandTimelinePointDto>>()
            .Produces(StatusCodes.Status404NotFound);

        return app;
    }
}
