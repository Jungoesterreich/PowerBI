// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Core.Data.Entities;

public sealed class MatomoDailySummary
{
    public int Id { get; init; }

    public int IdSite { get; init; }

    public required string SiteName { get; init; }

    public DateTime Date { get; init; }

    public int Visits { get; set; }

    public int UniqueVisitors { get; set; }

    public int PageViews { get; set; }

    public int Downloads { get; set; }

    public int Events { get; set; }

    public int Searches { get; set; }

    public int AvgVisitDuration { get; set; }

    public int BounceCount { get; set; }

    public int UniquePageViews { get; set; }

    public int UniqueDownloads { get; set; }
}
