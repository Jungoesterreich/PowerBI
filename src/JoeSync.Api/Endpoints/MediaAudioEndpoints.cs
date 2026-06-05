// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Dtos;
using JoeSync.Api.Services;

namespace JoeSync.Api.Endpoints;

public static class MediaAudioEndpoints
{
    public static IEndpointRouteBuilder MapMediaAudioEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/media-audio").WithTags("MediaAudio");

        group.MapGet(
            "/",
            async (
                IMediaAudioService service,
                int? siteId,
                DateTime? from,
                DateTime? to,
                string? schuljahr,
                string? ausgabe,
                int? top,
                CancellationToken cancellationToken) =>
            {
                var rows = await service.GetTopAsync(
                    siteId, from, to, schuljahr, ausgabe, top, cancellationToken);
                return Results.Ok(rows);
            })
            .WithName("GetTopMediaAudio")
            .WithSummary("Top audio / video resources by play count")
            .WithDescription(
                "Meistgespielte Audio-/Video-Ressourcen inkl. Finish-Rate. Parameter: "
                + "siteId – optionale Matomo-Site-ID; from/to – inklusive Datumsgrenzen "
                + "(ISO 8601); schuljahr/ausgabe – Editions-Filter (ausgabe hat Vorrang); "
                + "top – maximale Zeilenzahl (1–5000, Default 100).")
            .Produces<IReadOnlyList<MediaAudioRowDto>>();

        return app;
    }
}
