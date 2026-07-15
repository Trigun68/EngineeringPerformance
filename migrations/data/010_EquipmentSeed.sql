-- ============================================================================
-- 010: Демо-номенклатура оборудования (idempotent)
-- ============================================================================
SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Equipments', N'U') IS NULL
    RETURN;
GO

IF NOT EXISTS (SELECT 1 FROM [Equipments])
BEGIN
    DECLARE @UnitId INT = (SELECT TOP 1 [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт' ORDER BY [Id]);
    DECLARE @ClassifierId INT = (
        SELECT TOP 1 [Id] FROM [IicWorkDirections]
        WHERE [ParentId] IS NOT NULL AND [IsArchived] = 0
        ORDER BY [SortOrder], [Id]
    );

    INSERT INTO [Equipments] (
        [Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment],
        [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash]
    )
    VALUES
        (
            N'Насос центробежный', N'Демо: насосное оборудование', @ClassifierId, @UnitId, 1, 1, 0, 0, SYSUTCDATETIME(),
            LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный', CHAR(0), COALESCE(CAST(@UnitId AS nvarchar(20)), N''))), 2))
        ),
        (
            N'Теплообменник', N'Демо: теплообменное оборудование', @ClassifierId, @UnitId, 1, 1, 1, 0, SYSUTCDATETIME(),
            LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Теплообменник', CHAR(0), COALESCE(CAST(@UnitId AS nvarchar(20)), N''))), 2))
        ),
        (
            N'Резервуар вертикальный', N'Демо: ёмкостное оборудование', @ClassifierId, @UnitId, 1, 0, 2, 0, SYSUTCDATETIME(),
            LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Резервуар вертикальный', CHAR(0), COALESCE(CAST(@UnitId AS nvarchar(20)), N''))), 2))
        );
END
GO
