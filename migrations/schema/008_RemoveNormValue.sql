-- Migration: 20260420213701_RemoveIicWorkDirectionNormNormValue
-- Remove NormValue column from IicWorkDirectionNorms table

IF EXISTS (
    SELECT 1
    FROM sys.columns
    WHERE object_id = OBJECT_ID(N'[IicWorkDirectionNorms]')
    AND name = 'NormValue'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[IicWorkDirectionNorms]') AND [c].[name] = N'NormValue');

    IF @var IS NOT NULL EXEC(N'ALTER TABLE [IicWorkDirectionNorms] DROP CONSTRAINT ' + @var + ';');

    ALTER TABLE [IicWorkDirectionNorms] DROP COLUMN [NormValue];

    PRINT 'Column NormValue dropped from IicWorkDirectionNorms';
END
ELSE
BEGIN
    PRINT 'Column NormValue does not exist in IicWorkDirectionNorms - skipping';
END
GO

-- Mark migration as applied
IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260420213701_RemoveIicWorkDirectionNormNormValue')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260420213701_RemoveIicWorkDirectionNormNormValue', N'10.0.0');
END
GO
