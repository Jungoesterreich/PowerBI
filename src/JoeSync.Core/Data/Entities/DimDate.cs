// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Core.Data.Entities;

public sealed class DimDate
{
    public DateTime Date { get; init; }

    public int DayNum { get; init; }

    public int MonthNum { get; init; }

    public required string MonthName { get; init; }

    public required string Month { get; init; }

    public int MonthID { get; init; }

    public int DayID { get; init; }

    public int Year { get; init; }
}
