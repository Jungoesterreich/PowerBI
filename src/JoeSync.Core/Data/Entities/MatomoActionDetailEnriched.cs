// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Core.Data.Entities;

public sealed class MatomoActionDetailEnriched
{
    public int Id { get; init; }

    public int IdSite { get; init; }

    public required string SiteName { get; init; }

    public long IdVisit { get; init; }

    public string? Type { get; set; }

    public string? Url { get; set; }

    public string? PageTitle { get; set; }

    public long? Timestamp { get; set; }

    public int? TimeSpent { get; set; }

    public string? UrlLastPathElement { get; set; }

    public string? UrlParameter { get; set; }

    public string? UrlClean { get; set; }

    public string? UrlPathFull { get; set; }

    public string? UrlPathElementOne { get; set; }

    public string? UrlPathElementTwo { get; set; }

    public string? FileType { get; set; }

    public string? Edition { get; set; }

    public string? SearchPatternIdu { get; set; }

    public string? SearchPatternTo { get; set; }

    public string? Seitenart { get; set; }

    public string? SubRubrik { get; set; }

    public DateTime? ServerDate { get; set; }

    public string? VisitorId { get; set; }

    public int? VisitDuration { get; set; }

    public string? DeviceType { get; set; }

    public string? Browser { get; set; }

    public string? Country { get; set; }

    public string? City { get; set; }
}
