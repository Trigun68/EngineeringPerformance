-- 114: каноническое написание типа физобъёмов «БЕР» (вместо «БЭР»).
SET NOCOUNT ON;

IF EXISTS (SELECT 1 FROM sys.tables WHERE name = N'PhysicalVolumeTypes')
BEGIN
    UPDATE dbo.PhysicalVolumeTypes
    SET [Name] = N'БЕР'
    WHERE [Name] = N'БЭР';
END;

PRINT '114_RenameBerTypeSpelling completed';
