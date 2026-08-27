-- 113: Плановые часы на строках оборудования (тот же набор колонок, что у ФО).
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.ProjectEquipments', N'LaborIntensityBase') IS NULL
BEGIN
    ALTER TABLE dbo.ProjectEquipments ADD LaborIntensityBase DECIMAL(18,4) NULL;
    PRINT 'Added ProjectEquipments.LaborIntensityBase';
END

IF COL_LENGTH(N'dbo.ProjectEquipments', N'LaborIntensityPessimistic') IS NULL
BEGIN
    ALTER TABLE dbo.ProjectEquipments ADD LaborIntensityPessimistic DECIMAL(18,4) NULL;
    PRINT 'Added ProjectEquipments.LaborIntensityPessimistic';
END

IF COL_LENGTH(N'dbo.ProjectEquipments', N'LaborIntensityOptimistic') IS NULL
BEGIN
    ALTER TABLE dbo.ProjectEquipments ADD LaborIntensityOptimistic DECIMAL(18,4) NULL;
    PRINT 'Added ProjectEquipments.LaborIntensityOptimistic';
END

COMMIT TRANSACTION;
PRINT '113_EquipmentLaborIntensity completed';
