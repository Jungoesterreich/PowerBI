// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Api.Dtos;

public sealed record PageviewRowDto(
    string? Url,
    string? PageTitle,
    string? UrlPathFull,
    string? UrlPathElementOne,
    string? UrlPathElementTwo,
    string? Edition,
    string? Schuljahr,
    string? SearchPatternIdu,
    string? SearchPatternTo,
    string? Seitenart,
    string? SubRubrik,
    int Hits,
    int UniqueVisitors);

public sealed record PageviewTimelinePointDto(
    DateTime Date,
    int Hits,
    int UniqueVisitors);

public sealed record EditionAggregateDto(
    string Edition,
    string? Schuljahr,
    int? Jahr,
    int? Ausgabe,
    int Hits,
    int UniqueVisitors);
