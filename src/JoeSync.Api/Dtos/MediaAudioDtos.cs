// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Api.Dtos;

public sealed record MediaAudioRowDto(
    string? Label,
    string? Url,
    string? UrlLastPathElement,
    string? Edition,
    string? Schuljahr,
    int Plays,
    int UniquePlays,
    int Finishes,
    double FinishRate,
    int AvgTimeWatched);
