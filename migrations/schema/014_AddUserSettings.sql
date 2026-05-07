-- 014_AddUserSettings.sql
CREATE TABLE [UserSettings] (
    [Id]     INT            NOT NULL IDENTITY,
    [UserId] INT            NOT NULL,
    [Key]    NVARCHAR(200)  NOT NULL,
    [Value]  NVARCHAR(MAX)  NOT NULL,
    CONSTRAINT [PK_UserSettings] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_UserSettings_Users_UserId]
        FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_UserSettings_UserId_Key] ON [UserSettings] ([UserId], [Key]);
CREATE INDEX [IX_UserSettings_UserId] ON [UserSettings] ([UserId]);
