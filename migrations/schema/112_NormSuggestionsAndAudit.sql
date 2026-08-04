-- 112: NormSuggestions + NormChangeAudits для анализа факта и норм
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.NormSuggestions', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NormSuggestions
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NormSuggestions PRIMARY KEY,
        NormId INT NOT NULL,
        SuggestionType INT NOT NULL,
        Status INT NOT NULL CONSTRAINT DF_NormSuggestions_Status DEFAULT (1),
        ProposedFormulaType NVARCHAR(64) NOT NULL,
        ProposedParametersJson NVARCHAR(MAX) NOT NULL,
        ProposedStdDeviation FLOAT NOT NULL,
        ProposedMeanPerformance FLOAT NOT NULL,
        CurrentR2 FLOAT NOT NULL,
        ProposedR2 FLOAT NOT NULL,
        DeltaR2 FLOAT NOT NULL,
        Confidence INT NOT NULL,
        DataPointsCount INT NOT NULL,
        PeriodStart DATETIME2 NOT NULL,
        PeriodEnd DATETIME2 NOT NULL,
        ProjectFilterJson NVARCHAR(4000) NULL,
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_NormSuggestions_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_NormSuggestions_Norm FOREIGN KEY (NormId)
            REFERENCES dbo.IicWorkDirectionNorms (Id)
    );

    CREATE INDEX IX_NormSuggestions_NormId_Status ON dbo.NormSuggestions (NormId, Status);
    CREATE INDEX IX_NormSuggestions_Period ON dbo.NormSuggestions (PeriodStart, PeriodEnd);
    PRINT 'Created NormSuggestions';
END
GO

IF OBJECT_ID(N'dbo.NormChangeAudits', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.NormChangeAudits
    (
        Id BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_NormChangeAudits PRIMARY KEY,
        NormId INT NOT NULL,
        Action INT NOT NULL,
        ChangedBy NVARCHAR(256) NOT NULL,
        OldValueJson NVARCHAR(MAX) NULL,
        NewValueJson NVARCHAR(MAX) NULL,
        SourceSuggestionId INT NULL,
        Timestamp DATETIME2 NOT NULL CONSTRAINT DF_NormChangeAudits_Timestamp DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_NormChangeAudits_Norm FOREIGN KEY (NormId)
            REFERENCES dbo.IicWorkDirectionNorms (Id)
    );

    CREATE INDEX IX_NormChangeAudits_NormId_Timestamp ON dbo.NormChangeAudits (NormId, Timestamp);
    PRINT 'Created NormChangeAudits';
END
GO

PRINT '112_NormSuggestionsAndAudit completed';
GO
