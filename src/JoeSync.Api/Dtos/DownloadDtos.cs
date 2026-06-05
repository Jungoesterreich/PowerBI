// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Api.Dtos;

public sealed record DownloadRowDto(
    string? Url,
    string? UrlLastPathElement,
    string? FileType,
    string? Edition,
    string? Schuljahr,
    string? SearchPatternIdu,
    string? SearchPatternTo,
    int Downloads,
    int UniqueDownloads);
