// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Api.Dtos;

public sealed record DailyKpiDto(
    DateTime Date,
    string SiteName,
    int Visits,
    int UniqueVisitors,
    int PageViews,
    int UniquePageViews,
    int Downloads,
    int UniqueDownloads,
    int Events,
    int Searches,
    int AvgVisitDuration,
    int BounceCount,
    decimal BounceRate);

public sealed record DailySummaryDto(
    DateTime? From,
    DateTime? To,
    int Visits,
    int UniqueVisitors,
    int PageViews,
    int Downloads,
    int Events,
    int Searches,
    int BounceCount,
    decimal BounceRate,
    int AvgVisitDuration,
    int DaysWithData);
