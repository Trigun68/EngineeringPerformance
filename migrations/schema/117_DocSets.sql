-- 117: справочник DocSet + FK на трудоёмкость версии; backfill из DocumentationCode после '|'.
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.DocSets', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.DocSets (
        Id INT IDENTITY(1,1) NOT NULL,
        Name NVARCHAR(200) NOT NULL,
        Code NVARCHAR(100) NOT NULL,
        ParentId INT NULL,
        SortOrder INT NOT NULL CONSTRAINT DF_DocSets_SortOrder DEFAULT (0),
        IsArchived BIT NOT NULL CONSTRAINT DF_DocSets_IsArchived DEFAULT (0),
        CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_DocSets_CreatedAt DEFAULT (SYSUTCDATETIME()),
        ModifiedAt DATETIME2 NULL,
        CONSTRAINT PK_DocSets PRIMARY KEY (Id),
        CONSTRAINT UQ_DocSets_Code UNIQUE (Code)
    );
    PRINT 'Created DocSets';
END

IF COL_LENGTH(N'dbo.ProjectLaborIntensities', N'DocSetId') IS NULL
BEGIN
    ALTER TABLE dbo.ProjectLaborIntensities ADD DocSetId INT NULL;
    PRINT 'Added ProjectLaborIntensities.DocSetId';
END

IF NOT EXISTS (
    SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ProjectLaborIntensities_DocSets'
)
BEGIN
    EXEC(N'
    ALTER TABLE dbo.ProjectLaborIntensities
        ADD CONSTRAINT FK_ProjectLaborIntensities_DocSets
        FOREIGN KEY (DocSetId) REFERENCES dbo.DocSets (Id);
    ');
END

EXEC(N'
;WITH codes AS (
    SELECT DISTINCT
        LTRIM(RTRIM(SUBSTRING(li.DocumentationCode, CHARINDEX(''|'', li.DocumentationCode) + 1, 450))) AS Code
    FROM dbo.ProjectLaborIntensities AS li
    WHERE CHARINDEX(''|'', li.DocumentationCode) > 0
      AND LEN(LTRIM(RTRIM(SUBSTRING(li.DocumentationCode, CHARINDEX(''|'', li.DocumentationCode) + 1, 450)))) > 0
)
INSERT INTO dbo.DocSets (Name, Code, SortOrder, IsArchived, CreatedAt)
SELECT c.Code, c.Code, 0, 0, SYSUTCDATETIME()
FROM codes AS c
WHERE NOT EXISTS (SELECT 1 FROM dbo.DocSets AS d WHERE d.Code = c.Code);
');

EXEC(N'
UPDATE li
SET DocSetId = d.Id
FROM dbo.ProjectLaborIntensities AS li
INNER JOIN dbo.DocSets AS d
    ON d.Code = LTRIM(RTRIM(SUBSTRING(li.DocumentationCode, CHARINDEX(''|'', li.DocumentationCode) + 1, 450)))
WHERE li.DocSetId IS NULL
  AND CHARINDEX(''|'', li.DocumentationCode) > 0
  AND LEN(LTRIM(RTRIM(SUBSTRING(li.DocumentationCode, CHARINDEX(''|'', li.DocumentationCode) + 1, 450)))) > 0;
');

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes WHERE name = N'IX_ProjectLaborIntensities_DocSetId' AND object_id = OBJECT_ID(N'dbo.ProjectLaborIntensities')
)
BEGIN
    EXEC(N'CREATE INDEX IX_ProjectLaborIntensities_DocSetId ON dbo.ProjectLaborIntensities (DocSetId);');
END

COMMIT TRANSACTION;
PRINT '117_DocSets completed';
