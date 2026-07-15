-- Параметры планирования (Z-сценарии) по группам физ. объёмов IIC для версии проекта
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ProjectVersionPlanningParameters')
BEGIN
    CREATE TABLE [ProjectVersionPlanningParameters] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ProjectAnalysisVersionId] INT NOT NULL,
        [IicWorkDirectionId] INT NOT NULL,
        [ZPessimistic] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_PVPP_ZPessimistic] DEFAULT (0.5),
        [ZBase] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_PVPP_ZBase] DEFAULT (0),
        [ZOptimistic] DECIMAL(18,4) NOT NULL CONSTRAINT [DF_PVPP_ZOptimistic] DEFAULT (-0.5),
        [ZActual] DECIMAL(18,4) NULL,
        CONSTRAINT [FK_PVPP_Version] FOREIGN KEY ([ProjectAnalysisVersionId])
            REFERENCES [ProjectAnalysisVersions]([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_PVPP_IicWorkDirection] FOREIGN KEY ([IicWorkDirectionId])
            REFERENCES [IicWorkDirections]([Id]),
        CONSTRAINT [UQ_PVPP_VersionDirection] UNIQUE ([ProjectAnalysisVersionId], [IicWorkDirectionId])
    );
END;
