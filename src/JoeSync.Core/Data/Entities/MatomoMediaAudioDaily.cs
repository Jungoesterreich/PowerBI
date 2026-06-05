// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Core.Data.Entities;

public sealed class MatomoMediaAudioDaily
{
    public int Id { get; init; }

    public int IdSite { get; init; }

    public required string SiteName { get; init; }

    public DateTime Date { get; init; }

    public string? Label { get; set; }

    public int Plays { get; set; }

    public int UniqueVisitorsPlays { get; set; }

    public int Finishes { get; set; }

    public string? Url { get; set; }

    public string? UrlLastPathElement { get; set; }

    public string? Edition { get; set; }

    public double PlayRate { get; set; }

    public double FinishRate { get; set; }

    public int AvgTimeWatched { get; set; }

    public double AvgCompletion { get; set; }
}
