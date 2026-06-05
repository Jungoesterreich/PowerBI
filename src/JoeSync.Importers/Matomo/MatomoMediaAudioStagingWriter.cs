// Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.

using System.Data;
using JoeSync.Importers.Matomo.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JoeSync.Importers.Matomo;

public sealed class MatomoMediaAudioStagingWriter
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MatomoMediaAudioStagingWriter> _logger;

    public MatomoMediaAudioStagingWriter(
        IConfiguration configuration,
        ILogger<MatomoMediaAudioStagingWriter> logger)
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

    public async Task<int> WriteMediaAudioAsync(
        int idSite,
        DateOnly date,
        List<MatomoMediaAudio> mediaEntries,
        CancellationToken cancellationToken = default)
    {
        if (mediaEntries.Count == 0)
        {
            return 0;
        }

        await using var connection = new SqlConnection(GetConnectionString());
        await connection.OpenAsync(cancellationToken);

        await DeleteExistingDataAsync(connection, idSite, date, cancellationToken);

        var table = BuildMediaAudioTable(idSite, date, mediaEntries);

        using var bulkCopy = new SqlBulkCopy(
            connection, SqlBulkCopyOptions.TableLock, null);
        bulkCopy.DestinationTableName = "staging.matomo_media_audio";
        bulkCopy.BulkCopyTimeout = 300;

        MapColumns(bulkCopy);
        await bulkCopy.WriteToServerAsync(table, cancellationToken);

        _logger.LogInformation(
            "Wrote {Count} media audio entries for site {IdSite} on {Date}",
            mediaEntries.Count, idSite, date);

        return mediaEntries.Count;
    }

    private static async Task DeleteExistingDataAsync(
        SqlConnection connection,
        int idSite,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            DELETE FROM staging.matomo_media_audio
            WHERE IdSite = @idSite AND [Date] = @date;
            """;
        cmd.Parameters.AddWithValue("@idSite", idSite);
        cmd.Parameters.AddWithValue("@date", date.ToDateTime(TimeOnly.MinValue));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private static DataTable BuildMediaAudioTable(
        int idSite,
        DateOnly date,
        List<MatomoMediaAudio> entries)
    {
        var table = new DataTable();
        table.Columns.Add("IdSite", typeof(int));
        table.Columns.Add("Date", typeof(DateTime));
        table.Columns.Add("Label", typeof(string));
        table.Columns.Add("Url", typeof(string));
        table.Columns.Add("Plays", typeof(int));
        table.Columns.Add("UniqueVisitorsPlays", typeof(int));
        table.Columns.Add("Impressions", typeof(int));
        table.Columns.Add("UniqueVisitorsImpressions", typeof(int));
        table.Columns.Add("Finishes", typeof(int));
        table.Columns.Add("PlayRate", typeof(double));
        table.Columns.Add("FinishRate", typeof(double));
        table.Columns.Add("AvgTimeWatched", typeof(int));
        table.Columns.Add("AvgCompletion", typeof(double));
        table.Columns.Add("AvgTimeToPlay", typeof(int));
        table.Columns.Add("AvgMediaLength", typeof(int));

        var dateValue = date.ToDateTime(TimeOnly.MinValue);

        foreach (var e in entries)
        {
            table.Rows.Add(
                idSite,
                dateValue,
                N(e.Label),
                N(e.Url),
                e.Plays,
                e.UniqueVisitorsPlays,
                e.Impressions,
                e.UniqueVisitorsImpressions,
                e.Finishes,
                e.PlayRate,
                e.FinishRate,
                e.AvgTimeWatched,
                e.AvgCompletion,
                e.AvgTimeToPlay,
                e.AvgMediaLength);
        }

        return table;
    }

    private static void MapColumns(SqlBulkCopy bulkCopy)
    {
        var columns = new[]
        {
            "IdSite", "Date", "Label", "Url", "Plays", "UniqueVisitorsPlays",
            "Impressions", "UniqueVisitorsImpressions", "Finishes",
            "PlayRate", "FinishRate", "AvgTimeWatched", "AvgCompletion",
            "AvgTimeToPlay", "AvgMediaLength",
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
        cmd.CommandText = "EXEC staging.usp_ProcessMediaAudioDaily";
        cmd.CommandTimeout = 300;
        await cmd.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogInformation("Aggregation SP usp_ProcessMediaAudioDaily completed");
    }

    private static object N(string? value) =>
        (object?)value ?? DBNull.Value;
}
