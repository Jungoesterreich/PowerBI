// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Core.Contracts;
using JoeSync.Core.Data.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JoeSync.Importers.Matomo;

public sealed class MatomoVisitsSummaryImporter : ISyncJob
{
    private readonly MatomoApiClient _apiClient;
    private readonly MatomoVisitsSummaryStagingWriter _stagingWriter;
    private readonly ISyncLogRepository _syncLogRepo;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MatomoVisitsSummaryImporter> _logger;

    public MatomoVisitsSummaryImporter(
        MatomoApiClient apiClient,
        MatomoVisitsSummaryStagingWriter stagingWriter,
        ISyncLogRepository syncLogRepo,
        IConfiguration configuration,
        ILogger<MatomoVisitsSummaryImporter> logger)
    {
        _apiClient = apiClient;
        _stagingWriter = stagingWriter;
        _syncLogRepo = syncLogRepo;
        _configuration = configuration;
        _logger = logger;
    }

    public string JobName => "MatomoVisitsSummary";

    public async Task<SyncResult> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var token = _configuration["Matomo:Token"]
            ?? throw new InvalidOperationException(
                "Matomo:Token is not configured.");

        var siteIds = _configuration
            .GetSection("Matomo:SiteIds")
            .Get<List<MatomoSiteConfig>>() ?? [];

        if (siteIds.Count == 0)
        {
            return SyncResult.Fail("No Matomo site IDs configured.");
        }

        var overrideStart = _configuration["Sync:OverrideStartDate"];
        var overrideEnd = _configuration["Sync:OverrideEndDate"];

        DateOnly startDate;
        DateOnly endDate;

        if (!string.IsNullOrEmpty(overrideStart)
            && DateOnly.TryParse(overrideStart, out var parsedStart))
        {
            startDate = parsedStart;
            endDate = !string.IsNullOrEmpty(overrideEnd)
                && DateOnly.TryParse(overrideEnd, out var parsedEnd)
                ? parsedEnd
                : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        }
        else
        {
            var lastRun = await _syncLogRepo.GetLastSuccessfulRunAsync(
                JobName, cancellationToken);

            startDate = lastRun.HasValue
                ? DateOnly.FromDateTime(lastRun.Value)
                : DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));

            endDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-1));
        }

        _logger.LogInformation(
            "MatomoVisitsSummary sync from {StartDate} to {EndDate} for {SiteCount} sites",
            startDate, endDate, siteIds.Count);

        var totalRows = 0;

        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            foreach (var site in siteIds)
            {
                var summary = await _apiClient.GetVisitsSummaryAsync(
                    site.Id, date, token, cancellationToken);

                if (summary is null)
                {
                    _logger.LogWarning(
                        "No visits summary for site {IdSite} on {Date}",
                        site.Id, date);
                    continue;
                }

                await _stagingWriter.WriteSummaryAsync(
                    site.Id, date, summary, cancellationToken);

                totalRows++;
            }
        }

        _logger.LogInformation(
            "MatomoVisitsSummary staging complete. {TotalRows} summaries. Running aggregation SP",
            totalRows);

        await _stagingWriter.RunAggregationAsync(cancellationToken);

        _logger.LogInformation("MatomoVisitsSummary sync completed");

        return SyncResult.Ok(totalRows);
    }
}
