-- 118: журнал persist-пересчёта версии анализа.
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.VersionCalculationLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VersionCalculationLogs (
        Id BIGINT IDENTITY(1,1) NOT NULL,
        ProjectAnalysisVersionId INT NOT NULL,
        Utc DATETIME2 NOT NULL CONSTRAINT DF_VersionCalculationLogs_Utc DEFAULT (SYSUTCDATETIME()),
        [Trigger] NVARCHAR(64) NOT NULL,
        PlanningScenario INT NOT NULL,
        SnapshotRowCount INT NOT NULL CONSTRAINT DF_VersionCalculationLogs_Snapshot DEFAULT (0),
        PpvRowCount INT NOT NULL CONSTRAINT DF_VersionCalculationLogs_Ppv DEFAULT (0),
        EquipmentRowCount INT NOT NULL CONSTRAINT DF_VersionCalculationLogs_Eq DEFAULT (0),
        BaseHoursSum DECIMAL(18,4) NOT NULL CONSTRAINT DF_VersionCalculationLogs_Base DEFAULT (0),
        OptimisticHoursSum DECIMAL(18,4) NOT NULL CONSTRAINT DF_VersionCalculationLogs_Opt DEFAULT (0),
        PessimisticHoursSum DECIMAL(18,4) NOT NULL CONSTRAINT DF_VersionCalculationLogs_Pess DEFAULT (0),
        CONSTRAINT PK_VersionCalculationLogs PRIMARY KEY (Id),
        CONSTRAINT FK_VersionCalculationLogs_Versions FOREIGN KEY (ProjectAnalysisVersionId)
            REFERENCES dbo.ProjectAnalysisVersions (Id) ON DELETE CASCADE
    );

    CREATE INDEX IX_VersionCalculationLogs_Version_Utc
        ON dbo.VersionCalculationLogs (ProjectAnalysisVersionId, Utc);
    PRINT 'Created VersionCalculationLogs';
END

COMMIT TRANSACTION;
PRINT '118_VersionCalculationLogs completed';
