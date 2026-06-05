-- Copyright (c) 2026 Nehl-IT GmbH. All rights reserved.
-- Source: ausgaben.xlsx (Power-BI canonical static dimension)
-- Idempotent seed for curated.dim_ausgaben. Safe to re-run.

SET NOCOUNT ON;

MERGE curated.dim_ausgaben AS tgt
USING (VALUES
    (N'sep22', N'2022/23', 2022, 1, 1),
    (N'okt22', N'2022/23', 2022, 2, NULL),
    (N'nov22', N'2022/23', 2022, 3, 2),
    (N'dez22', N'2022/23', 2022, 4, NULL),
    (N'jan23', N'2022/23', 2023, 5, 3),
    (N'feb23', N'2022/23', 2023, 6, NULL),
    (N'mar23', N'2022/23', 2023, 7, 4),
    (N'apr23', N'2022/23', 2023, 8, NULL),
    (N'mai23', N'2022/23', 2023, 9, 5),
    (N'jun23', N'2022/23', 2023, 10, NULL),
    (N'2223', N'2022/23', NULL, NULL, NULL),
    (N'sep23', N'2023/24', 2023, 1, 1),
    (N'nov23', N'2023/24', 2023, 3, 2),
    (N'dez23', N'2023/24', 2024, 4, NULL),
    (N'jan24', N'2023/24', 2024, 5, 3),
    (N'feb24', N'2023/24', 2024, 6, NULL),
    (N'mar24', N'2023/24', 2024, 7, 4),
    (N'apr24', N'2023/24', 2024, 8, NULL),
    (N'mai24', N'2023/24', 2024, 9, 5),
    (N'jun24', N'2023/24', 2024, 10, NULL),
    (N'2324', N'2023/24', NULL, NULL, NULL),
    (N'sep24', N'2024/25', 2024, 1, 1),
    (N'okt24', N'2024/25', 2024, NULL, NULL),
    (N'nov24', N'2024/25', 2024, 2, 2),
    (N'dez24', N'2024/25', 2024, 3, NULL),
    (N'jan25', N'2024/25', 2025, 4, 3),
    (N'feb25', N'2024/25', 2025, 5, NULL),
    (N'mar25', N'2024/25', 2025, 6, 4),
    (N'apr25', N'2024/25', 2025, 7, NULL),
    (N'mai25', N'2024/25', 2025, 8, 5),
    (N'jun25', N'2024/25', 2025, 9, NULL),
    (N'2425', N'2024/25', NULL, NULL, NULL),
    (N'sep25', N'2025/26', 2025, 1, 1),
    (N'okt25', N'2025/26', 2025, NULL, NULL),
    (N'nov25', N'2025/26', 2025, 2, 2),
    (N'dez25', N'2025/26', 2025, 3, NULL),
    (N'jan26', N'2025/26', 2026, 4, 3),
    (N'feb26', N'2025/26', 2026, 5, NULL),
    (N'mar26', N'2025/26', 2026, 6, 4),
    (N'apr26', N'2025/26', 2026, 7, NULL),
    (N'mai26', N'2025/26', 2026, 8, 5),
    (N'jun26', N'2025/26', 2026, 9, NULL),
    (N'2526', N'2025/26', NULL, NULL, NULL)
) AS src(Ausgabenkuerzel, Schuljahr, Jahr, Ausgabe, AusgabeIDU)
ON tgt.Ausgabenkuerzel = src.Ausgabenkuerzel
WHEN MATCHED AND (
        ISNULL(tgt.Schuljahr,N'')  <> ISNULL(src.Schuljahr,N'')
     OR ISNULL(tgt.Jahr,-1)        <> ISNULL(src.Jahr,-1)
     OR ISNULL(tgt.Ausgabe,-1)     <> ISNULL(src.Ausgabe,-1)
     OR ISNULL(tgt.AusgabeIDU,-1)  <> ISNULL(src.AusgabeIDU,-1)
    ) THEN UPDATE SET
        Schuljahr   = src.Schuljahr,
        Jahr        = src.Jahr,
        Ausgabe     = src.Ausgabe,
        AusgabeIDU  = src.AusgabeIDU
WHEN NOT MATCHED THEN
    INSERT (Ausgabenkuerzel, Schuljahr, Jahr, Ausgabe, AusgabeIDU)
    VALUES (src.Ausgabenkuerzel, src.Schuljahr, src.Jahr, src.Ausgabe, src.AusgabeIDU);

SELECT COUNT(*) AS dim_ausgaben_rows FROM curated.dim_ausgaben;
