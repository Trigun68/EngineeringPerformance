-- ============================================================================
-- CI / fresh-DB demo projects for Playwright E2E and API smoke.
-- Idempotent by Code. Production DBs that already have projects are untouched.
-- ============================================================================
SET NOCOUNT ON;
GO

DECLARE @KfoTypeId INT = (SELECT TOP (1) [Id] FROM [PhysicalVolumeTypes] WHERE [Name] = N'КФО' ORDER BY [Id]);
DECLARE @Now DATETIME2 = SYSUTCDATETIME();

IF NOT EXISTS (SELECT 1 FROM [Projects] WHERE [Code] = N'PRJ-CI-310')
BEGIN
    INSERT INTO [Projects] (
        [Code], [Name], [WorkNumber], [ContractNumber], [Customer],
        [IsArchived], [CreatedAt], [SortOrder]
    )
    VALUES (
        N'PRJ-CI-310', N'CI Demo Project A', N'W-310', N'C-310', N'CI Customer',
        0, @Now, 0
    );
END;

IF NOT EXISTS (SELECT 1 FROM [Projects] WHERE [Code] = N'PRJ-CI-970')
BEGIN
    INSERT INTO [Projects] (
        [Code], [Name], [WorkNumber], [ContractNumber], [Customer],
        [IsArchived], [CreatedAt], [SortOrder]
    )
    VALUES (
        N'PRJ-CI-970', N'CI Demo Project B', N'W-970', N'C-970', N'CI Customer',
        0, @Now, 1
    );
END;

DECLARE @ProjectA INT = (SELECT [Id] FROM [Projects] WHERE [Code] = N'PRJ-CI-310');
DECLARE @ProjectB INT = (SELECT [Id] FROM [Projects] WHERE [Code] = N'PRJ-CI-970');

IF @ProjectA IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM [ProjectAnalysisVersions]
       WHERE [ProjectId] = @ProjectA AND [VersionName] = N'UAT Baseline (план)'
   )
BEGIN
    INSERT INTO [ProjectAnalysisVersions] (
        [ProjectId], [VersionName], [Name], [Description], [Direction],
        [PhysicalVolumeTypeId], [IsArchived], [CreatedAt], [SortOrder]
    )
    VALUES (
        @ProjectA, N'UAT Baseline (план)', N'UAT Baseline (план)',
        N'CI seed plan version', 1, @KfoTypeId, 0, @Now, 0
    );
END;

IF @ProjectA IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM [ProjectAnalysisVersions]
       WHERE [ProjectId] = @ProjectA AND [VersionName] = N'UAT Baseline (факт)'
   )
BEGIN
    INSERT INTO [ProjectAnalysisVersions] (
        [ProjectId], [VersionName], [Name], [Description], [Direction],
        [PhysicalVolumeTypeId], [IsArchived], [CreatedAt], [SortOrder]
    )
    VALUES (
        @ProjectA, N'UAT Baseline (факт)', N'UAT Baseline (факт)',
        N'CI seed fact version', 2, @KfoTypeId, 0, @Now, 1
    );
END;

IF @ProjectB IS NOT NULL
   AND NOT EXISTS (
       SELECT 1 FROM [ProjectAnalysisVersions]
       WHERE [ProjectId] = @ProjectB AND [VersionName] = N'UAT Baseline (план)'
   )
BEGIN
    INSERT INTO [ProjectAnalysisVersions] (
        [ProjectId], [VersionName], [Name], [Description], [Direction],
        [PhysicalVolumeTypeId], [IsArchived], [CreatedAt], [SortOrder]
    )
    VALUES (
        @ProjectB, N'UAT Baseline (план)', N'UAT Baseline (план)',
        N'CI seed plan for project B', 1, @KfoTypeId, 0, @Now, 0
    );
END;
GO
