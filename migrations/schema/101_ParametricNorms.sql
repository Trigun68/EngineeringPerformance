-- 101: Параметрические нормы ИИС (двойная миграция: формула Linear + σ как доля)
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IicWorkDirectionNorms') AND name = N'FormulaType')
BEGIN
    ALTER TABLE dbo.IicWorkDirectionNorms ADD FormulaType INT NOT NULL CONSTRAINT DF_IicNorms_FormulaType DEFAULT 1;
    PRINT 'Added FormulaType';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IicWorkDirectionNorms') AND name = N'CoefficientsJson')
BEGIN
    ALTER TABLE dbo.IicWorkDirectionNorms ADD CoefficientsJson NVARCHAR(MAX) NULL;
    PRINT 'Added CoefficientsJson';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IicWorkDirectionNorms') AND name = N'MinVolume')
BEGIN
    ALTER TABLE dbo.IicWorkDirectionNorms ADD MinVolume DECIMAL(18,4) NOT NULL CONSTRAINT DF_IicNorms_MinVolume DEFAULT 0;
    PRINT 'Added MinVolume';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IicWorkDirectionNorms') AND name = N'MaxVolume')
BEGIN
    ALTER TABLE dbo.IicWorkDirectionNorms ADD MaxVolume DECIMAL(18,4) NOT NULL CONSTRAINT DF_IicNorms_MaxVolume DEFAULT 999999;
    PRINT 'Added MaxVolume';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IicWorkDirectionNorms') AND name = N'ExtrapolationMode')
BEGIN
    ALTER TABLE dbo.IicWorkDirectionNorms ADD ExtrapolationMode INT NOT NULL CONSTRAINT DF_IicNorms_Extrapolation DEFAULT 2;
    PRINT 'Added ExtrapolationMode';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IicWorkDirectionNorms') AND name = N'DeviationModelType')
BEGIN
    ALTER TABLE dbo.IicWorkDirectionNorms ADD DeviationModelType INT NOT NULL CONSTRAINT DF_IicNorms_DeviationModel DEFAULT 1;
    PRINT 'Added DeviationModelType';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IicWorkDirectionNorms') AND name = N'DeviationCoefficientsJson')
BEGIN
    ALTER TABLE dbo.IicWorkDirectionNorms ADD DeviationCoefficientsJson NVARCHAR(MAX) NULL;
    PRINT 'Added DeviationCoefficientsJson';
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID(N'dbo.IicWorkDirectionNorms') AND name = N'MigratedAt')
BEGIN
    ALTER TABLE dbo.IicWorkDirectionNorms ADD MigratedAt DATETIME2 NULL;
    PRINT 'Added MigratedAt';
END
GO

-- Фаза 1: Linear a=Performance, b=0
UPDATE dbo.IicWorkDirectionNorms
SET
    FormulaType = 1,
    CoefficientsJson = N'{"a":' + CAST(ISNULL(Performance, 0) AS NVARCHAR(50)) + N',"b":0}',
    MinVolume = 0,
    MaxVolume = 999999,
    ExtrapolationMode = 2,
    MigratedAt = SYSUTCDATETIME()
WHERE CoefficientsJson IS NULL;
GO

-- Фаза 2: σ = StdDev/Performance (мультипликативная модель)
UPDATE dbo.IicWorkDirectionNorms
SET
    DeviationModelType = 1,
    DeviationCoefficientsJson = N'{"sigma":' +
        CAST(CASE WHEN ISNULL(Performance, 0) > 0
                  THEN ISNULL(StandardDeviation, 0) / Performance
                  ELSE 0 END AS NVARCHAR(50)) + N'}'
WHERE DeviationCoefficientsJson IS NULL;
GO

PRINT '101_ParametricNorms completed';
GO
