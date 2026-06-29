-- ============================================================================
-- Engineering Performance — часть 2 из 2: ИМПОРТ ДАННЫХ
-- ============================================================================
-- Содержит: справочники (002), заглушка 003, LI-* (004), номенклатура ФО (005 из «Физ.объем»).
-- Проекты из 002 — без версий анализа и без ProjectUserAccess (голые карточки).
-- Запускайте из каталога migrations (текущая рабочая папка sqlcmd).
-- Выполняйте после успешного FullMigration_01_Structure.sql.
-- При sqlcmd без -I: включите ANSI_NULLS/QUOTED_IDENTIFIER.
-- ============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'';
PRINT N'>>> [Часть 2/2] Импорт данных';
PRINT N'';

:r data\002_SeedData.sql
:r data\003_SeedDataFromExcel.sql
:r data\004_LaborIntensity.sql
:r data\005_PhysicalVolumes.sql
:r data\008_ProjectCharacteristics.sql

PRINT N'';
PRINT N'>>> [Часть 2/2] Завершено.';
PRINT N'';
