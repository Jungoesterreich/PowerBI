-- =========================================================================
-- == JoeSync canonical initial schema (non-EF SQL objects)
-- ==
-- == Consolidated end-state of all 16 EF Core migrations up to and including
-- == 20260515120000_OptimizeEnrichEditionLookup. Each object below is the
-- == final winning definition after all later overrides and ALTERs have been
-- == applied. Curated tables and logging/SyncLog remain EF-managed and are
-- == intentionally omitted; this file covers only what EF cannot generate:
-- ==   * staging.* tables
-- ==   * staging.usp_* stored procedures
-- ==   * pbi.vw_* views
-- ==
-- == Execute top-to-bottom against an empty JOEDB_KPI after EF has created
-- == its curated.* / logging.* tables.
-- =========================================================================

-- =========================================================================
-- == SCHEMAS (informational - EF generates these via EnsureSchema)
-- =========================================================================
-- staging, pbi (curated and logging handled by EF)

-- =========================================================================
-- == STAGING TABLES
-- =========================================================================

-- staging.matomo_visits
-- Base from 20260323105952_AddMatomoSchema; the 7 columns at the end were
-- added by 20260331100000_AlignWithSemanticModel (OperatingSystemCode,
-- BrowserCode, BrowserFamilyDescription, Continent, ContinentCode,
-- RegionCode, Location) — merged inline below.
CREATE TABLE staging.matomo_visits (
    Id                          int IDENTITY(1,1) PRIMARY KEY,
    IdSite                      int NOT NULL,
    IdVisit                     bigint NOT NULL,
    VisitorId                   nvarchar(50),
    VisitIp                     nvarchar(50),
    UserId                      nvarchar(200),
    ServerDate                  date NOT NULL,
    ServerTimestamp             bigint,
    FirstActionTimestamp        bigint,
    LastActionTimestamp         bigint,
    VisitDuration               int,
    Actions                     int,
    Searches                    int,
    Events                      int,
    Interactions                int,
    VisitorType                 nvarchar(20),
    VisitCount                  int,
    DaysSinceFirstVisit         int,
    DaysSinceLastVisit          int,
    ReferrerType                nvarchar(50),
    ReferrerName                nvarchar(500),
    ReferrerUrl                 nvarchar(max),
    ReferrerKeyword             nvarchar(500),
    DeviceType                  nvarchar(50),
    DeviceBrand                 nvarchar(100),
    DeviceModel                 nvarchar(100),
    OperatingSystem             nvarchar(100),
    OperatingSystemName         nvarchar(100),
    OperatingSystemVersion      nvarchar(50),
    Browser                     nvarchar(100),
    BrowserName                 nvarchar(100),
    BrowserVersion              nvarchar(50),
    BrowserFamily               nvarchar(50),
    Country                     nvarchar(100),
    CountryCode                 nvarchar(10),
    Region                      nvarchar(100),
    City                        nvarchar(100),
    Latitude                    nvarchar(20),
    Longitude                   nvarchar(20),
    Language                    nvarchar(100),
    LanguageCode                nvarchar(10),
    Resolution                  nvarchar(20),
    CampaignId                  nvarchar(200),
    CampaignName                nvarchar(200),
    CampaignKeyword             nvarchar(200),
    CampaignContent             nvarchar(500),
    CampaignSource              nvarchar(200),
    CampaignMedium              nvarchar(200),
    VisitConverted              int,
    GoalConversions             int,
    FormConversions             int,
    SyncedAt                    datetime2 NOT NULL DEFAULT GETUTCDATE(),
    -- Added by 20260331100000_AlignWithSemanticModel
    OperatingSystemCode         nvarchar(50) NULL,
    BrowserCode                 nvarchar(50) NULL,
    BrowserFamilyDescription    nvarchar(200) NULL,
    Continent                   nvarchar(100) NULL,
    ContinentCode               nvarchar(10) NULL,
    RegionCode                  nvarchar(50) NULL,
    Location                    nvarchar(200) NULL
);

CREATE UNIQUE INDEX UX_matomo_visits_IdSite_IdVisit
    ON staging.matomo_visits (IdSite, IdVisit);


-- staging.matomo_action_details
-- From 20260323105952_AddMatomoSchema (unchanged in later migrations).
CREATE TABLE staging.matomo_action_details (
    Id                  int IDENTITY(1,1) PRIMARY KEY,
    IdSite              int NOT NULL,
    IdVisit             bigint NOT NULL,
    Type                nvarchar(50),
    Url                 nvarchar(max),
    PageTitle           nvarchar(1000),
    PageIdAction        int,
    IdPageview          nvarchar(50),
    Timestamp           bigint,
    PageId              int,
    TimeSpent           int,
    PageLoadTimeMs      int,
    PageviewPosition    int,
    EventCategory       nvarchar(max),
    EventAction         nvarchar(max),
    EventName           nvarchar(max),
    Dimension2          nvarchar(max),
    Dimension3          nvarchar(max),
    SyncedAt            datetime2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE INDEX IX_matomo_action_details_IdSite_IdVisit
    ON staging.matomo_action_details (IdSite, IdVisit);


-- staging.matomo_visits_summary
-- From 20260323132334_AddPbiMigration (unchanged in later migrations).
CREATE TABLE staging.matomo_visits_summary (
    Id                  int IDENTITY(1,1) PRIMARY KEY,
    IdSite              int NOT NULL,
    [Date]              date NOT NULL,
    Visits              int,
    UniqueVisitors      int,
    Actions             int,
    PageViews           int,
    UniquePageViews     int,
    Downloads           int,
    UniqueDownloads     int,
    BounceCount         int,
    SumVisitLength     int,
    Searches            int,
    Events              int,
    SyncedAt            datetime2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE UNIQUE INDEX UX_matomo_visits_summary
    ON staging.matomo_visits_summary (IdSite, [Date]);


-- staging.matomo_media_audio
-- Base from 20260323132334_AddPbiMigration; Url column added by
-- 20260416180000_AddUrlToStagingMediaAudio — merged inline below.
CREATE TABLE staging.matomo_media_audio (
    Id                          int IDENTITY(1,1) PRIMARY KEY,
    IdSite                      int NOT NULL,
    [Date]                      date NOT NULL,
    Label                       nvarchar(2000),
    Plays                       int,
    UniqueVisitorsPlays         int,
    Impressions                 int,
    UniqueVisitorsImpressions   int,
    Finishes                    int,
    PlayRate                    float,
    FinishRate                  float,
    AvgTimeWatched              int,
    AvgCompletion               float,
    AvgTimeToPlay               int,
    AvgMediaLength              int,
    SyncedAt                    datetime2 NOT NULL DEFAULT GETUTCDATE(),
    -- Added by 20260416180000_AddUrlToStagingMediaAudio
    Url                         nvarchar(2000) NULL
);

CREATE INDEX IX_matomo_media_audio_IdSite_Date
    ON staging.matomo_media_audio (IdSite, [Date]);


-- =========================================================================
-- == STORED PROCEDURES
-- =========================================================================

-- staging.usp_ProcessMatomoDaily
-- Latest definition: 20260323132334_AddPbiMigration (no later override).
CREATE OR ALTER PROCEDURE staging.usp_ProcessMatomoDaily
AS
BEGIN
    SET NOCOUNT ON;

    MERGE curated.matomo_daily_summary AS target
    USING (
        SELECT
            v.IdSite,
            v.ServerDate AS [Date],
            COUNT(DISTINCT v.IdVisit) AS Visits,
            COUNT(DISTINCT v.VisitorId) AS UniqueVisitors,
            ISNULL(SUM(CASE WHEN a.Type = 'action' THEN 1 ELSE 0 END), 0) AS PageViews,
            ISNULL(SUM(CASE WHEN a.Type = 'download' THEN 1 ELSE 0 END), 0) AS Downloads,
            ISNULL(SUM(CASE WHEN a.Type = 'event' THEN 1 ELSE 0 END), 0) AS [Events],
            ISNULL(SUM(DISTINCT CASE WHEN v.Searches > 0 THEN v.Searches ELSE 0 END), 0) AS Searches,
            AVG(v.VisitDuration) AS AvgVisitDuration,
            SUM(CASE WHEN v.Actions = 1 THEN 1 ELSE 0 END) AS BounceCount,
            ISNULL(s.UniquePageViews, 0) AS UniquePageViews,
            ISNULL(s.UniqueDownloads, 0) AS UniqueDownloads
        FROM staging.matomo_visits v
        LEFT JOIN staging.matomo_action_details a
            ON v.IdSite = a.IdSite AND v.IdVisit = a.IdVisit
        LEFT JOIN staging.matomo_visits_summary s
            ON v.IdSite = s.IdSite AND v.ServerDate = s.[Date]
        GROUP BY v.IdSite, v.ServerDate, s.UniquePageViews, s.UniqueDownloads
    ) AS source
    ON target.IdSite = source.IdSite AND target.[Date] = source.[Date]
    WHEN MATCHED THEN
        UPDATE SET
            Visits = source.Visits,
            UniqueVisitors = source.UniqueVisitors,
            PageViews = source.PageViews,
            Downloads = source.Downloads,
            Events = source.[Events],
            Searches = source.Searches,
            AvgVisitDuration = source.AvgVisitDuration,
            BounceCount = source.BounceCount,
            UniquePageViews = source.UniquePageViews,
            UniqueDownloads = source.UniqueDownloads
    WHEN NOT MATCHED THEN
        INSERT (IdSite, SiteName, [Date], Visits, UniqueVisitors,
                PageViews, Downloads, Events, Searches,
                AvgVisitDuration, BounceCount,
                UniquePageViews, UniqueDownloads)
        VALUES (source.IdSite,
                CASE source.IdSite
                    WHEN 1 THEN 'jungoe'
                    WHEN 2 THEN 'msdigi'
                    WHEN 3 THEN 'spdigi'
                    WHEN 4 THEN 'luxdigi'
                    WHEN 5 THEN 'joedigi'
                    WHEN 6 THEN 'topicdigi'
                    WHEN 10 THEN 'lesen'
                    ELSE 'unknown'
                END,
                source.[Date], source.Visits, source.UniqueVisitors,
                source.PageViews, source.Downloads, source.[Events],
                source.Searches, source.AvgVisitDuration,
                source.BounceCount,
                source.UniquePageViews, source.UniqueDownloads);
END
GO


-- staging.usp_ProcessMediaAudioDaily
-- Latest definition: 20260417112126_RestoreMediaAudioMergeDedup
-- (re-adds GROUP BY + MAX(s.Url) on top of FixEditionMatching's substring
--  Edition match).
CREATE OR ALTER PROCEDURE staging.usp_ProcessMediaAudioDaily
AS
BEGIN
    SET NOCOUNT ON;

    MERGE curated.matomo_media_audio_daily AS target
    USING (
        SELECT
            s.IdSite,
            CASE s.IdSite
                WHEN 1 THEN 'jungoe'
                WHEN 2 THEN 'msdigi'
                WHEN 3 THEN 'spdigi'
                WHEN 4 THEN 'luxdigi'
                WHEN 5 THEN 'joedigi'
                WHEN 6 THEN 'topicdigi'
                WHEN 10 THEN 'lesen'
                ELSE 'unknown'
            END AS SiteName,
            s.[Date],
            s.Label,
            SUM(s.Plays) AS Plays,
            SUM(s.UniqueVisitorsPlays) AS UniqueVisitorsPlays,
            SUM(s.Finishes) AS Finishes,
            MAX(s.Url) AS Url,
            CASE
                WHEN MAX(s.Url) IS NOT NULL AND CHARINDEX('/', MAX(s.Url)) > 0
                THEN RIGHT(MAX(s.Url), CHARINDEX('/', REVERSE(MAX(s.Url))) - 1)
                ELSE MAX(s.Url)
            END AS UrlLastPathElement,
            -- Edition: match Ausgabenkuerzel as substring in
            -- the Url. ORDER BY LEN DESC keeps the longest
            -- Kuerzel winning when several would match.
            (SELECT TOP 1 da.Ausgabenkuerzel
             FROM curated.dim_ausgaben da
             WHERE LOWER(MAX(s.Url)) LIKE '%' + LOWER(da.Ausgabenkuerzel) + '%'
             ORDER BY LEN(da.Ausgabenkuerzel) DESC
            ) AS Edition,
            MAX(s.PlayRate) AS PlayRate,
            MAX(s.FinishRate) AS FinishRate,
            MAX(s.AvgTimeWatched) AS AvgTimeWatched,
            MAX(s.AvgCompletion) AS AvgCompletion
        FROM staging.matomo_media_audio s
        GROUP BY s.IdSite, s.[Date], s.Label
    ) AS source
    ON target.IdSite = source.IdSite
        AND target.[Date] = source.[Date]
        AND ISNULL(target.Label, '') = ISNULL(source.Label, '')
    WHEN MATCHED THEN
        UPDATE SET
            Plays = source.Plays,
            UniqueVisitorsPlays = source.UniqueVisitorsPlays,
            Finishes = source.Finishes,
            Url = source.Url,
            UrlLastPathElement = source.UrlLastPathElement,
            Edition = source.Edition,
            PlayRate = source.PlayRate,
            FinishRate = source.FinishRate,
            AvgTimeWatched = source.AvgTimeWatched,
            AvgCompletion = source.AvgCompletion
    WHEN NOT MATCHED THEN
        INSERT (IdSite, SiteName, [Date], Label, Plays,
                UniqueVisitorsPlays, Finishes, Url,
                UrlLastPathElement, Edition, PlayRate,
                FinishRate, AvgTimeWatched, AvgCompletion)
        VALUES (source.IdSite, source.SiteName, source.[Date],
                source.Label, source.Plays,
                source.UniqueVisitorsPlays, source.Finishes,
                source.Url, source.UrlLastPathElement,
                source.Edition, source.PlayRate,
                source.FinishRate, source.AvgTimeWatched,
                source.AvgCompletion);
END
GO


-- staging.usp_EnrichActionDetails
-- Latest definition: 20260515120000_OptimizeEnrichEditionLookup
-- (precomputed #UrlEdition lookup; full visit-join + SearchPattern apply).
CREATE OR ALTER PROCEDURE staging.usp_EnrichActionDetails
AS
BEGIN
    SET NOCOUNT ON;

    -- 1) DISTINCT cleaned URLs from staging
    IF OBJECT_ID('tempdb..#UniqueUrls') IS NOT NULL
        DROP TABLE #UniqueUrls;

    SELECT DISTINCT
        CASE
            WHEN CHARINDEX('?', a.Url) > 0
            THEN LEFT(a.Url, CHARINDEX('?', a.Url) - 1)
            ELSE a.Url
        END AS UrlClean
    INTO #UniqueUrls
    FROM staging.matomo_action_details a
    WHERE a.Url IS NOT NULL;

    -- 2) Resolve Edition once per distinct URL
    IF OBJECT_ID('tempdb..#UrlEdition') IS NOT NULL
        DROP TABLE #UrlEdition;

    SELECT
        u.UrlClean,
        e.Ausgabenkuerzel AS Edition
    INTO #UrlEdition
    FROM #UniqueUrls u
    OUTER APPLY (
        SELECT TOP 1 da.Ausgabenkuerzel
        FROM curated.dim_ausgaben da
        WHERE LOWER(u.UrlClean) LIKE '%' + LOWER(da.Ausgabenkuerzel) + '%'
        ORDER BY LEN(da.Ausgabenkuerzel) DESC
    ) e;

    -- No index on #UrlEdition: UrlClean is nvarchar(max) so
    -- it can't be a key column. The optimizer will hash-join
    -- against this small (~tens of thousands of rows) table.

    -- 3) Main INSERT — joins on the precomputed lookup
    TRUNCATE TABLE curated.matomo_action_details_enriched;

    INSERT INTO curated.matomo_action_details_enriched
        (IdSite, SiteName, IdVisit, Type, Url, PageTitle,
         Timestamp, TimeSpent,
         UrlLastPathElement, UrlParameter, UrlClean,
         UrlPathFull, UrlPathElementOne, UrlPathElementTwo,
         FileType, Edition, SearchPatternIdu, SearchPatternTo,
         Seitenart, SubRubrik,
         ServerDate, VisitorId, VisitDuration, DeviceType,
         Browser, Country, City)
    SELECT
        a.IdSite,
        CASE a.IdSite
            WHEN 1 THEN 'jungoe'
            WHEN 2 THEN 'msdigi'
            WHEN 3 THEN 'spdigi'
            WHEN 4 THEN 'luxdigi'
            WHEN 5 THEN 'joedigi'
            WHEN 6 THEN 'topicdigi'
            WHEN 10 THEN 'lesen'
            ELSE 'unknown'
        END AS SiteName,
        a.IdVisit, a.Type, a.Url, a.PageTitle,
        a.Timestamp, a.TimeSpent,

        CASE
            WHEN cleanUrl.val IS NOT NULL AND CHARINDEX('/', cleanUrl.val) > 0
            THEN RIGHT(cleanUrl.val, CHARINDEX('/', REVERSE(cleanUrl.val)) - 1)
            ELSE NULL
        END,

        CASE
            WHEN CHARINDEX('?', a.Url) > 0
            THEN SUBSTRING(a.Url, CHARINDEX('?', a.Url) + 1, LEN(a.Url))
            ELSE NULL
        END,

        cleanUrl.val,

        CASE
            WHEN cleanUrl.val IS NOT NULL AND CHARINDEX('/', cleanUrl.val, 9) > 0
            THEN SUBSTRING(cleanUrl.val, CHARINDEX('/', cleanUrl.val, 9), LEN(cleanUrl.val))
            ELSE '/'
        END,

        pathOne.val,
        pathTwo.val,

        CASE
            WHEN LOWER(a.Url) LIKE '%.pdf%' THEN 'PDF'
            WHEN LOWER(a.Url) LIKE '%.mp3%' THEN 'MP3'
            ELSE NULL
        END,

        -- Edition: precomputed via #UrlEdition lookup
        ue.Edition,

        lastPath.SearchPatternIdu,
        lastPath.SearchPatternTo,

        CASE
            WHEN a.Url IS NULL THEN NULL
            WHEN a.Type = 'download' THEN 'Download'
            WHEN a.Type = 'event' THEN 'Event'
            WHEN pathOne.val IS NULL OR pathOne.val = '' THEN 'Startseite'
            WHEN LOWER(pathOne.val) = 'quiz' THEN 'Quiz'
            WHEN LOWER(pathOne.val) = 'blaetterbuch'
              OR LOWER(pathOne.val) LIKE 'bl_tterbuch' THEN N'Blätterbuch'
            WHEN LOWER(pathOne.val) = 'suche'
              OR LOWER(pathOne.val) = 'search' THEN 'Suche'
            WHEN LOWER(pathOne.val) = 'digital-lesen' THEN 'Digital-Lesen'
            WHEN LOWER(pathOne.val) IN ('rubrik', 'rubriken', 'thema', 'themen') THEN 'Rubrik'
            WHEN a.Type = 'action' THEN 'Beitrag'
            ELSE 'Sonstige'
        END,

        CASE
            WHEN LOWER(pathOne.val) = 'digital-lesen' THEN
                CASE
                    WHEN LOWER(pathTwo.val) = 'hoeren'
                      OR LOWER(pathTwo.val) LIKE 'h_ren' THEN N'Hören'
                    WHEN LOWER(pathTwo.val) = 'lesen' THEN 'Lesen'
                    WHEN LOWER(pathTwo.val) = 'spielen' THEN 'Spielen'
                    WHEN LOWER(pathTwo.val) = 'lernen' THEN 'Lernen'
                    WHEN LOWER(pathTwo.val) = 'basteln' THEN 'Basteln'
                    WHEN pathTwo.val IS NOT NULL THEN pathTwo.val
                    ELSE 'Digital-Lesen Sonstige'
                END
            ELSE NULL
        END,

        v.ServerDate,
        v.VisitorId,
        v.VisitDuration,
        v.DeviceType,
        v.BrowserName,
        v.Country,
        v.City

    FROM staging.matomo_action_details a
    INNER JOIN staging.matomo_visits v
        ON a.IdSite = v.IdSite AND a.IdVisit = v.IdVisit
    CROSS APPLY (
        SELECT CASE
            WHEN CHARINDEX('?', a.Url) > 0
            THEN LEFT(a.Url, CHARINDEX('?', a.Url) - 1)
            ELSE a.Url
        END AS val
    ) cleanUrl
    LEFT JOIN #UrlEdition ue
        ON ue.UrlClean = cleanUrl.val
    CROSS APPLY (
        SELECT CASE
            WHEN cleanUrl.val IS NOT NULL AND CHARINDEX('/', cleanUrl.val, 9) > 0
            THEN CASE
                WHEN CHARINDEX('/', cleanUrl.val, CHARINDEX('/', cleanUrl.val, 9) + 1) > 0
                THEN SUBSTRING(
                    cleanUrl.val,
                    CHARINDEX('/', cleanUrl.val, 9) + 1,
                    CHARINDEX('/', cleanUrl.val, CHARINDEX('/', cleanUrl.val, 9) + 1) - CHARINDEX('/', cleanUrl.val, 9) - 1
                )
                ELSE SUBSTRING(cleanUrl.val, CHARINDEX('/', cleanUrl.val, 9) + 1, LEN(cleanUrl.val))
            END
            ELSE NULL
        END AS val
    ) pathOne
    CROSS APPLY (
        SELECT CASE
            WHEN cleanUrl.val IS NOT NULL
                AND CHARINDEX('/', cleanUrl.val, 9) > 0
                AND CHARINDEX('/', cleanUrl.val, CHARINDEX('/', cleanUrl.val, 9) + 1) > 0
            THEN CASE
                WHEN CHARINDEX('/', cleanUrl.val, CHARINDEX('/', cleanUrl.val, CHARINDEX('/', cleanUrl.val, 9) + 1) + 1) > 0
                THEN SUBSTRING(
                    cleanUrl.val,
                    CHARINDEX('/', cleanUrl.val, CHARINDEX('/', cleanUrl.val, 9) + 1) + 1,
                    CHARINDEX('/', cleanUrl.val, CHARINDEX('/', cleanUrl.val, CHARINDEX('/', cleanUrl.val, 9) + 1) + 1)
                        - CHARINDEX('/', cleanUrl.val, CHARINDEX('/', cleanUrl.val, 9) + 1) - 1
                )
                ELSE SUBSTRING(
                    cleanUrl.val,
                    CHARINDEX('/', cleanUrl.val, CHARINDEX('/', cleanUrl.val, 9) + 1) + 1,
                    LEN(cleanUrl.val)
                )
            END
            ELSE NULL
        END AS val
    ) pathTwo
    CROSS APPLY (
        SELECT
            CASE
                WHEN LOWER(a.Url) LIKE '%daz-spiel%' THEN 'daz-spiel'
                WHEN LOWER(a.Url) LIKE '%spielideen%' THEN 'spielideen'
                WHEN LOWER(a.Url) LIKE '%bewegungseinheit%' THEN 'bewegungseinheit'
                WHEN LOWER(a.Url) LIKE '%sprache%' THEN 'sprache'
                WHEN LOWER(a.Url) LIKE '%arbeitsmaterial%' THEN 'arbeitsmaterial'
                WHEN LOWER(a.Url) LIKE '%arbeitsblatt%' THEN 'arbeitsblatt'
                WHEN LOWER(a.Url) LIKE '%ausmalbild%' THEN 'ausmalbild'
                WHEN LOWER(a.Url) LIKE '%vorlesegeschichte%' THEN 'vorlesegeschichte'
                WHEN LOWER(a.Url) LIKE '%englisch-arbeitsanleitung%' THEN 'englisch-arbeitsanleitung'
                WHEN LOWER(a.Url) LIKE '%englisch-lied-notenblatt%' THEN 'englisch-lied-notenblatt'
                WHEN LOWER(a.Url) LIKE '%englisch-lied-karaoke%' THEN 'englisch-lied-karaoke'
                WHEN LOWER(a.Url) LIKE '%englisch-lied%' THEN 'englisch-lied'
                WHEN LOWER(a.Url) LIKE '%lied-notenblatt%' THEN 'lied-notenblatt'
                WHEN LOWER(a.Url) LIKE '%lied-karaoke%' THEN 'lied-karaoke'
                WHEN LOWER(a.Url) LIKE '%lied%' THEN 'lied'
                WHEN LOWER(a.Url) LIKE '%fehlersuchbild-loesung%' THEN 'fehlersuchbild-loesung'
                WHEN LOWER(a.Url) LIKE '%fehlersuchbild%' THEN 'fehlersuchbild'
                WHEN LOWER(a.Url) LIKE '%fachinformation%' THEN 'fachinformation'
                ELSE ''
            END AS SearchPatternIdu,
            CASE
                WHEN LOWER(a.Url) LIKE '%level-1-ts%' THEN 'level-1-ts'
                WHEN LOWER(a.Url) LIKE '%level-2-ts%' THEN 'level-2-ts'
                WHEN LOWER(a.Url) LIKE '%oe1%' THEN 'oe1'
                WHEN LOWER(a.Url) LIKE '%daz%' THEN 'daz'
                WHEN LOWER(a.Url) LIKE '%kurzgeschichte%' THEN 'kurzgeschichte'
                WHEN LOWER(a.Url) LIKE '%polbil%' THEN 'polbil'
                WHEN LOWER(a.Url) LIKE '%digital-var%' THEN 'digital-var'
                WHEN LOWER(a.Url) LIKE '%leserallye%' THEN 'leserallye'
                WHEN LOWER(a.Url) LIKE '%englisch%' THEN 'englisch'
                WHEN LOWER(a.Url) LIKE '%mint%' THEN 'mint'
                WHEN LOWER(a.Url) LIKE '%lesecheck%' THEN 'lesecheck'
                WHEN LOWER(a.Url) LIKE '%lesestrategien%' THEN 'polbil'
                ELSE ''
            END AS SearchPatternTo
    ) lastPath;

    DROP TABLE #UrlEdition;
    DROP TABLE #UniqueUrls;
END
GO


-- =========================================================================
-- == PBI VIEWS
-- == (in dependency order: dim views first, then matomo_daily,
-- ==  matomo_media_audio, matomo_visits_details, then pageviews/downloads
-- ==  which depend on matomo_visits_details)
-- =========================================================================

-- pbi.vw_dim_date
-- Latest: 20260323145000_ExtendVisitsDetailsAndAddDimDateView
CREATE OR ALTER VIEW pbi.vw_dim_date AS
SELECT
    [Date],
    DayNum,
    MonthNum,
    MonthName,
    [Month],
    MonthID,
    DayID,
    [Year]
FROM curated.dim_date;
GO


-- pbi.vw_dim_ausgaben
-- Latest: 20260323145000_ExtendVisitsDetailsAndAddDimDateView
CREATE OR ALTER VIEW pbi.vw_dim_ausgaben AS
SELECT
    Ausgabenkuerzel,
    Schuljahr,
    Jahr,
    Ausgabe,
    AusgabeIDU
FROM curated.dim_ausgaben;
GO


-- pbi.vw_date_monthname
-- Latest: 20260416191000_FixDateMonthnameView (dropped [Month] / MonthID
-- to match DAX grouping)
CREATE OR ALTER VIEW pbi.vw_date_monthname AS
SELECT DISTINCT
    MonthNum,
    MonthName
FROM curated.dim_date;
GO


-- pbi.vw_matomo_daily
-- Latest: 20260323132334_AddPbiMigration (no later override)
CREATE OR ALTER VIEW pbi.vw_matomo_daily AS
SELECT
    IdSite,
    SiteName,
    [Date],
    Visits,
    UniqueVisitors,
    PageViews,
    UniquePageViews,
    Downloads,
    UniqueDownloads,
    Events,
    Searches,
    AvgVisitDuration,
    ROUND(CAST(AvgVisitDuration AS float) / 60.0, 2) AS AvgVisitDurationMinutes,
    BounceCount,
    ROUND(BounceCount * 100.0 / NULLIF(Visits, 0), 2) AS BounceRate
FROM curated.matomo_daily_summary;
GO


-- pbi.vw_matomo_media_audio
-- Latest: 20260323132334_AddPbiMigration (no later override)
CREATE OR ALTER VIEW pbi.vw_matomo_media_audio AS
SELECT
    IdSite,
    SiteName,
    [Date],
    Label,
    Plays,
    Finishes,
    ROUND(Finishes * 1.0 / NULLIF(Plays, 0), 4) AS FinishRate,
    Url,
    UrlLastPathElement,
    Edition
FROM curated.matomo_media_audio_daily;
GO


-- pbi.vw_matomo_visits_details
-- Latest: 20260331100000_AlignWithSemanticModel
-- (joins curated.enriched with staging.matomo_visits; uses bracket-quoted
--  dot-named aliases that mirror the Power BI semantic model column names.)
CREATE OR ALTER VIEW pbi.vw_matomo_visits_details AS
SELECT
    -- Action-level (from curated)
    e.IdVisit               AS idVisit,
    e.Type                  AS [actionDetails.type],
    e.Url                   AS [actionDetails.url],
    e.PageTitle             AS [actionDetails.pageTitle],
    e.UrlLastPathElement    AS [actionDetails.url.last.path.element],
    e.UrlParameter          AS [actionDetails.url.parameter],
    e.UrlClean              AS [actionDetails.url.clean],
    e.UrlPathFull           AS [actionDetails.url.path.full],
    e.UrlPathElementOne     AS [actionDetails.url.path.element.one],
    e.UrlPathElementTwo     AS [actionDetails.url.path.element.two],
    e.FileType,
    e.Edition,
    e.SearchPatternIdu      AS [SearchPattern.IDU],
    e.SearchPatternTo       AS [SearchPattern.TO],
    e.Seitenart,
    e.SubRubrik,

    -- Visit-level (from staging.matomo_visits)
    v.VisitorId             AS visitorId,
    v.ServerDate            AS serverDate,
    v.LastActionTimestamp    AS lastActionTimestamp,
    DATEADD(
        SECOND,
        v.LastActionTimestamp,
        '1970-01-01')       AS lastActionDateTime,
    e.SiteName              AS siteName,
    v.ServerTimestamp        AS serverTimestamp,
    v.FirstActionTimestamp   AS firstActionTimestamp,
    CAST(DATEADD(
        SECOND,
        v.ServerTimestamp,
        '1970-01-01') AS TIME)
                            AS serverTimePretty,
    v.ServerDate            AS serverDatePretty,
    CAST(DATEADD(
        SECOND,
        v.FirstActionTimestamp,
        '1970-01-01') AS DATE)
                            AS serverDatePrettyFirstAction,
    CAST(DATEADD(
        SECOND,
        v.FirstActionTimestamp,
        '1970-01-01') AS TIME)
                            AS serverTimePrettyFirstAction,
    v.VisitDuration         AS visitDuration,
    CASE
        WHEN v.VisitDuration >= 3600
        THEN CAST(v.VisitDuration / 3600 AS VARCHAR)
            + ' h '
            + CAST((v.VisitDuration % 3600) / 60 AS VARCHAR)
            + ' min'
        WHEN v.VisitDuration >= 60
        THEN CAST(v.VisitDuration / 60 AS VARCHAR)
            + ' min '
            + CAST(v.VisitDuration % 60 AS VARCHAR) + 's'
        ELSE CAST(v.VisitDuration AS VARCHAR) + 's'
    END                     AS visitDurationPretty,
    v.DeviceType            AS deviceType,
    v.DeviceBrand           AS deviceBrand,
    v.DeviceModel           AS deviceModel,
    v.OperatingSystem       AS operatingSystem,
    v.OperatingSystemName   AS operatingSystemName,
    v.OperatingSystemCode   AS operatingSystemCode,
    v.BrowserFamily         AS browserFamily,
    v.BrowserFamilyDescription
                            AS browserFamilyDescription,
    v.Browser               AS browser,
    v.BrowserName           AS browserName,
    v.BrowserCode           AS browserCode,
    v.BrowserVersion        AS browserVersion,
    v.Continent             AS continent,
    v.ContinentCode         AS continentCode,
    v.Country               AS country,
    v.CountryCode           AS countryCode,
    v.Region                AS region,
    v.RegionCode            AS regionCode,
    v.City                  AS city,
    v.Location              AS location,
    v.Latitude              AS latitude,
    v.Longitude             AS longitude,
    v.Resolution            AS resolution
FROM curated.matomo_action_details_enriched e
INNER JOIN staging.matomo_visits v
    ON e.IdSite = v.IdSite AND e.IdVisit = v.IdVisit;
GO


-- pbi.vw_matomo_pageviews
-- Latest: 20260416190000_AlignPageviewsView (adds PageTitle, Seitenart,
-- SubRubrik, siteName to match the PBI semantic model calculated table)
CREATE OR ALTER VIEW pbi.vw_matomo_pageviews AS
SELECT
    vd.[actionDetails.pageTitle]     AS PageTitle,
    vd.[actionDetails.url]           AS url,
    vd.[actionDetails.url.clean]     AS [url.clean],
    vd.[actionDetails.url.parameter] AS [url.parameter],
    vd.[actionDetails.type]          AS type,
    vd.[actionDetails.url.path.full] AS [path.full],
    vd.[actionDetails.url.path.element.one]
                                     AS [path.element.one],
    vd.[actionDetails.url.path.element.two]
                                     AS [path.element.two],
    vd.[actionDetails.url.last.path.element]
                                     AS [last.path.element],
    vd.serverDate,
    vd.Seitenart,
    vd.SubRubrik,
    vd.siteName
FROM pbi.vw_matomo_visits_details vd
WHERE vd.[actionDetails.type] = 'action'
    AND (vd.[actionDetails.url.parameter] IS NULL
        OR vd.[actionDetails.url.parameter] = ''
        OR vd.[actionDetails.url.parameter]
            LIKE 'tx_felogin%');
GO


-- pbi.vw_matomo_downloads
-- Latest: 20260331100000_AlignWithSemanticModel (no later override)
CREATE OR ALTER VIEW pbi.vw_matomo_downloads AS
SELECT
    vd.[actionDetails.url]          AS url,
    vd.[actionDetails.url.clean]    AS [url.clean],
    vd.[actionDetails.url.parameter] AS [url.parameter],
    vd.[actionDetails.type]         AS type,
    vd.[actionDetails.url.path.full] AS [path.full],
    vd.[actionDetails.url.path.element.one]
                                    AS [path.element.one],
    vd.[actionDetails.url.path.element.two]
                                    AS [path.element.two],
    vd.[actionDetails.url.last.path.element]
                                    AS [last.path.element],
    vd.serverDate
FROM pbi.vw_matomo_visits_details vd
WHERE vd.[actionDetails.type] = 'download'
    AND (vd.[actionDetails.url.parameter] IS NULL
        OR vd.[actionDetails.url.parameter] = '');
GO


-- pbi.vw_downloads_idu
-- Latest: 20260417072207_FixJungoeDownloadViews
CREATE OR ALTER VIEW pbi.vw_downloads_idu AS
SELECT * FROM pbi.vw_matomo_visits_details
WHERE [actionDetails.type] = 'download'
    AND [actionDetails.url] LIKE '%ich-und-du%';
GO


-- pbi.vw_downloads_joe
-- Latest: 20260417072207_FixJungoeDownloadViews
CREATE OR ALTER VIEW pbi.vw_downloads_joe AS
SELECT * FROM pbi.vw_matomo_visits_details
WHERE [actionDetails.type] = 'download'
    AND [actionDetails.url] LIKE '%joe-%';
GO


-- pbi.vw_downloads_jin
-- Latest: 20260417072207_FixJungoeDownloadViews
CREATE OR ALTER VIEW pbi.vw_downloads_jin AS
SELECT * FROM pbi.vw_matomo_visits_details
WHERE [actionDetails.type] = 'download'
    AND [actionDetails.url] LIKE '%join-in-%';
GO


-- pbi.vw_downloads_ms
-- Latest: 20260417072207_FixJungoeDownloadViews
CREATE OR ALTER VIEW pbi.vw_downloads_ms AS
SELECT * FROM pbi.vw_matomo_visits_details
WHERE [actionDetails.type] = 'download'
    AND [actionDetails.url] LIKE '%mini-spatzenpost-%';
GO


-- pbi.vw_downloads_sp
-- Latest: 20260417072207_FixJungoeDownloadViews
-- Spatzenpost excludes mini-spatzenpost (mirrors the DAX:
--   CONTAINSSTRING(url, "spatzenpost-")
--     && NOT CONTAINSSTRING(url, "mini-spatzenpost"))
CREATE OR ALTER VIEW pbi.vw_downloads_sp AS
SELECT * FROM pbi.vw_matomo_visits_details
WHERE [actionDetails.type] = 'download'
    AND [actionDetails.url] LIKE '%spatzenpost-%'
    AND [actionDetails.url] NOT LIKE '%mini-spatzenpost%';
GO


-- pbi.vw_downloads_lux
-- Latest: 20260417072207_FixJungoeDownloadViews
CREATE OR ALTER VIEW pbi.vw_downloads_lux AS
SELECT * FROM pbi.vw_matomo_visits_details
WHERE [actionDetails.type] = 'download'
    AND [actionDetails.url] LIKE '%lux-%';
GO


-- pbi.vw_downloads_to
-- Latest: 20260417072207_FixJungoeDownloadViews
CREATE OR ALTER VIEW pbi.vw_downloads_to AS
SELECT * FROM pbi.vw_matomo_visits_details
WHERE [actionDetails.type] = 'download'
    AND [actionDetails.url] LIKE '%topic-%';
GO
