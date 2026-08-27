-- =============================================================================
-- EPMS IIC norms effective from 2026-07-01
-- Performance = Median (or sqrt(Q1*Q3) when Median absent)
-- Estimate = ln(Performance)
-- StandardDeviation = exp((ln(Q3)-ln(Q1))/1.349)
-- Formula: Linear a=Performance, b=0; Deviation sigma = StdDev/Performance
-- Idempotent UPSERT: non-EPMS periods normalized to 2026-01-01..2026-06-30;
-- EPMS period EffectiveFrom=2026-07-01 (open).
-- =============================================================================
SET NOCOUNT ON;
BEGIN TRANSACTION;

DECLARE @EffectiveFrom DATE = '2026-07-01';
DECLARE @PriorFrom DATE = '2026-01-01';
DECLARE @EffectiveToPrior DATE = '2026-06-30';
DECLARE @WdId INT;
DECLARE @UnitId INT;
DECLARE @ChelHourId INT = (
    SELECT TOP 1 [Id] FROM [UnitsOfMeasurement]
    WHERE [Name] = N'час'
);

-- IIC-1: Performance=4.3000, Estimate=1.4586, StandardDeviation=3.8324
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 4.3000,
            [Estimate] = 1.4586,
            [StandardDeviation] = 3.8324,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":4.3000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.8912618098}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 4.3000, 1.4586, 3.8324,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":4.3000,"b":0}', 1, N'{"sigma":0.8912618098}'
        );
    END
END;

-- IIC-1-1: Performance=1.3000, Estimate=0.2624, StandardDeviation=7.6639
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 1.3000,
            [Estimate] = 0.2624,
            [StandardDeviation] = 7.6639,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":1.3000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":5.895325589}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 1.3000, 0.2624, 7.6639,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":1.3000,"b":0}', 1, N'{"sigma":5.895325589}'
        );
    END
END;

-- IIC-1-1-1: Performance=3.9000, Estimate=1.3610, StandardDeviation=5.6281
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 3.9000,
            [Estimate] = 1.3610,
            [StandardDeviation] = 5.6281,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":3.9000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":1.44309365}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 3.9000, 1.3610, 5.6281,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":3.9000,"b":0}', 1, N'{"sigma":1.44309365}'
        );
    END
END;

-- IIC-1-1-2: Performance=0.8000, Estimate=-0.2231, StandardDeviation=3.1989
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 0.8000,
            [Estimate] = -0.2231,
            [StandardDeviation] = 3.1989,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":0.8000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":3.998593207}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 0.8000, -0.2231, 3.1989,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":0.8000,"b":0}', 1, N'{"sigma":3.998593207}'
        );
    END
END;

-- IIC-1-2: Performance=2.6000, Estimate=0.9555, StandardDeviation=3.8208
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 2.6000,
            [Estimate] = 0.9555,
            [StandardDeviation] = 3.8208,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":2.6000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":1.469547686}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 2.6000, 0.9555, 3.8208,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":2.6000,"b":0}', 1, N'{"sigma":1.469547686}'
        );
    END
END;

-- IIC-1-2-2: Performance=6.4000, Estimate=1.8563, StandardDeviation=4.7362
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 6.4000,
            [Estimate] = 1.8563,
            [StandardDeviation] = 4.7362,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":6.4000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.7400343998}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 6.4000, 1.8563, 4.7362,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":6.4000,"b":0}', 1, N'{"sigma":0.7400343998}'
        );
    END
END;

-- IIC-1-2-3: Performance=1.8000, Estimate=0.5878, StandardDeviation=3.0135
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-3');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 1.8000,
            [Estimate] = 0.5878,
            [StandardDeviation] = 3.0135,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":1.8000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":1.674156981}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 1.8000, 0.5878, 3.0135,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":1.8000,"b":0}', 1, N'{"sigma":1.674156981}'
        );
    END
END;

-- IIC-1-3: Performance=4.2000, Estimate=1.4351, StandardDeviation=2.5693
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 4.2000,
            [Estimate] = 1.4351,
            [StandardDeviation] = 2.5693,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":4.2000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.6117399546}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 4.2000, 1.4351, 2.5693,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":4.2000,"b":0}', 1, N'{"sigma":0.6117399546}'
        );
    END
END;

-- IIC-2: Performance=12.0000, Estimate=2.4849, StandardDeviation=2.5757
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 12.0000,
            [Estimate] = 2.4849,
            [StandardDeviation] = 2.5757,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":12.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.2146378123}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 12.0000, 2.4849, 2.5757,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":12.0000,"b":0}', 1, N'{"sigma":0.2146378123}'
        );
    END
END;

-- IIC-2-1: Performance=11.5326, Estimate=2.4452, StandardDeviation=5.8582 (geo median sqrt(Q1*Q3))
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 11.5326,
            [Estimate] = 2.4452,
            [StandardDeviation] = 5.8582,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":11.5326,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.5079717964}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 11.5326, 2.4452, 5.8582,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":11.5326,"b":0}', 1, N'{"sigma":0.5079717964}'
        );
    END
END;

-- IIC-2-2: Performance=10.0000, Estimate=2.3026, StandardDeviation=2.1851
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 10.0000,
            [Estimate] = 2.3026,
            [StandardDeviation] = 2.1851,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":10.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.2185066924}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 10.0000, 2.3026, 2.1851,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":10.0000,"b":0}', 1, N'{"sigma":0.2185066924}'
        );
    END
END;

-- IIC-2-2-1: Performance=10.0000, Estimate=2.3026, StandardDeviation=2.3521
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 10.0000,
            [Estimate] = 2.3026,
            [StandardDeviation] = 2.3521,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":10.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.2352078163}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 10.0000, 2.3026, 2.3521,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":10.0000,"b":0}', 1, N'{"sigma":0.2352078163}'
        );
    END
END;

-- IIC-2-2-2: Performance=11.2250, Estimate=2.4181, StandardDeviation=2.5311 (geo median sqrt(Q1*Q3))
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 11.2250,
            [Estimate] = 2.4181,
            [StandardDeviation] = 2.5311,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":11.2250,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.2254897699}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 11.2250, 2.4181, 2.5311,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":11.2250,"b":0}', 1, N'{"sigma":0.2254897699}'
        );
    END
END;

-- IIC-3-1: Performance=29.0000, Estimate=3.3673, StandardDeviation=2.3083
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 29.0000,
            [Estimate] = 3.3673,
            [StandardDeviation] = 2.3083,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":29.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.07959722893}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 29.0000, 3.3673, 2.3083,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":29.0000,"b":0}', 1, N'{"sigma":0.07959722893}'
        );
    END
END;

-- IIC-3-1-1: Performance=21.5000, Estimate=3.0681, StandardDeviation=2.3046
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 21.5000,
            [Estimate] = 3.0681,
            [StandardDeviation] = 2.3046,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":21.5000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.1071886403}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 21.5000, 3.0681, 2.3046,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":21.5000,"b":0}', 1, N'{"sigma":0.1071886403}'
        );
    END
END;

-- IIC-3-1-2: Performance=28.0000, Estimate=3.3322, StandardDeviation=3.7829
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 28.0000,
            [Estimate] = 3.3322,
            [StandardDeviation] = 3.7829,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":28.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.1351044624}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 28.0000, 3.3322, 3.7829,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":28.0000,"b":0}', 1, N'{"sigma":0.1351044624}'
        );
    END
END;

-- IIC-3-2-1: Performance=0.7000, Estimate=-0.3567, StandardDeviation=3.7743
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 0.7000,
            [Estimate] = -0.3567,
            [StandardDeviation] = 3.7743,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":0.7000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":5.391847238}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 0.7000, -0.3567, 3.7743,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":0.7000,"b":0}', 1, N'{"sigma":5.391847238}'
        );
    END
END;

-- IIC-3-2-2: Performance=7.0000, Estimate=1.9459, StandardDeviation=6.3977
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 7.0000,
            [Estimate] = 1.9459,
            [StandardDeviation] = 6.3977,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":7.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.9139624418}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 7.0000, 1.9459, 6.3977,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":7.0000,"b":0}', 1, N'{"sigma":0.9139624418}'
        );
    END
END;

-- IIC-3-3: Performance=1.6000, Estimate=0.4700, StandardDeviation=4.0506
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-3');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 1.6000,
            [Estimate] = 0.4700,
            [StandardDeviation] = 4.0506,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":1.6000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":2.531626388}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 1.6000, 0.4700, 4.0506,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":1.6000,"b":0}', 1, N'{"sigma":2.531626388}'
        );
    END
END;

-- IIC-3-4-1: Performance=0.8000, Estimate=-0.2231, StandardDeviation=1.0000 (Q1=Q3 => StdDev=1)
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 0.8000,
            [Estimate] = -0.2231,
            [StandardDeviation] = 1.0000,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":0.8000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":1.25}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 0.8000, -0.2231, 1.0000,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":0.8000,"b":0}', 1, N'{"sigma":1.25}'
        );
    END
END;

-- IIC-3-4-2: Performance=0.6000, Estimate=-0.5108, StandardDeviation=4.6714
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 0.6000,
            [Estimate] = -0.5108,
            [StandardDeviation] = 4.6714,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":0.6000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":7.785745179}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 0.6000, -0.5108, 4.6714,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":0.6000,"b":0}', 1, N'{"sigma":7.785745179}'
        );
    END
END;

-- IIC-3-5-1: Performance=4.5000, Estimate=1.5041, StandardDeviation=1.7940
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 4.5000,
            [Estimate] = 1.5041,
            [StandardDeviation] = 1.7940,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":4.5000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.398677698}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 4.5000, 1.5041, 1.7940,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":4.5000,"b":0}', 1, N'{"sigma":0.398677698}'
        );
    END
END;

-- IIC-4: Performance=2.3000, Estimate=0.8329, StandardDeviation=2.1277
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 2.3000,
            [Estimate] = 0.8329,
            [StandardDeviation] = 2.1277,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":2.3000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.9250996322}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 2.3000, 0.8329, 2.1277,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":2.3000,"b":0}', 1, N'{"sigma":0.9250996322}'
        );
    END
END;

-- IIC-4-1: Performance=2.0000, Estimate=0.6931, StandardDeviation=2.3134
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 2.0000,
            [Estimate] = 0.6931,
            [StandardDeviation] = 2.3134,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":2.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":1.156675237}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 2.0000, 0.6931, 2.3134,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":2.0000,"b":0}', 1, N'{"sigma":1.156675237}'
        );
    END
END;

-- IIC-4-2: Performance=2.0712, Estimate=0.7281, StandardDeviation=1.9948 (geo median sqrt(Q1*Q3))
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 2.0712,
            [Estimate] = 0.7281,
            [StandardDeviation] = 1.9948,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":2.0712,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.9631089568}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 2.0712, 0.7281, 1.9948,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":2.0712,"b":0}', 1, N'{"sigma":0.9631089568}'
        );
    END
END;

-- IIC-4-3: Performance=2.1000, Estimate=0.7419, StandardDeviation=1.4030
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-3');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 2.1000,
            [Estimate] = 0.7419,
            [StandardDeviation] = 1.4030,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":2.1000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.6680802489}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 2.1000, 0.7419, 1.4030,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":2.1000,"b":0}', 1, N'{"sigma":0.6680802489}'
        );
    END
END;

-- IIC-5-1: Performance=23.0000, Estimate=3.1355, StandardDeviation=3.2972
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 23.0000,
            [Estimate] = 3.1355,
            [StandardDeviation] = 3.2972,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":23.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.1433545658}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 23.0000, 3.1355, 3.2972,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":23.0000,"b":0}', 1, N'{"sigma":0.1433545658}'
        );
    END
END;

-- IIC-5-2: Performance=11.0000, Estimate=2.3979, StandardDeviation=2.8461
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 11.0000,
            [Estimate] = 2.3979,
            [StandardDeviation] = 2.8461,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":11.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.2587365391}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 11.0000, 2.3979, 2.8461,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":11.0000,"b":0}', 1, N'{"sigma":0.2587365391}'
        );
    END
END;

-- IIC-5-3: Performance=10.3000, Estimate=2.3321, StandardDeviation=3.6571
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-3');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 10.3000,
            [Estimate] = 2.3321,
            [StandardDeviation] = 3.6571,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":10.3000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.3550559816}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 10.3000, 2.3321, 3.6571,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":10.3000,"b":0}', 1, N'{"sigma":0.3550559816}'
        );
    END
END;

-- IIC-6: Performance=127.0000, Estimate=4.8442, StandardDeviation=2.7031
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 127.0000,
            [Estimate] = 4.8442,
            [StandardDeviation] = 2.7031,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":127.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.02128423919}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 127.0000, 4.8442, 2.7031,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":127.0000,"b":0}', 1, N'{"sigma":0.02128423919}'
        );
    END
END;

-- IIC-6-1: Performance=87.0000, Estimate=4.4659, StandardDeviation=2.3410
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-1');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 87.0000,
            [Estimate] = 4.4659,
            [StandardDeviation] = 2.3410,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":87.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.02690749698}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 87.0000, 4.4659, 2.3410,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":87.0000,"b":0}', 1, N'{"sigma":0.02690749698}'
        );
    END
END;

-- IIC-6-2: Performance=62.2000, Estimate=4.1304, StandardDeviation=2.1514
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-2');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 62.2000,
            [Estimate] = 4.1304,
            [StandardDeviation] = 2.1514,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":62.2000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.0345878792}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 62.2000, 4.1304, 2.1514,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":62.2000,"b":0}', 1, N'{"sigma":0.0345878792}'
        );
    END
END;

-- IIC-6-3: Performance=67.4000, Estimate=4.2106, StandardDeviation=2.2606
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-3');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 67.4000,
            [Estimate] = 4.2106,
            [StandardDeviation] = 2.2606,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":67.4000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.03353976575}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 67.4000, 4.2106, 2.2606,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":67.4000,"b":0}', 1, N'{"sigma":0.03353976575}'
        );
    END
END;

-- IIC-6-6: Performance=304.0000, Estimate=5.7170, StandardDeviation=4.7985
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-6');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 304.0000,
            [Estimate] = 5.7170,
            [StandardDeviation] = 4.7985,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":304.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.01578449528}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 304.0000, 5.7170, 4.7985,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":304.0000,"b":0}', 1, N'{"sigma":0.01578449528}'
        );
    END
END;

-- IIC-6-7: Performance=78.0000, Estimate=4.3567, StandardDeviation=2.9451
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-7');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 78.0000,
            [Estimate] = 4.3567,
            [StandardDeviation] = 2.9451,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":78.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.03775791101}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 78.0000, 4.3567, 2.9451,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":78.0000,"b":0}', 1, N'{"sigma":0.03775791101}'
        );
    END
END;

-- IIC-6-10: Performance=174.0000, Estimate=5.1591, StandardDeviation=3.7563
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-10');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = @ChelHourId;

    -- Normalize previous (non-EPMS) periods to 2026-01-01 .. 2026-06-30
    UPDATE n
    SET n.[EffectiveFrom] = @PriorFrom,
        n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND CONVERT(date, n.[EffectiveFrom]) <> @EffectiveFrom;

    UPDATE n
    SET n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND n.[EffectiveFrom] < @EffectiveFrom
      AND (n.[EffectiveTo] IS NULL OR n.[EffectiveTo] >= @EffectiveFrom);

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 304.0000,
            [Estimate] = 5.7170,
            [StandardDeviation] = 4.7985,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":304.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.01578449528}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 304.0000, 5.7170, 4.7985,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":304.0000,"b":0}', 1, N'{"sigma":0.01578449528}'
        );
    END
END;

-- IIC-6-7: Performance=78.0000, Estimate=4.3567, StandardDeviation=2.9451
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-7');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = COALESCE(
        (SELECT [UnitId] FROM [IicWorkDirections] WHERE [Id] = @WdId),
        @ChelHourId
    );

    UPDATE n
    SET n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND n.[EffectiveFrom] < @EffectiveFrom
      AND (n.[EffectiveTo] IS NULL OR n.[EffectiveTo] >= @EffectiveFrom);

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 78.0000,
            [Estimate] = 4.3567,
            [StandardDeviation] = 2.9451,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":78.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.03775791101}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 78.0000, 4.3567, 2.9451,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":78.0000,"b":0}', 1, N'{"sigma":0.03775791101}'
        );
    END
END;

-- IIC-6-10: Performance=174.0000, Estimate=5.1591, StandardDeviation=3.7563
SET @WdId = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-10');
IF @WdId IS NOT NULL
BEGIN
    SET @UnitId = COALESCE(
        (SELECT [UnitId] FROM [IicWorkDirections] WHERE [Id] = @WdId),
        @ChelHourId
    );

    UPDATE n
    SET n.[EffectiveTo] = @EffectiveToPrior,
        n.[ModifiedAt] = SYSUTCDATETIME()
    FROM [IicWorkDirectionNorms] n
    WHERE n.[WorkDirectionId] = @WdId
      AND n.[EffectiveFrom] < @EffectiveFrom
      AND (n.[EffectiveTo] IS NULL OR n.[EffectiveTo] >= @EffectiveFrom);

    IF EXISTS (
        SELECT 1 FROM [IicWorkDirectionNorms]
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom
    )
    BEGIN
        UPDATE [IicWorkDirectionNorms]
        SET [Performance] = 174.0000,
            [Estimate] = 5.1591,
            [StandardDeviation] = 3.7563,
            [UnitId] = @UnitId,
            [EffectiveTo] = NULL,
            [FormulaType] = 1,
            [CoefficientsJson] = N'{"a":174.0000,"b":0}',
            [DeviationModelType] = 1,
            [DeviationCoefficientsJson] = N'{"sigma":0.0215881796}',
            [ModifiedAt] = SYSUTCDATETIME()
        WHERE [WorkDirectionId] = @WdId AND [EffectiveFrom] = @EffectiveFrom;
    END
    ELSE
    BEGIN
        INSERT INTO [IicWorkDirectionNorms] (
            [WorkDirectionId], [Performance], [Estimate], [StandardDeviation],
            [UnitId], [EffectiveFrom], [EffectiveTo], [CreatedAt],
            [FormulaType], [CoefficientsJson], [DeviationModelType], [DeviationCoefficientsJson]
        )
        VALUES (
            @WdId, 174.0000, 5.1591, 3.7563,
            @UnitId, @EffectiveFrom, NULL, SYSUTCDATETIME(),
            1, N'{"a":174.0000,"b":0}', 1, N'{"sigma":0.0215881796}'
        );
    END
END;

COMMIT TRANSACTION;
PRINT '022_IicNormsEpms20260701 completed';
GO
