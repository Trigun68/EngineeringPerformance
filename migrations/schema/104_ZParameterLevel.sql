-- 104: Уровень редактирования Z-параметров на версии анализа
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.ProjectAnalysisVersions') AND name = N'ZParameterLevel')
BEGIN
    ALTER TABLE dbo.ProjectAnalysisVersions
        ADD ZParameterLevel INT NOT NULL
            CONSTRAINT DF_PAV_ZParameterLevel DEFAULT 4;
    PRINT 'Added ZParameterLevel';
END

COMMIT TRANSACTION;
PRINT '104_ZParameterLevel completed';
