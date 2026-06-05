// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Api.Dtos;

public sealed record BrandDto(
    string Key,
    string Label,
    string UrlContains,
    string? UrlExcludes);

public sealed record BrandSummaryDto(
    string Key,
    string Label,
    int Pageviews,
    int UniquePageVisitors,
    int Downloads,
    int UniqueDownloadVisitors,
    int Pages,
    int Files);

public sealed record BrandTimelinePointDto(
    DateTime Date,
    int Pageviews,
    int Downloads);
