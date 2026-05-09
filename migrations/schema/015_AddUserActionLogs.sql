-- =====================================================================
-- Migration 015: Add UserActionLogs
-- Date: 2026-05-09
-- Purpose: Журнал действий пользователей (метаданные).
-- Связано: ADR-002-user-action-journal.md
-- =====================================================================

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'UserActionLogs')
BEGIN
    CREATE TABLE [dbo].[UserActionLogs] (
        [Id]          INT             IDENTITY(1,1) NOT NULL,
        [UserId]      INT             NOT NULL,
        [Timestamp]   DATETIME2(7)    NOT NULL,
        [ActionType]  NVARCHAR(64)    NOT NULL,
        [EntityType]  NVARCHAR(100)   NULL,
        [EntityId]    INT             NULL,
        [Description] NVARCHAR(500)   NULL,
        [IpOrHost]    NVARCHAR(64)    NULL,
        CONSTRAINT [PK_UserActionLogs] PRIMARY KEY CLUSTERED ([Id] ASC),
        CONSTRAINT [FK_UserActionLogs_Users_UserId] FOREIGN KEY ([UserId])
            REFERENCES [dbo].[Users]([Id]) ON DELETE NO ACTION
    );

    CREATE INDEX [IX_UserActionLogs_UserId_Timestamp]
        ON [dbo].[UserActionLogs]([UserId] ASC, [Timestamp] ASC);
    CREATE INDEX [IX_UserActionLogs_ActionType]
        ON [dbo].[UserActionLogs]([ActionType] ASC);
    CREATE INDEX [IX_UserActionLogs_EntityType_EntityId]
        ON [dbo].[UserActionLogs]([EntityType] ASC, [EntityId] ASC);
END
GO
