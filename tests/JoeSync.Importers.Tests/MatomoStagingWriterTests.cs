// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Importers.Matomo;
using JoeSync.Importers.Matomo.Models;

namespace JoeSync.Importers.Tests;

public sealed class MatomoStagingWriterTests
{
    private static MatomoVisit Visit(long idVisit)
    {
        return new MatomoVisit
        {
            IdVisit = idVisit,
            IdSite = 1,
            ServerDate = "2026-01-01",
        };
    }

    [Fact]
    public void DeduplicateVisits_RemovesDuplicateIdVisit_KeepsFirstOccurrence()
    {
        var visits = new List<MatomoVisit>
        {
            Visit(1),
            Visit(2),
            Visit(1),
            Visit(3),
            Visit(2),
        };

        var result = MatomoStagingWriter.DeduplicateVisits(visits);

        Assert.Equal([1L, 2L, 3L], result.Select(v => v.IdVisit));
    }

    [Fact]
    public void DeduplicateVisits_NoDuplicates_ReturnsAllInOrder()
    {
        var visits = new List<MatomoVisit> { Visit(10), Visit(20), Visit(30) };

        var result = MatomoStagingWriter.DeduplicateVisits(visits);

        Assert.Equal([10L, 20L, 30L], result.Select(v => v.IdVisit));
    }

    [Fact]
    public void DeduplicateVisits_EmptyInput_ReturnsEmpty()
    {
        var result = MatomoStagingWriter.DeduplicateVisits([]);

        Assert.Empty(result);
    }
}
