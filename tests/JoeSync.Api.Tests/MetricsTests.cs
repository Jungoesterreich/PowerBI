// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Api.Services;

namespace JoeSync.Api.Tests;

public sealed class MetricsTests
{
    [Fact]
    public void BounceRate_ZeroVisits_ReturnsZero()
    {
        Assert.Equal(0m, Metrics.BounceRate(5, 0));
    }

    [Theory]
    [InlineData(50, 100, 50.00)]
    [InlineData(0, 100, 0.00)]
    [InlineData(1, 3, 33.33)]
    [InlineData(2, 3, 66.67)]
    [InlineData(100, 100, 100.00)]
    public void BounceRate_RoundsToTwoDecimals(int bounceCount, int visits, decimal expected)
    {
        Assert.Equal(expected, Metrics.BounceRate(bounceCount, visits));
    }

    [Fact]
    public void FinishRate_ZeroPlays_ReturnsZero()
    {
        Assert.Equal(0d, Metrics.FinishRate(3, 0));
    }

    [Theory]
    [InlineData(1, 2, 0.5)]
    [InlineData(3, 4, 0.75)]
    [InlineData(1, 3, 0.3333)]
    [InlineData(2, 3, 0.6667)]
    public void FinishRate_RoundsToFourDecimals(int finishes, int plays, double expected)
    {
        Assert.Equal(expected, Metrics.FinishRate(finishes, plays));
    }
}
