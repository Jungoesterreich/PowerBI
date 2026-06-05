// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace JoeSync.Core.Data;

public sealed class JoeSyncDbContext : DbContext
{
    public JoeSyncDbContext(DbContextOptions<JoeSyncDbContext> options) : base(options)
    {
    }

    public DbSet<SyncLog> SyncLogs => Set<SyncLog>();

    public DbSet<MatomoDailySummary> MatomoDailySummaries => Set<MatomoDailySummary>();

    public DbSet<MatomoMediaAudioDaily> MatomoMediaAudioDailies => Set<MatomoMediaAudioDaily>();

    public DbSet<MatomoActionDetailEnriched> MatomoActionDetailsEnriched => Set<MatomoActionDetailEnriched>();

    public DbSet<DimDate> DimDates => Set<DimDate>();

    public DbSet<DimAusgabe> DimAusgaben => Set<DimAusgabe>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(JoeSyncDbContext).Assembly);
    }
}
