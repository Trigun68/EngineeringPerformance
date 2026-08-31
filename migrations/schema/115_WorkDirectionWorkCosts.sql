-- 115: регистр стоимости работ по группам IIC (₽/ч, периоды как нормы).
SET NOCOUNT ON;

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'IicWorkDirectionWorkCosts')
BEGIN
    CREATE TABLE [IicWorkDirectionWorkCosts] (
        [Id] INT IDENTITY(1,1) NOT NULL,
        [WorkDirectionId] INT NOT NULL,
        [RatePerHour] DECIMAL(18,4) NOT NULL,
        [EffectiveFrom] DATETIME2 NOT NULL,
        [EffectiveTo] DATETIME2 NULL,
        [CreatedAt] DATETIME2 NOT NULL CONSTRAINT [DF_IicWorkDirectionWorkCosts_CreatedAt] DEFAULT (SYSUTCDATETIME()),
        [ModifiedAt] DATETIME2 NULL,
        CONSTRAINT [PK_IicWorkDirectionWorkCosts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_IicWorkDirectionWorkCosts_WorkDirection]
            FOREIGN KEY ([WorkDirectionId]) REFERENCES [IicWorkDirections]([Id]) ON DELETE CASCADE,
        CONSTRAINT [CK_IicWorkDirectionWorkCosts_EffectiveRange]
            CHECK ([EffectiveTo] IS NULL OR [EffectiveTo] > [EffectiveFrom]),
        CONSTRAINT [CK_IicWorkDirectionWorkCosts_RateNonNegative]
            CHECK ([RatePerHour] >= 0)
    );

    CREATE UNIQUE INDEX [UX_IicWorkDirectionWorkCosts_WorkDirection_EffectiveFrom]
        ON [IicWorkDirectionWorkCosts] ([WorkDirectionId], [EffectiveFrom] DESC)
        INCLUDE ([EffectiveTo], [RatePerHour]);
END;

-- Admin = Edit (2), прочие не-SuperAdmin роли = Read (1). SuperAdmin — bypass.
INSERT INTO [AccessRights] ([RoleId], [ObjectName], [AccessLevel])
SELECT r.[Id], N'EngineeringWorkCosts', CASE WHEN r.[Name] = N'Admin' THEN 2 ELSE 1 END
FROM [Roles] r
WHERE r.[Name] NOT IN (N'SuperAdmin', N'Administrator')
  AND NOT EXISTS (
      SELECT 1 FROM [AccessRights] ar
      WHERE ar.[RoleId] = r.[Id] AND ar.[ObjectName] = N'EngineeringWorkCosts'
  );

PRINT '115_WorkDirectionWorkCosts completed';
