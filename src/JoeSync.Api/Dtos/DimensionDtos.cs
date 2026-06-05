// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Api.Dtos;

public sealed record AusgabeDto(
    string Ausgabenkuerzel,
    string Schuljahr,
    int? Jahr,
    int? Ausgabe,
    int? AusgabeIdu);

public sealed record SiteDto(int IdSite, string SiteName);

public sealed record SearchTermDto(string Term, string Source, int Count);
