-- 120: история сэмплов вкладки «Ресурсы» (90 суток, запись каждые 10 с)
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.ResourceMetricSamples', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.ResourceMetricSamples
    (
        TimestampUtc              DATETIME2(3)    NOT NULL,
        ActiveUsers               INT             NOT NULL,
        ActiveSessions            INT             NOT NULL,
        KestrelConnections        INT             NOT NULL,
        CurrentHttpRequests       INT             NOT NULL,
        SqlInteractive            INT             NOT NULL,
        SqlAnalytics              INT             NOT NULL,
        SqlOther                  INT             NOT NULL,
        RedisClients              INT             NOT NULL,
        AnalyticsGateBusy         INT             NOT NULL,
        AnalyticsGateLimit        INT             NOT NULL,
        BackendAvailable          BIT             NOT NULL,
        BackendCpuCores           FLOAT           NOT NULL,
        BackendRssMib             FLOAT           NOT NULL,
        RedisAvailable            BIT             NOT NULL,
        RedisStatus               NVARCHAR(200)   NULL,
        RedisCpuCores             FLOAT           NOT NULL,
        RedisRssMib               FLOAT           NOT NULL,
        SqlAvailable              BIT             NOT NULL,
        SqlStatus                 NVARCHAR(200)   NULL,
        SqlCpuCores               FLOAT           NOT NULL,
        SqlRssMib                 FLOAT           NOT NULL,
        GcHeapMib                 FLOAT           NOT NULL,
        AllocRateBytesPerSecond   FLOAT           NOT NULL,
        Gen2Collections           INT             NOT NULL,
        ThreadPoolBusy            INT             NOT NULL,
        ThreadPoolQueue           BIGINT          NOT NULL,
        SqlRunnable               INT             NOT NULL,
        RedisHitRatio             FLOAT           NOT NULL,
        RedisOpsPerSec            FLOAT           NOT NULL,
        RedisEvictedKeys          BIGINT          NOT NULL,
        RedisFragmentation        FLOAT           NOT NULL,
        SqlBatchRequestsPerSec    FLOAT           NOT NULL,
        SqlBufferCacheHitRatio    FLOAT           NOT NULL,
        CONSTRAINT PK_ResourceMetricSamples PRIMARY KEY CLUSTERED (TimestampUtc)
    );
END
GO
