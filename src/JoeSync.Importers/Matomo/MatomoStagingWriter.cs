// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using System.Data;
using JoeSync.Importers.Matomo.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JoeSync.Importers.Matomo;

public sealed class MatomoStagingWriter
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MatomoStagingWriter> _logger;

    public MatomoStagingWriter(
        IConfiguration configuration,
        ILogger<MatomoStagingWriter> logger)
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

    public async Task<int> WriteVisitsAsync(
        List<MatomoVisit> visits,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        if (visits.Count == 0)
        {
            return 0;
        }

        // Matomo's Live.getLastVisitsDetails is offset-paginated over a
        // live-sorted stream, so the same IdVisit can appear on adjacent
        // pages when new visits arrive mid-fetch. Dedupe before bulk
        // insert to satisfy UX_matomo_visits_IdSite_IdVisit.
        var deduped = DeduplicateVisits(visits);

        var idSite = deduped[0].IdSite;
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await DeleteExistingDataAsync(
            connection, idSite, date, deduped, cancellationToken);

        var visitCount = await BulkInsertVisitsAsync(
            connection, deduped, cancellationToken);

        var actionCount = await BulkInsertActionsAsync(
            connection, deduped, cancellationToken);

        _logger.LogInformation(
            "Wrote {VisitCount} visits and {ActionCount} actions "
            + "for site {IdSite} on {Date}",
            visitCount, actionCount, idSite, date);

        return visitCount;
    }

    internal static List<MatomoVisit> DeduplicateVisits(List<MatomoVisit> visits)
    {
        var seen = new HashSet<long>(visits.Count);
        var result = new List<MatomoVisit>(visits.Count);
        foreach (var v in visits)
        {
            if (seen.Add(v.IdVisit))
            {
                result.Add(v);
            }
        }

        return result;
    }

    private static async Task DeleteExistingDataAsync(
        SqlConnection connection,
        int idSite,
        DateOnly date,
        List<MatomoVisit> incomingVisits,
        CancellationToken cancellationToken = default)
    {
        // Stage incoming IdVisits so we can dedupe by both
        //   (IdSite, ServerDate = @date) — remove stale rows for the date
        //   (IdSite, IdVisit IN incoming) — catches a visit whose ServerDate
        //     drifted between runs, which would otherwise violate the
        //     (IdSite, IdVisit) unique index.
        await using (var createCmd = connection.CreateCommand())
        {
            createCmd.CommandText =
                "CREATE TABLE #incoming_visits (IdVisit BIGINT PRIMARY KEY);";
            await createCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        var table = new DataTable();
        table.Columns.Add("IdVisit", typeof(long));
        foreach (var v in incomingVisits)
        {
            table.Rows.Add(v.IdVisit);
        }

        using (var bulkCopy = new SqlBulkCopy(connection))
        {
            bulkCopy.DestinationTableName = "#incoming_visits";
            bulkCopy.ColumnMappings.Add("IdVisit", "IdVisit");
            await bulkCopy.WriteToServerAsync(table, cancellationToken);
        }

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            DELETE FROM staging.matomo_action_details
            WHERE IdSite = @idSite AND (
                IdVisit IN (
                    SELECT IdVisit FROM staging.matomo_visits
                    WHERE IdSite = @idSite AND ServerDate = @date
                )
                OR IdVisit IN (SELECT IdVisit FROM #incoming_visits)
            );
            DELETE FROM staging.matomo_visits
            WHERE IdSite = @idSite AND (
                ServerDate = @date
                OR IdVisit IN (SELECT IdVisit FROM #incoming_visits)
            );
            DROP TABLE #incoming_visits;
            """;
        cmd.Parameters.AddWithValue("@idSite", idSite);
        cmd.Parameters.AddWithValue("@date", date.ToDateTime(TimeOnly.MinValue));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<int> BulkInsertVisitsAsync(
        SqlConnection connection,
        List<MatomoVisit> visits,
        CancellationToken cancellationToken = default)
    {
        var table = BuildVisitsTable(visits);

        using var bulkCopy = new SqlBulkCopy(
            connection, SqlBulkCopyOptions.TableLock, null);
        bulkCopy.DestinationTableName = "staging.matomo_visits";
        bulkCopy.BulkCopyTimeout = 300;

        MapVisitColumns(bulkCopy);
        await bulkCopy.WriteToServerAsync(table, cancellationToken);
        return visits.Count;
    }

    private static async Task<int> BulkInsertActionsAsync(
        SqlConnection connection,
        List<MatomoVisit> visits,
        CancellationToken cancellationToken = default)
    {
        var table = BuildActionsTable(visits);

        if (table.Rows.Count == 0)
        {
            return 0;
        }

        using var bulkCopy = new SqlBulkCopy(
            connection, SqlBulkCopyOptions.TableLock, null);
        bulkCopy.DestinationTableName = "staging.matomo_action_details";
        bulkCopy.BulkCopyTimeout = 300;

        MapActionColumns(bulkCopy);
        await bulkCopy.WriteToServerAsync(table, cancellationToken);
        return table.Rows.Count;
    }

    private static DataTable BuildVisitsTable(List<MatomoVisit> visits)
    {
        var table = new DataTable();
        table.Columns.Add("IdSite", typeof(int));
        table.Columns.Add("IdVisit", typeof(long));
        table.Columns.Add("VisitorId", typeof(string));
        table.Columns.Add("VisitIp", typeof(string));
        table.Columns.Add("UserId", typeof(string));
        table.Columns.Add("ServerDate", typeof(DateTime));
        table.Columns.Add("ServerTimestamp", typeof(long));
        table.Columns.Add("FirstActionTimestamp", typeof(long));
        table.Columns.Add("LastActionTimestamp", typeof(long));
        table.Columns.Add("VisitDuration", typeof(int));
        table.Columns.Add("Actions", typeof(int));
        table.Columns.Add("Searches", typeof(int));
        table.Columns.Add("Events", typeof(int));
        table.Columns.Add("Interactions", typeof(int));
        table.Columns.Add("VisitorType", typeof(string));
        table.Columns.Add("VisitCount", typeof(int));
        table.Columns.Add("DaysSinceFirstVisit", typeof(int));
        table.Columns.Add("DaysSinceLastVisit", typeof(int));
        table.Columns.Add("ReferrerType", typeof(string));
        table.Columns.Add("ReferrerName", typeof(string));
        table.Columns.Add("ReferrerUrl", typeof(string));
        table.Columns.Add("ReferrerKeyword", typeof(string));
        table.Columns.Add("DeviceType", typeof(string));
        table.Columns.Add("DeviceBrand", typeof(string));
        table.Columns.Add("DeviceModel", typeof(string));
        table.Columns.Add("OperatingSystem", typeof(string));
        table.Columns.Add("OperatingSystemName", typeof(string));
        table.Columns.Add("OperatingSystemCode", typeof(string));
        table.Columns.Add("OperatingSystemVersion", typeof(string));
        table.Columns.Add("Browser", typeof(string));
        table.Columns.Add("BrowserName", typeof(string));
        table.Columns.Add("BrowserCode", typeof(string));
        table.Columns.Add("BrowserVersion", typeof(string));
        table.Columns.Add("BrowserFamily", typeof(string));
        table.Columns.Add("BrowserFamilyDescription", typeof(string));
        table.Columns.Add("Continent", typeof(string));
        table.Columns.Add("ContinentCode", typeof(string));
        table.Columns.Add("Country", typeof(string));
        table.Columns.Add("CountryCode", typeof(string));
        table.Columns.Add("Region", typeof(string));
        table.Columns.Add("RegionCode", typeof(string));
        table.Columns.Add("City", typeof(string));
        table.Columns.Add("Location", typeof(string));
        table.Columns.Add("Latitude", typeof(string));
        table.Columns.Add("Longitude", typeof(string));
        table.Columns.Add("Language", typeof(string));
        table.Columns.Add("LanguageCode", typeof(string));
        table.Columns.Add("Resolution", typeof(string));
        table.Columns.Add("CampaignId", typeof(string));
        table.Columns.Add("CampaignName", typeof(string));
        table.Columns.Add("CampaignKeyword", typeof(string));
        table.Columns.Add("CampaignContent", typeof(string));
        table.Columns.Add("CampaignSource", typeof(string));
        table.Columns.Add("CampaignMedium", typeof(string));
        table.Columns.Add("VisitConverted", typeof(int));
        table.Columns.Add("GoalConversions", typeof(int));
        table.Columns.Add("FormConversions", typeof(int));

        foreach (var v in visits)
        {
            table.Rows.Add(
                v.IdSite, v.IdVisit, N(v.VisitorId), N(v.VisitIp), N(v.UserId),
                DateOnly.Parse(v.ServerDate).ToDateTime(TimeOnly.MinValue),
                N(v.ServerTimestamp), N(v.FirstActionTimestamp), N(v.LastActionTimestamp),
                N(v.VisitDuration), N(v.Actions), N(v.Searches), N(v.Events),
                N(v.Interactions), N(v.VisitorType), N(v.VisitCount),
                N(v.DaysSinceFirstVisit), N(v.DaysSinceLastVisit),
                N(v.ReferrerType), N(v.ReferrerName), N(v.ReferrerUrl),
                N(v.ReferrerKeyword),
                N(v.DeviceType), N(v.DeviceBrand), N(v.DeviceModel),
                N(v.OperatingSystem), N(v.OperatingSystemName),
                N(v.OperatingSystemCode), N(v.OperatingSystemVersion),
                N(v.Browser), N(v.BrowserName), N(v.BrowserCode),
                N(v.BrowserVersion), N(v.BrowserFamily),
                N(v.BrowserFamilyDescription),
                N(v.Continent), N(v.ContinentCode),
                N(v.Country), N(v.CountryCode),
                N(v.Region), N(v.RegionCode), N(v.City), N(v.Location),
                N(v.Latitude), N(v.Longitude),
                N(v.Language), N(v.LanguageCode), N(v.Resolution),
                N(v.CampaignId), N(v.CampaignName), N(v.CampaignKeyword),
                N(v.CampaignContent), N(v.CampaignSource), N(v.CampaignMedium),
                N(v.VisitConverted), N(v.GoalConversions), N(v.FormConversions));
        }

        return table;
    }

    private static DataTable BuildActionsTable(List<MatomoVisit> visits)
    {
        var table = new DataTable();
        table.Columns.Add("IdSite", typeof(int));
        table.Columns.Add("IdVisit", typeof(long));
        table.Columns.Add("Type", typeof(string));
        table.Columns.Add("Url", typeof(string));
        table.Columns.Add("PageTitle", typeof(string));
        table.Columns.Add("PageIdAction", typeof(int));
        table.Columns.Add("IdPageview", typeof(string));
        table.Columns.Add("Timestamp", typeof(long));
        table.Columns.Add("PageId", typeof(int));
        table.Columns.Add("TimeSpent", typeof(int));
        table.Columns.Add("PageLoadTimeMs", typeof(int));
        table.Columns.Add("PageviewPosition", typeof(int));
        table.Columns.Add("EventCategory", typeof(string));
        table.Columns.Add("EventAction", typeof(string));
        table.Columns.Add("EventName", typeof(string));
        table.Columns.Add("Dimension2", typeof(string));
        table.Columns.Add("Dimension3", typeof(string));

        foreach (var v in visits)
        {
            foreach (var a in v.ActionDetails)
            {
                table.Rows.Add(
                    v.IdSite, v.IdVisit,
                    N(a.Type), N(a.Url), N(a.PageTitle), N(a.PageIdAction),
                    N(a.IdPageview), N(a.Timestamp), N(a.PageId),
                    N(a.TimeSpent), N(a.PageLoadTimeMilliseconds),
                    N(a.PageviewPosition),
                    N(a.EventCategory), N(a.EventAction), N(a.EventName),
                    N(a.Dimension2), N(a.Dimension3));
            }
        }

        return table;
    }

    private static void MapVisitColumns(SqlBulkCopy bulkCopy)
    {
        var columns = new[]
        {
            "IdSite", "IdVisit", "VisitorId", "VisitIp", "UserId",
            "ServerDate", "ServerTimestamp", "FirstActionTimestamp",
            "LastActionTimestamp", "VisitDuration", "Actions", "Searches",
            "Events", "Interactions", "VisitorType", "VisitCount",
            "DaysSinceFirstVisit", "DaysSinceLastVisit",
            "ReferrerType", "ReferrerName", "ReferrerUrl", "ReferrerKeyword",
            "DeviceType", "DeviceBrand", "DeviceModel",
            "OperatingSystem", "OperatingSystemName",
            "OperatingSystemCode", "OperatingSystemVersion",
            "Browser", "BrowserName", "BrowserCode",
            "BrowserVersion", "BrowserFamily", "BrowserFamilyDescription",
            "Continent", "ContinentCode",
            "Country", "CountryCode", "Region", "RegionCode",
            "City", "Location",
            "Latitude", "Longitude", "Language", "LanguageCode", "Resolution",
            "CampaignId", "CampaignName", "CampaignKeyword",
            "CampaignContent", "CampaignSource", "CampaignMedium",
            "VisitConverted", "GoalConversions", "FormConversions",
        };

        foreach (var col in columns)
        {
            bulkCopy.ColumnMappings.Add(col, col);
        }
    }

    private static void MapActionColumns(SqlBulkCopy bulkCopy)
    {
        var columns = new[]
        {
            "IdSite", "IdVisit", "Type", "Url", "PageTitle",
            "PageIdAction", "IdPageview", "Timestamp", "PageId",
            "TimeSpent", "PageLoadTimeMs", "PageviewPosition",
            "EventCategory", "EventAction", "EventName",
            "Dimension2", "Dimension3",
        };

        foreach (var col in columns)
        {
            bulkCopy.ColumnMappings.Add(col, col);
        }
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

    public async Task RunEnrichmentAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "EXEC staging.usp_EnrichActionDetails";
        // Enrichment scans ~5M staging rows + per-row LIKE-correlated
        // edition lookup — runtime is hard to bound. Disable the
        // command timeout for this maintenance job.
        cmd.CommandTimeout = 0;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Enrichment SP usp_EnrichActionDetails completed");
    }

    private static object N<T>(T? value) where T : struct =>
        value.HasValue ? value.Value : DBNull.Value;

    private static object N(string? value) =>
        (object?)value ?? DBNull.Value;
}
