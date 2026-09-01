-- 119: optimistic concurrency на версии анализа.
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.ProjectAnalysisVersions', N'RowVersion') IS NULL
BEGIN
    ALTER TABLE dbo.ProjectAnalysisVersions ADD [RowVersion] rowversion NOT NULL;
    PRINT 'Added ProjectAnalysisVersions.RowVersion';
END

COMMIT TRANSACTION;
PRINT '119_ProjectAnalysisVersionRowVersion completed';
