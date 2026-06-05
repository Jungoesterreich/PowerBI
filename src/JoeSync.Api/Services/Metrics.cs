// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

namespace JoeSync.Api.Services;

/// <summary>
/// Pure KPI calculations shared by the analytics services. Kept free of any
/// data-access dependency so the rounding / divide-by-zero rules can be unit
/// tested in isolation.
/// </summary>
public static class Metrics
{
    /// <summary>
    /// Bounce rate as a percentage (0–100), rounded to two decimals.
    /// Returns 0 when there are no visits.
    /// </summary>
    public static decimal BounceRate(int bounceCount, int visits)
    {
        if (visits == 0)
        {
            return 0m;
        }

        return Math.Round((decimal)bounceCount * 100m / visits, 2);
    }

    /// <summary>
    /// Finish rate as a ratio (0–1), rounded to four decimals.
    /// Returns 0 when there are no plays.
    /// </summary>
    public static double FinishRate(int finishes, int plays)
    {
        if (plays == 0)
        {
            return 0d;
        }

        return Math.Round((double)finishes / plays, 4);
    }
}
