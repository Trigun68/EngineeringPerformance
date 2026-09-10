-- Журнал применённых schema-скриптов для ep-db-init.
-- Не путать с dbo.__EFMigrationsHistory (EF Core).
SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID(N'dbo.SchemaMigrations', N'U') IS NULL
CREATE TABLE dbo.SchemaMigrations (
  ScriptName nvarchar(260) NOT NULL CONSTRAINT PK_SchemaMigrations PRIMARY KEY,
  AppliedAtUtc datetime2(7) NOT NULL CONSTRAINT DF_SchemaMigrations_AppliedAtUtc DEFAULT (SYSUTCDATETIME()),
  Checksum char(64) NULL
);

IF COL_LENGTH('dbo.SchemaMigrations','Checksum') IS NULL ALTER TABLE dbo.SchemaMigrations ADD Checksum char(64) NULL;
