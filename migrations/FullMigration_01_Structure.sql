-- ============================================================================
-- Engineering Performance — часть 1 из 2: СТРУКТУРА БД
-- ============================================================================
-- Содержит: начальную схему (EF Core) и накопительные изменения DDL.
-- Запускайте из каталога migrations (текущая рабочая папка sqlcmd).
-- При sqlcmd без -I: включите ANSI_NULLS/QUOTED_IDENTIFIER (иначе ALTER с индексами — ошибка 1934).
-- ============================================================================

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

PRINT N'';
PRINT N'>>> [Часть 1/2] Структура БД';
PRINT N'';

:r schema\001_InitialSchema.sql
:r schema\002_Consolidated_20260413-14.sql
:r schema\006_Consolidated_20260415.sql
:r schema\007_PhysicalVolumeCoefficient.sql
:r schema\009_DropIicWorkDirectionNormNormValue.sql
:r schema\010_AddLaborIntensity.sql
:r schema\011_AddProjectCharacteristics.sql
:r schema\012_DropProjectCharacteristicOptionAccuracy.sql
:r schema\013_AddProjectCharacteristicSelections.sql

PRINT N'';
PRINT N'>>> [Часть 1/2] Завершено.';
PRINT N'';
