-- ============================================================================
-- CI / fresh-DB demo projects for Playwright E2E and API smoke.
-- Idempotent by Code. Versions are created ONLY together with a new project
-- (не дописываются при повторном прогоне на существующей БД).
-- ============================================================================
SET NOCOUNT ON;
GO

DECLARE @KfoTypeId INT = (SELECT TOP (1) [Id] FROM [PhysicalVolumeTypes] WHERE [Name] = N'КФО' ORDER BY [Id]);
DECLARE @Now DATETIME2 = SYSUTCDATETIME();
DECLARE @CreatedA BIT = 0;
DECLARE @CreatedB BIT = 0;

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
    SET @CreatedA = 1;
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
    SET @CreatedB = 1;
END;

DECLARE @ProjectA INT = (SELECT [Id] FROM [Projects] WHERE [Code] = N'PRJ-CI-310');
DECLARE @ProjectB INT = (SELECT [Id] FROM [Projects] WHERE [Code] = N'PRJ-CI-970');

-- Версии — только при первом создании проекта (не при update/re-seed).
IF @CreatedA = 1 AND @ProjectA IS NOT NULL
BEGIN
    INSERT INTO [ProjectAnalysisVersions] (
        [ProjectId], [VersionName], [Name], [Description], [Direction],
        [PhysicalVolumeTypeId], [IsArchived], [CreatedAt], [SortOrder]
    )
    VALUES (
        @ProjectA, N'UAT Baseline (план)', N'UAT Baseline (план)',
        N'CI seed plan version', 1, @KfoTypeId, 0, @Now, 0
    );

    INSERT INTO [ProjectAnalysisVersions] (
        [ProjectId], [VersionName], [Name], [Description], [Direction],
        [PhysicalVolumeTypeId], [IsArchived], [CreatedAt], [SortOrder]
    )
    VALUES (
        @ProjectA, N'UAT Baseline (факт)', N'UAT Baseline (факт)',
        N'CI seed fact version', 2, @KfoTypeId, 0, @Now, 1
    );
END;

IF @CreatedB = 1 AND @ProjectB IS NOT NULL
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
