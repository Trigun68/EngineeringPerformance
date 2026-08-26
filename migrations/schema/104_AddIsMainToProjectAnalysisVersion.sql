-- ============================================================================
-- Schema 104: ProjectAnalysisVersions.IsMain — основная версия проекта по направлению
-- (ровно одна основная Plan-версия и ровно одна основная Fact-версия, независимо друг от друга;
-- используется отчётом «план/факт» на вкладке «Отчёт»).
-- ============================================================================
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.ProjectAnalysisVersions', 'IsMain') IS NULL
    ALTER TABLE [ProjectAnalysisVersions] ADD [IsMain] bit NOT NULL CONSTRAINT [DF_ProjectAnalysisVersions_IsMain] DEFAULT (0);
GO

-- Частичный индекс не поддержан во всех редакциях единообразно для bit-фильтра, поэтому
-- уникальность «одна основная версия на направление» обеспечивается на уровне сервиса
-- (транзакция в ProjectVersionService.SetMainAsync), а не constraint'ом БД.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectAnalysisVersions_ProjectId_Direction_IsMain' AND object_id = OBJECT_ID(N'[dbo].[ProjectAnalysisVersions]'))
    CREATE INDEX [IX_ProjectAnalysisVersions_ProjectId_Direction_IsMain] ON [ProjectAnalysisVersions]([ProjectId], [Direction], [IsMain]);
GO
