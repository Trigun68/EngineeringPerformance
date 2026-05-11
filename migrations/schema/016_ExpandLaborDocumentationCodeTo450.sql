-- =====================================================================
-- Migration 016: Expand ProjectLaborIntensities.DocumentationCode 100 -> 450
-- Date: 2026-05-11
-- Purpose: ISS-006 — короткая длина (100) схлопывала разные «тип+марка»
--          по truncate, теряя ~2 830 ч при импорте трудоёмкости проекта 4.
-- Связано: docs/issues/ISS-006-labor-import-truncate-loss.md
-- Безопасность: 450 nvarchar = 900 байт ≤ 1700 байт (лимит ключа SQL Server),
--               уникальный фильтрованный индекс пересоздавать не требуется.
-- =====================================================================

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE [object_id] = OBJECT_ID(N'[dbo].[ProjectLaborIntensities]')
      AND name = N'DocumentationCode'
      AND max_length = 200 -- nvarchar(100) хранится как 200 байт
)
BEGIN
    ALTER TABLE [dbo].[ProjectLaborIntensities]
        ALTER COLUMN [DocumentationCode] NVARCHAR(450) NOT NULL;
END
GO
