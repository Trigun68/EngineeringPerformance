-- 024: ключ импорта TF — DocumentTypeName; в сиде 004 заполнен только Name.
SET NOCOUNT ON;

IF COL_LENGTH(N'dbo.LaborIntensities', N'DocumentTypeName') IS NOT NULL
BEGIN
    UPDATE [LaborIntensities]
    SET [DocumentTypeName] = [Name]
    WHERE [DocumentTypeName] IS NULL
      AND [Name] IS NOT NULL
      AND LTRIM(RTRIM([Name])) <> N'';
END;

PRINT '024_BackfillLaborIntensityDocumentTypeName completed';
