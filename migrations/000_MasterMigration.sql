-- ============================================================================
-- Engineering Performance — полная миграция БД (2 части)
-- ============================================================================
-- Часть 1: структура (FullMigration_01_Structure.sql)
-- Часть 2: данные   (FullMigration_02_DataImport.sql)
--
-- Запуск из каталога migrations:
--   sqlcmd -S <сервер> -d <база> -i 000_MasterMigration.sql
--
-- Или по отдельности:
--   sqlcmd ... -i FullMigration_01_Structure.sql
--   sqlcmd ... -i FullMigration_02_DataImport.sql
--
-- Рекомендуется: sqlcmd ... -I -C  (-I = QUOTED_IDENTIFIER ON, -C = доверие к сертификату ODBC 18)
-- ============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'';
PRINT N'========================================';
PRINT N' Engineering Performance — полная миграция';
PRINT N'========================================';
PRINT N'';

:r FullMigration_01_Structure.sql
:r FullMigration_02_DataImport.sql

PRINT N'';
PRINT N'========================================';
PRINT N' Полная миграция завершена.';
PRINT N'========================================';
PRINT N'';
