-- 116: снимок коэффициента / оценки / классификатора на строке ФО версии.
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF COL_LENGTH(N'dbo.ProjectPhysicalVolumes', N'CatalogCoefficient') IS NULL
BEGIN
    ALTER TABLE dbo.ProjectPhysicalVolumes ADD CatalogCoefficient DECIMAL(18,4) NULL;
    PRINT 'Added ProjectPhysicalVolumes.CatalogCoefficient';
END

IF COL_LENGTH(N'dbo.ProjectPhysicalVolumes', N'CatalogIsAssessment') IS NULL
BEGIN
    ALTER TABLE dbo.ProjectPhysicalVolumes ADD CatalogIsAssessment BIT NULL;
    PRINT 'Added ProjectPhysicalVolumes.CatalogIsAssessment';
END

IF COL_LENGTH(N'dbo.ProjectPhysicalVolumes', N'CatalogClassifierId') IS NULL
BEGIN
    ALTER TABLE dbo.ProjectPhysicalVolumes ADD CatalogClassifierId INT NULL;
    PRINT 'Added ProjectPhysicalVolumes.CatalogClassifierId';
END

-- UPDATE в том же батче, что ALTER, не компилируется — отложенный EXEC.
EXEC(N'
UPDATE ppv
SET
    CatalogCoefficient = CASE WHEN kfo.Coefficient <= 0 THEN 1 ELSE kfo.Coefficient END,
    CatalogIsAssessment = kfo.IsAssessment,
    CatalogClassifierId = kfo.ClassifierId
FROM dbo.ProjectPhysicalVolumes AS ppv
INNER JOIN dbo.PhysicalVolumesKfo AS kfo ON kfo.Id = ppv.PhysicalVolumeKfoId
WHERE ppv.CatalogCoefficient IS NULL AND ppv.PhysicalVolumeKfoId IS NOT NULL;
');

EXEC(N'
UPDATE ppv
SET
    CatalogCoefficient = CASE WHEN ber.Coefficient <= 0 THEN 1 ELSE ber.Coefficient END,
    CatalogIsAssessment = ber.IsAssessment,
    CatalogClassifierId = ber.ClassifierId
FROM dbo.ProjectPhysicalVolumes AS ppv
INNER JOIN dbo.PhysicalVolumesBer AS ber ON ber.Id = ppv.PhysicalVolumeBerId
WHERE ppv.CatalogCoefficient IS NULL AND ppv.PhysicalVolumeBerId IS NOT NULL;
');

COMMIT TRANSACTION;
PRINT '116_PpvCatalogSnapshot completed';
