-- 108: Corporate authentication foundation
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

UPDATE dbo.Roles SET Name = N'SuperAdmin' WHERE Id = 1 AND Name = N'Administrator';
UPDATE dbo.Roles SET Name = N'Admin' WHERE Id = 2 AND Name = N'ProjectManager';
UPDATE dbo.Roles SET Name = N'Engineer' WHERE Id = 3 AND Name = N'Observer';

IF NOT EXISTS (SELECT 1 FROM dbo.Roles WHERE Id = 4)
BEGIN
    SET IDENTITY_INSERT dbo.Roles ON;

    INSERT INTO dbo.Roles (Id, Name, Description, ParentId, IsArchived, CreatedAt, ModifiedAt)
    VALUES (4, N'Gip', N'Главный инженер проекта', NULL, 0, SYSUTCDATETIME(), NULL);

    SET IDENTITY_INSERT dbo.Roles OFF;
END;

IF COL_LENGTH(N'dbo.Users', N'FailedLoginAttempts') IS NULL
    ALTER TABLE dbo.Users ADD FailedLoginAttempts INT NOT NULL
        CONSTRAINT DF_Users_FailedLoginAttempts DEFAULT 0;

IF COL_LENGTH(N'dbo.Users', N'IsLockedOut') IS NULL
    ALTER TABLE dbo.Users ADD IsLockedOut BIT NOT NULL
        CONSTRAINT DF_Users_IsLockedOut DEFAULT 0;

IF COL_LENGTH(N'dbo.Users', N'LockoutEndsAt') IS NULL
    ALTER TABLE dbo.Users ADD LockoutEndsAt DATETIME2 NULL;

IF COL_LENGTH(N'dbo.Users', N'LastFailedLoginAt') IS NULL
    ALTER TABLE dbo.Users ADD LastFailedLoginAt DATETIME2 NULL;

IF COL_LENGTH(N'dbo.Users', N'MfaEnabled') IS NULL
    ALTER TABLE dbo.Users ADD MfaEnabled BIT NOT NULL
        CONSTRAINT DF_Users_MfaEnabled DEFAULT 0;

IF COL_LENGTH(N'dbo.Users', N'MfaSecret') IS NULL
    ALTER TABLE dbo.Users ADD MfaSecret NVARCHAR(1024) NULL;

IF COL_LENGTH(N'dbo.Users', N'MustChangePassword') IS NULL
    ALTER TABLE dbo.Users ADD MustChangePassword BIT NOT NULL
        CONSTRAINT DF_Users_MustChangePassword DEFAULT 0;

IF COL_LENGTH(N'dbo.Users', N'AdSyncedAt') IS NULL
    ALTER TABLE dbo.Users ADD AdSyncedAt DATETIME2 NULL;

IF COL_LENGTH(N'dbo.Users', N'AdGroups') IS NULL
    ALTER TABLE dbo.Users ADD AdGroups NVARCHAR(4000) NULL;

IF COL_LENGTH(N'dbo.Users', N'LastLoginAt') IS NULL
    ALTER TABLE dbo.Users ADD LastLoginAt DATETIME2 NULL;

IF OBJECT_ID(N'dbo.RefreshTokens', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.RefreshTokens
    (
        Id                INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY,
        UserId            INT NOT NULL,
        TokenHash         NVARCHAR(128) NOT NULL,
        CreatedAt         DATETIME2 NOT NULL,
        ExpiresAt         DATETIME2 NOT NULL,
        RevokedAt         DATETIME2 NULL,
        IpAddress         NVARCHAR(64) NULL,
        UserAgent         NVARCHAR(1024) NULL,
        ReplacedByTokenId INT NULL,
        RevocationReason  NVARCHAR(256) NULL,
        CONSTRAINT FK_RefreshTokens_Users_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.Users (Id) ON DELETE CASCADE,
        CONSTRAINT FK_RefreshTokens_RefreshTokens_ReplacedByTokenId FOREIGN KEY (ReplacedByTokenId)
            REFERENCES dbo.RefreshTokens (Id)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RefreshTokens') AND name = N'UX_RefreshTokens_TokenHash')
    CREATE UNIQUE INDEX UX_RefreshTokens_TokenHash ON dbo.RefreshTokens (TokenHash);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.RefreshTokens') AND name = N'IX_RefreshTokens_UserId_ExpiresAt')
    CREATE INDEX IX_RefreshTokens_UserId_ExpiresAt ON dbo.RefreshTokens (UserId, ExpiresAt);

IF OBJECT_ID(N'dbo.AuthAuditLogs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AuthAuditLogs
    (
        Id            BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_AuthAuditLogs PRIMARY KEY,
        Timestamp     DATETIME2 NOT NULL,
        UserId        INT NULL,
        Identifier    NVARCHAR(256) NULL,
        EventType     INT NOT NULL,
        Success       BIT NOT NULL,
        IpAddress     NVARCHAR(64) NULL,
        UserAgent     NVARCHAR(1024) NULL,
        CorrelationId NVARCHAR(128) NULL,
        Details       NVARCHAR(4000) NULL,
        CONSTRAINT FK_AuthAuditLogs_Users_UserId FOREIGN KEY (UserId)
            REFERENCES dbo.Users (Id) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuthAuditLogs') AND name = N'IX_AuthAuditLogs_Timestamp')
    CREATE INDEX IX_AuthAuditLogs_Timestamp ON dbo.AuthAuditLogs (Timestamp);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuthAuditLogs') AND name = N'IX_AuthAuditLogs_UserId_Timestamp')
    CREATE INDEX IX_AuthAuditLogs_UserId_Timestamp ON dbo.AuthAuditLogs (UserId, Timestamp);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID(N'dbo.AuthAuditLogs') AND name = N'IX_AuthAuditLogs_EventType_Timestamp')
    CREATE INDEX IX_AuthAuditLogs_EventType_Timestamp ON dbo.AuthAuditLogs (EventType, Timestamp);

INSERT INTO dbo.AccessRights (RoleId, ObjectName, AccessLevel)
SELECT 4, source.ObjectName, source.AccessLevel
FROM dbo.AccessRights source
WHERE source.RoleId = 3
  AND NOT EXISTS
  (
      SELECT 1
      FROM dbo.AccessRights target
      WHERE target.RoleId = 4 AND target.ObjectName = source.ObjectName
  );

UPDATE dbo.AccessRights
SET AccessLevel = 2
WHERE RoleId = 4
  AND ObjectName IN (N'Projects', N'Planning', N'Assessment', N'ProjectDataUpload')
  AND AccessLevel <> 2;

COMMIT;
GO
