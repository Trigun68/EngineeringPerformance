-- 107: Таблица метрик SQL-профилирования (внутренний monitoring)
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.SqlQueryMetrics', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SqlQueryMetrics
    (
        Id                    BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_SqlQueryMetrics PRIMARY KEY,
        TimestampUtc          DATETIME2 NOT NULL,
        Endpoint              NVARCHAR(512) NOT NULL,
        RequestPath           NVARCHAR(512) NULL,
        QueryType             NVARCHAR(16) NOT NULL,
        TablesInvolved        NVARCHAR(1000) NULL,
        DurationMs            DECIMAL(18,4) NOT NULL,
        RowsReturned          INT NULL,
        RowsAffected          INT NULL,
        QueryHash             NVARCHAR(64) NOT NULL,
        SqlPreview            NVARCHAR(2000) NULL,
        ParametersJson        NVARCHAR(4000) NULL,
        RequestParametersJson NVARCHAR(2000) NULL,
        RequestDurationMs     DECIMAL(18,4) NULL,
        CorrelationId         NVARCHAR(64) NULL
    );

    CREATE INDEX IX_SqlQueryMetrics_TimestampUtc
        ON dbo.SqlQueryMetrics (TimestampUtc);

    CREATE INDEX IX_SqlQueryMetrics_QueryHash_Timestamp
        ON dbo.SqlQueryMetrics (QueryHash, TimestampUtc);

    CREATE INDEX IX_SqlQueryMetrics_Endpoint_Timestamp
        ON dbo.SqlQueryMetrics (Endpoint, TimestampUtc);

    CREATE INDEX IX_SqlQueryMetrics_QueryType_Timestamp
        ON dbo.SqlQueryMetrics (QueryType, TimestampUtc);

    CREATE INDEX IX_SqlQueryMetrics_DurationMs
        ON dbo.SqlQueryMetrics (DurationMs);

    PRINT 'Created SqlQueryMetrics';
END
GO

PRINT '107_SqlQueryMetrics completed';
GO
