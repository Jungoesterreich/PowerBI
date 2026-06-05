// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Worker;

namespace JoeSync.Worker.Tests;

public sealed class SyncOrchestratorTests
{
    [Fact]
    public void CalculateDelay_BeforeTrigger_WaitsUntilSameDay()
    {
        var now = new DateTime(2026, 6, 4, 8, 0, 0);

        var delay = SyncOrchestrator.CalculateDelay(new TimeOnly(21, 0), now);

        Assert.Equal(TimeSpan.FromHours(13), delay);
    }

    [Fact]
    public void CalculateDelay_AfterTrigger_RollsToNextDay()
    {
        var now = new DateTime(2026, 6, 4, 22, 0, 0);

        var delay = SyncOrchestrator.CalculateDelay(new TimeOnly(21, 0), now);

        // Next trigger is 21:00 the following day → 23 hours away.
        Assert.Equal(TimeSpan.FromHours(23), delay);
    }

    [Fact]
    public void CalculateDelay_ExactlyAtTrigger_RollsToNextDay()
    {
        var now = new DateTime(2026, 6, 4, 21, 0, 0);

        var delay = SyncOrchestrator.CalculateDelay(new TimeOnly(21, 0), now);

        Assert.Equal(TimeSpan.FromHours(24), delay);
    }

    [Fact]
    public void CalculateDelay_IsAlwaysPositive()
    {
        var now = new DateTime(2026, 6, 4, 20, 59, 59);

        var delay = SyncOrchestrator.CalculateDelay(new TimeOnly(21, 0), now);

        Assert.True(delay > TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromSeconds(1), delay);
    }
}
