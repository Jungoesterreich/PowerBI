// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using JoeSync.Importers.Matomo.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JoeSync.Importers.Matomo;

public sealed class MatomoVisitsSummaryStagingWriter
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MatomoVisitsSummaryStagingWriter> _logger;

    public MatomoVisitsSummaryStagingWriter(
        IConfiguration configuration,
        ILogger<MatomoVisitsSummaryStagingWriter> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private string GetConnectionString()
    {
        return _configuration.GetConnectionString("JoeDB")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:JoeDB is not configured.");
    }

    public async Task WriteSummaryAsync(
        int idSite,
        DateOnly date,
        MatomoVisitsSummary summary,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var deleteCmd = connection.CreateCommand();
        deleteCmd.CommandText = """
            DELETE FROM staging.matomo_visits_summary
            WHERE IdSite = @idSite AND [Date] = @date;
            """;
        deleteCmd.Parameters.AddWithValue("@idSite", idSite);
        deleteCmd.Parameters.AddWithValue("@date", date.ToDateTime(TimeOnly.MinValue));
        await deleteCmd.ExecuteNonQueryAsync(cancellationToken);

        await using var insertCmd = connection.CreateCommand();
        insertCmd.CommandText = """
            INSERT INTO staging.matomo_visits_summary
                (IdSite, [Date], Visits, UniqueVisitors, Actions, PageViews,
                 UniquePageViews, Downloads, UniqueDownloads, BounceCount,
                 SumVisitLength, Searches, Events)
            VALUES
                (@idSite, @date, @visits, @uniqueVisitors, @actions, @pageViews,
                 @uniquePageViews, @downloads, @uniqueDownloads, @bounceCount,
                 @sumVisitLength, @searches, @events);
            """;
        insertCmd.Parameters.AddWithValue("@idSite", idSite);
        insertCmd.Parameters.AddWithValue("@date", date.ToDateTime(TimeOnly.MinValue));
        insertCmd.Parameters.AddWithValue("@visits", summary.Visits);
        insertCmd.Parameters.AddWithValue("@uniqueVisitors", summary.UniqueVisitors);
        insertCmd.Parameters.AddWithValue("@actions", summary.Actions);
        insertCmd.Parameters.AddWithValue("@pageViews", summary.PageViews);
        insertCmd.Parameters.AddWithValue("@uniquePageViews", summary.UniquePageViews);
        insertCmd.Parameters.AddWithValue("@downloads", summary.Downloads);
        insertCmd.Parameters.AddWithValue("@uniqueDownloads", summary.UniqueDownloads);
        insertCmd.Parameters.AddWithValue("@bounceCount", summary.BounceCount);
        insertCmd.Parameters.AddWithValue("@sumVisitLength", summary.SumVisitLength);
        insertCmd.Parameters.AddWithValue("@searches", summary.Searches);
        insertCmd.Parameters.AddWithValue("@events", summary.Events);
        await insertCmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation(
            "Wrote visits summary for site {IdSite} on {Date}",
            idSite, date);
    }

    public async Task RunAggregationAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC staging.usp_ProcessMatomoDaily";
        cmd.CommandTimeout = 300;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Aggregation SP usp_ProcessMatomoDaily completed");
    }
}
