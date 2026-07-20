-- ============================================================================
-- Номенклатура физических объёмов КФО — лист «Спр_Номенклатура ФО»
-- Источник: _Расчет_трудоемкости_2_версии.xl.xlsx
-- Иерархия: «0» → «KQ.xx» → «KQ.xx.yyy»; ClassifierId ← «Классификатор КИМ».
-- ============================================================================

-- Единицы измерения из листа «Спр_Номенклатура ФО»
IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 врезка')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 врезка', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 жила')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 жила', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 км линии (3 провода) при 10 опорах')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 км линии (3 провода) при 10 опорах', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 км троса')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 км троса', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 колодец')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 колодец', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 компл. (3 фазы)')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 компл. (3 фазы)', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 лифт')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 лифт', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 м заземляющих устройств')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 м заземляющих устройств', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 м закрепленной скважины')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 м закрепленной скважины', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 м3 сборных железобетонных конструкций')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 м3 сборных железобетонных конструкций', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 опора')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 опора', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 опора (2 провода)')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 опора (2 провода)', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'1 переезд')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'1 переезд', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'км')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'км', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'компл.')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'компл.', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'м', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м (3 фазы)')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'м (3 фазы)', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м бурения скважин')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'м бурения скважин', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м ступеней')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'м ступеней', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м шва')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'м шва', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м2 облицовки')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'м2 облицовки', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м2 поверхности')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'м2 поверхности', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м3 глинистого раствора')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'м3 глинистого раствора', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м²')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'м²', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'м³')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'м³', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'опора')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'опора', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'отверстие')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'отверстие', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'п.м.')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'п.м.', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'пара')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'пара', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'см сварного шва')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'см сварного шва', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'снимок')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'снимок', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'соединение')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'соединение', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'стык')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'стык', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'т')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'т', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'т свай')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'т свай', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'узел')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'узел', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'шкаф')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'шкаф', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'шт.', N'', 1000, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'0')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'0', N'Справочник Новый горизонт', N'', NULL, NULL, NULL, 0, 0, 1, 0, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.01')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.01', N'Расчистка территории', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 1, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02', N'Земляные работы', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 2, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.03')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.03', N'Сваи', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'), NULL, 0, 0, 1, 3, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.04')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.04', N'Железобетонные конструкции', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3'), NULL, 0, 0, 1, 4, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.05', N'Сборные ЖБ конструкции', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1'), NULL, 0, 0, 1, 5, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06', N'Монтаж металлоконструкций', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1'), NULL, 0, 0, 1, 6, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.07')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.07', N'Ограждающие конструкции', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), NULL, 0, 0, 1, 7, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.08')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.08', N'Устройство кровли', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 8, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09', N'Отделочные работы', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3'), NULL, 0, 0, 1, 9, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.10')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.10', N'Монтаж технологического оборудования', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 10, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.11')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.11', N'Монтаж технологического оборудования', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 11, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.12', N'АКЗ и ОГЗ', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 12, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.13', N'Теплоизоляция трубопроводов', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 13, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.14', N'Монтаж ОВК трубопроводов', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), NULL, 0, 0, 1, 14, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15', N'Монтаж технологических трубопроводов', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), NULL, 0, 0, 1, 15, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.16', N'Монтаж сетей вентиляции и кондиционирования', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 16, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17', N'Монтаж слаботочных сетей', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'), NULL, 0, 0, 1, 17, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18', N'Монтаж силового кабеля и провода', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'), NULL, 0, 0, 1, 18, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19', N'Монтаж электросилового оборудования', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), NULL, 0, 0, 1, 19, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20', N'Дороги и благоустройство', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 20, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.21', N'Кладка доменных печей и агрегатов', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3'), NULL, 0, 0, 1, 21, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22', N'Ремонтные и демонтажные работы', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 22, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.23', N'Ремонтные и демонтажные работы', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'), NULL, 0, 0, 1, 23, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.24', N'Ремонтные и демонтажные работы', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), NULL, 0, 0, 1, 24, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.25', N'Устройство ЖД путей', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 25, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.26')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.26', N'Горновскрышные работы', N'', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'0'), NULL, NULL, 0, 0, 1, 26, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.01.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.01.100', N'Комплекс работ по валке и вывозу деревьев', N'01. Валка деревьев с корня.02. Трелевка древесины на расстояние до 300 м тракторами мощностью 59 кВт (80 л.с.).03. Разделка древесины, полученной от валки леса.04. Устройство разделочных площадок.05. Корчевка пней в грунтах естественного залегания корчевателями-собирателями на тракторе мощностью 79 кВт (108 л.с.) с перемещением пней до 5 м.06. Обивка земли с выкорчеванных пней корчевателями-собирателями на тракторе мощностью 79 кВт (108 л.с.).07. Засыпка ям подкоренных бульдозерами мощностью 79 кВт (108 л.с.) с привозом грунта.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.01'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 27, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.01.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.01.110', N'Расчистка кустарников и мелколесья', N'01. Срезка кустарника и мелколесья.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.01'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 28, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.100', N'Срезка плодородного слоя', N'01. Срезка растительного слоя с перемещением бульдозером (включая добавку на перемещение грунта).02. Разработка грунта экскаваторами с погрузкой на автомобили-самосвалы.03. Перевозка грунта автомобилями-самосвалами на расстояние до 3 км.04. Работа на отвале.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 29, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.110', N'Разработка грунта', N'01. Разработка грунта в отвал.02. Устройство и содержание водоотводных канав или ограждающих валиков.03. Вспомогательные работы, связанные с перемещением экскаватора из забоя в забой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 30, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.120', N'Обратная засыпка', N'01. Перемещение грунта с засыпкой траншей и котлованов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 31, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.130', N'Устройство насыпей', N'01. Разработка грунта с перемещением его из резервов и выемок в насыпь.02. Разравнивание грунта в насыпи.03. Разравнивание грунта в резервах.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 32, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.140', N'Уплотнение грунта', N'01. Разравнивание грунта перед уплотнением.02. Уплотнение грунта.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 33, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.150', N'Планировка площадей и откосов', N'01. Предварительная (грубая) планировка площадей со срезкой неровностей грунта и засыпка впадин.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 34, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.160', N'Укрепление откосов', N'01. Планировка укрепляемых поверхностей.02. Устройство щебеночного (гравийного) основания и песчаной подготовки.03. Укладка плит.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 35, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.170', N'Разработка грунта вручную', N'01. Разработка грунта в траншеях и котлованах с выбрасыванием на бровку.02. Зачистка дна и поверхности стенок.03. Откидка грунта от бровки.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 36, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.180', N'Обратная засыпка вручную', N'01. Засыпка ранее выброшенным грунтом с разбивкой комьев и трамбованием.02. Поливка водой при необходимости.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 37, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.190')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.190', N'Планировка площадей и откосов вручную', N'01. Планировка поверхности со срезкой неровностей.02. Засыпка углублений, уплотнение грунта, зачистка поверхности и проверка шаблоном.03. Планировка основной площадки полотна.04. Устройство сливной призмы и зачистка неровностей.05. Планировка откосов срезкой.06. Разравнивание грунта и планировка естественной бермы.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 38, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.200')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.200', N'Разработка скального грунта (в т.ч. мерзлового)', N'01. Рыхление мерзлого грунта клин-молотом с передвижкой экскаватора в забое и в пределах разработки.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 39, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.210')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.210', N'Уплотнение грунта под полы промышленных цехов', N'01. Разравнивание грунта перед уплотнением.02. Уплотнение грунта.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 40, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.220')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.220', N'Бурение скважин', N'01. Бурение скважин с прямой промывкой.02. Наращивание бурильных труб.03. Спуск и подъем бурового снаряда.04. Смена породоразрушающего инструмента (долота, коронки).05. Подготовительно-заключительные работы, связанные с подъемом и спуском бурового снаряда.06. Приготовление глинистого раствора для забурки скважин и в процессе бурения.07. Чистка желобов и отстойников циркуляционной системы.08. Контроль за параметрами глинистого раствора.09. Оформление документации, отбор шлама и другие работы, способствующие нормальному бурению скважин.10. Обслуживание бурового оборудования и внутрипостроечного транспорта.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м бурения скважин'), 0, 0, 1.0000, 41, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.230')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.230', N'Монтаж обсадной трубы', N'01. Проработка скважины под обсадную колонну.02. Подбор труб.03. Проверка резьбы: калибровка и навинчивание труб, подварка стыков и приварка направляющих на обсадные трубы.04. Постановка и снятие хомутов.05. Посадка колонны в незакрепленную часть скважины.06. Обслуживание внутрипостроечного транспорта.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 м закрепленной скважины'), 0, 0, 1.0000, 42, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.240')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.240', N'Погрузочно-разгрузочные работы при а/м перевозках', N'01. Подъезд (отъезд) автотранспорта к месту погрузки (выгрузки) груза.02. Крепление и раскрепление грузов, укладка и снятие прокладок между рядами груза, очистка подвижного состава (кроме дополнительной обработки и промывки).03. Подноска (подвозка) к месту работы и относка (отвозка) к месту хранения инструмента, инвентаря и простейших приспособлений (тележек, тачек, мостиков, лестниц, стеллажей), реквизита (стоек, подкладок, прокладок), брезента для укрытия груза на расстояние до 50 м.04. Подноска порожних поддонов к месту погрузки (выгрузки) в пределах фронта работ, очистка снега с контейнеров перед погрузкой груза.05. Экипировка погрузо-разгрузочных машин (снабжение водой, смазочными материалами, топливом).06. Смена грузозахватных приспособлений на механизмах (кроме грейферов).07. Кантовка катно-бочковых грузов, установка на упоры и крепление на месте складирования.08. Разравнивание навалочных грузов в подвижном составе.09. Уборка рабочего места после окончания работы, очистка габарита пути при разгрузке навалочных грузов.10. Укладка тарно-штучных грузов на высоту не выше 1,6 м.11. Перемещение грузов с применением простейших приспособлений или вручную по горизонтали на расстояние, указанное в тех.части.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 43, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.250')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.250', N'Погрузочно-разгрузочные работы вручную при а/м перевозках', N'01. Погрузка вручную.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 44, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.260')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.260', N'Перевозка грузов самосвалами', N'01. Перевозка грузов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 45, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.270')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.270', N'Перевозка грузов бортовыми а/м', N'01. Перевозка грузов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 46, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02.280')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.02.280', N'Прочие работы КФО.02 по земляным работам', N'01. Ремонт и содержание дороги от забоя до отвала при транспортировке грунта автомобилями-самосвалами, полуприцепами-самосвалами или думперами.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.02'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 47, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.03.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.03.100', N'Устройство свайного поля', N'01. Холостое бурение скважин (при необходимости).02. Устройство железобетонных буронабивных свай с бурением скважин и креплением обсадными трубами.03. Извлечение труб из скважины (при необходимости).04. Перевозка вынутого грунта в отвал автомобилями-самосвалами.05. Изготовление и монтаж каркасов и закладных деталей.06.  Вырубка, погрузка и перевозка шлама.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.03'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 1, 0, 1.0000, 48, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.04.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.04.100', N'Устройство подливки', N'01. Раскрой и установка досок.02. Крепление досок гвоздями строительными.03. Укладка бетонной смеси.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.04'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 49, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.04.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.04.110', N'Устройство монолитных конструкций', N'01.  Установка досок и щитов опалубки.03.  Укладка бетонной смеси (бетонная подготовка, подбетонка, набетонка, бетонные фундаменты).', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.04'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 1, 0, 1.0000, 50, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.05.100', N'Устройство сборных ЖБ конструкций', N'01. Устройство подстилающих слоев.02. Установка блоков стен подвалов и подпорных стен, фундаментных блоков03. Устройство осадочного шва для сопряжения существующих и пристраиваемых фундаментов.04. Устройство деформационных швов.05. Устройство гидроизоляции.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 1, 0, 1.0000, 51, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.05.110', N'Устройство сборных ЖБ конструкций', N'01. Установка колонн.02. Установка изделий соединительных.03. Монтаж насадок колонн.04. Электродуговая сварка при монтаже.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 52, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.05.120', N'Устройство ЖБ лестниц по готовому основанию', N'01. Укладка ступеней.02. Электродуговая сварка ступеней при монтаже.03. Резка стального профилированного настила.04. Установка закладных деталей.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м ступеней'), 0, 0, 1.0000, 53, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.05.130', N'Устройство колодцев из сборных ж/б плит и колец', N'01. Устройство бетонной подготовки.02. Установка опор из плит и колец.03. Установка монтажных изделий.04. Заделка сальников при проходе труб через фундаменты или стены подвала.05. Разработка грунта с погрузкой на автомобили-самосвалы экскаваторами с ковшом вместимостью 1 (1-1,2) м3, группа грунтов 1, в том числе вблизи существующих конструкций.06. Разработка грунта вручную в траншеях глубиной до 2 м без креплений с откосами, группа грунтов 1, в том числе вблизи существующих трубопроводов.07. Доработка вручную после механизированной разработки.08. Погрузка разработанного вручную грунта на автомобили-самосвалы экскаваторами с ковшом вместимостью 1 (1-1,2) м3, группа грунтов 1.09. Перевозка грузов автомобилями-самосвалами грузоподъемностью 10 т работающих вне карьера на расстояние до 2 км (класс груза 1).10. Работа на отвале.11. Разработка грунта с погрузкой на автомобили-самосвалы экскаваторами с ковшом вместимостью 1 (1-1,2) м3, группа грунтов 1для обратной засыпки.12. Перевозка грузов автомобилями-самосвалами грузоподъемностью 10 т работающих вне карьера на расстояние до 2 км (класс груза 1).13. Засыпка траншей и котлованов с перемещением грунта до 5 м бульдозерами мощностью 79 кВт (108 л.с.), группа грунтов 2.14. Уплотнение грунта пневматическими трамбовками, группа грунтов 1-2.15. Засыпка вручную траншей, пазух котлованов и ям, группа грунтов 1.16. Устройство гидроизоляции обмазочной холодной асфальтовой мастикой в два слоя.17. Устройство подстилающих слоев щебеночных.18. Устройство бетонной отмостки.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 1, 0, 1.0000, 54, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.05.140', N'Устройство ЖБ оград из панелей', N'01. Рытье ям под фундаменты или столбы, рытье траншей под цокольные панели с обратной засыпкой и трамбованием, погрузкой и отвозкой излишков грунта.02. Установка панелей и столбов в стаканы фундаментов с заделкой бетоном.03. Установка монтажных изделий со сваркой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 55, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.05.150', N'Прочие работы КФО.05 по сборным ЖБ конструкциям', N'01. Приготовление глинистого раствора.02. Подача глинистого раствора в застенное пространство колодца.03. Промывка и очистка оборудования и трубопроводов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.05'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м3 глинистого раствора'), 0, 0, 1.0000, 56, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.100', N'Монтаж основных металлоконструкций каркаса зданий, корпусов, кожухов, бункеров', N'01. Подача в зону монтажа.02. Обеспечение жесткости при монтаже, монтаж и демонтаж временных опор.03. Подъем и установка в проектное положение, совместная выверка конструкций.04. Выполнение монтажных соединений (стыков, узлов) при укрупнительной сборке и монтаже конструкций (сварное или болтовое соединение).05. Подгонка (доработка) м/к по месту (при необходимости).06. Монтаж люков, лазов, патрубков, устройство отверстий технологических.07. Антикоррозийное покрытие сварных швов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 57, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.110', N'Монтаж второстепенных металлоконструкций', N'01. Монтаж металлоконструкций навеса, укрытия над приямками или выхода из подвала, карниза02. Бурение ям и обетонирование стоек (при необходимости, с приготовлением бетона в построечных условиях).', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 58, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.120', N'Монтаж галерей и эстакад', N'01. Подача в зону монтажа.02. Обеспечение жесткости при монтаже.03. Подъем и установка в проектное положение, совместная выверка конструкций.04. Выполнение монтажных соединений (стыков, узлов) при укрупнительной сборке и монтаже конструкций (сварное или болтовое соединение).05. Подгонка (доработка) м/к по месту (при необходимости).06. Устройство отверстий технологических.07. Антикоррозийное покрытие сварных швов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 59, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.130', N'Монтаж подкрановых балок', N'01. Контрольная сборка со вспомогательными фермами и тормозным настилом, доработка конструкций, подача в зону монтажа.02. Обеспечение жесткости при монтаже.03. Подъем и установка в проектное положение, совместная выверка конструкций.04. Выполнение монтажных соединений (стыков, узлов) при укрупнительной сборке и монтаже конструкций (сварное или болтовое соединение).05. Монтаж тормозного настила, ферм, траверс, ограждений, подгонка м/к (по месту (при необходимости).06. Устройство подмостей.07. Антикоррозийное покрытие сварных швов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 60, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.140', N'Монтаж металлических мачт, опор, арок и рам', N'01. Подача в зону монтажа.02. Обеспечение жесткости при монтаже.03. Подъем и установка в проектное положение, совместная выверка конструкций.04. Выполнение монтажных соединений (стыков, узлов) при укрупнительной сборке и монтаже конструкций (сварное или болтовое соединение).05. Подгонка (доработка) м/к по месту (при необходимости).06. Устройство отверстий технологических.08. Антикоррозийное покрытие сварных швов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 61, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.150', N'Монтаж опорных конструкций для крепления трубопроводов', N'01. Подача в зону монтажа.02. Обеспечение жесткости при монтаже.03. Подъем и установка в проектное положение, совместная выверка конструкций.04. Выполнение монтажных соединений (стыков, узлов) при укрупнительной сборке и монтаже конструкций (сварное или болтовое соединение).05. Подгонка (доработка) м/к по месту (при необходимости).06. Устройство отверстий технологических.07. Антикоррозийное покрытие сварных швов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 62, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.160', N'Монтаж стального резервуара заводского изготовления', N'01. Сборка и установка стальных конструкций резервуаров (корпус и днище выполняются из рулонной листовой стали, а окрайки днища и крыша - из отдельных металлических конструктивных элементов).02. Монтаж люков, лазов, патрубков и др.03. Зачистка околошовной зоны для производства сварочных работ и восстановление огрунтовки.04. Проверка сварных швов на герметичность.05. Гидравлическое испытание.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 63, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.170', N'Монтаж стального резервуара методом полистовой сборки', N'01. Сборка и установка стальных конструкций резервуаров методом полистовой сборки.02. Монтаж люков, лазов, патрубков и др.03. Зачистка околошовной зоны для производства сварочных работ и восстановление огрунтовки.04. Проверка сварных швов на герметичность.05. Гидравлическое испытание.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 64, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.180', N'Устройство технологических ограждений', N'01. Монтаж сетчатых панелей ограждения.02. Бурение ям под стойки (с учетом погрузки, перевозки грунта, работы на отвале).03. Монтаж стоек.04. Устройство подливки толщиной до 50 мм с приготовлением раствора.05. Устройство бетонных фундаментов.06. Установка монтажных изделий массой до 20 кг.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 1, 0, 0.0097, 65, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.190')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.190', N'Монтаж подвесных подкрановых путей', N'01. Подача в зону монтажа.02. Обеспечение жесткости при монтаже.03. Подъем и установка в проектное положение, совместная выверка конструкций (подвесных путей и монорельсов, подкрановых путей).04. Выполнение монтажных соединений (стыков, узлов) при укрупнительной сборке и монтаже конструкций (сварное или болтовое соединение).05. Подгонка (доработка) м/к по месту (при необходимости).06. Устройство отверстий технологических.07. Антикоррозийное покрытие сварных швов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'п.м.'), 1, 0, 0.0501, 66, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.200')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.200', N'Монтаж защитного ограждения оборудования', N'01. Установка и крепление защитных ограждений оборудования.02. Устройство подмостей.03. Антикоррозийное покрытие сварных швов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 67, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.210')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.210', N'Монтаж конструкций газоходов', N'01. Подача в зону монтажа.02. Обеспечение жесткости при монтаже.03. Подъем и установка в проектное положение, совместная выверка конструкций (газоходы, аспирационные газоходы, выбросные трубы, вытяжные трубы, факельные трубы, свечи дожигания).04. Выполнение монтажных соединений (стыков, узлов) при укрупнительной сборке и монтаже конструкций (сварное или болтовое соединение).05. Подгонка (доработка) м/к по месту (при необходимости).06. Устройство отверстий технологических.07. Антикоррозийное покрытие сварных швов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 68, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.220')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.220', N'Изготовление металлоконструкций', N'01. Изготовление металлоконструкций.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 69, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.230')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.230', N'Устройство шпунтовых ограждений', N'01. Проверка замков, маркировка и разметка свай.02. Изготовление клиновидных свай.03. Подача свай к месту погружения.04. Погружение маячных свай и установка направляющих схваток.05. Погружение свай.06. Смена наголовников и вкладышей.07. Демонтаж направляющих схваток и извлечение маячных свай.08. Срезка голов свай.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т свай'), 1, 0, 1.0000, 70, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06.240')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.06.240', N'прочие работы КФО.06 по монтажу металлоконструкций', N'01. Установка стальных констуркций трубных блоков.02. Заделка отвестий гнезд и борозд.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.06'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 71, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.07.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.07.100', N'Монтаж ограждающих конструкций стен из сэндвич панелей', N'01. Установка панелей с применением крепежных элементов, в т.ч. резка конструкций.02. Заполнение стыков монтажной пеной, герметиком, утеплителем.03. Установка доборных фасонных элементов.04. Установка решеток жалюзийных, закладных/монтажных деталей, обрамление проемов', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.07'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 0.0097, 72, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.07.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.07.110', N'Монтаж ограждающих конструкций стен из профилированного листа', N'01. Установка стальных профилированных листов с креплением, в т.ч. резка конструкций.02. Устройство элементов из листовой оцинкованной стали.03. Устройство герметизации стыков.04. Установка закладных деталей.05. Заделка отверстий.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.07'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 1, 0, 0.0097, 73, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.07.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.07.120', N'Монтаж кровельного покрытия из профлиста', N'01. Установка стальных профилированных листов с креплением, в т.ч. резка конструкций. 02 Устройство мелких покрытий из оцинкованной стали.03. Устройство герметизации стыков.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.07'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 1, 0, 0.0097, 74, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.07.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.07.130', N'Монтаж кровельного покрытия из сэндвич панелей', N'01. Монтаж кровельного покрытия из многослойных панелей заводской готовности.02. Устройство мелких покрытий из листовой оцинкованной стали.03. Заполнение стыков монтажной пеной, герметиком, утеплителем.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.07'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 0.0097, 75, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.08.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.08.100', N'Устройство кровли', N'01.Устройство бортиков в местах примыканий к парапетам, трубам, шахтам и т.п.02. Штукатурка вертикальных поверхностей на высоту примыкания.03. Огрунтовка основания под наклейку материала рулонного кровельного.04. Устройство гидроизоляции вокруг водоприемных воронок.05. Устройство дополнительных слоев в лотках и в местах примыканий.06. Устройство основного кровельного ковра из наплавляемых материалов в два слоя.07. Обделка примыканий фартуками из оцинкованной стали и закрепление их полосовой сталью.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.08'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 76, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.08.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.08.110', N'Утепление кровли', N'01. Укладка монолитных утеплителей из легких бетонов или засыпка утеплителя с трамбованием.02. Устройство разуклонки.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.08'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 77, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.100', N'Кладка наружных стен', N'01. Кладка стен из кирпича или блоков.02. Гидроизоляция стен.03. Армирование кладки стен и других конструкций.04. Монтаж перемычек с заполнением утеплителем.05. Установка закладных деталей.06. Установка монтажных изделий.07. Установка рам дверей и ворот.08. Обрамление проемов угловой сталью.09. Устройство монолитного пояса.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 1, 0, 1.0000, 78, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.110', N'Устройство фасадов (вентилируемые, металлосайдинг) наружных стен', N'01. Разметка проектного положения металлического каркаса.02. Установка и крепление кронштейна выравнивающего к конструкциям здания дюбелями.03. Установка П-образных направляющих профилей в кронштейны с креплением.04. Срезка излишков кронштейна.05. Устройство пароизоляционного слоя.06. Установка углов и нащельников с креплением самонарезающими винтами.07. Установка фасадной панели или металлосайдинга с креплением.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 79, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.120', N'Облицовка наружных стен кирпичом', N'01. Выгрузка материалов.02. Подъем материалов к месту производства работ.03. Перемещение материалов на расстояние до 30 м.04. Устройство и разборка подмостей.05. Укладка кирпичей с расшивкой швов.06. Резка арматурной сетки.07. Укладка арматурной сетки.08. Установка анкеров в облицовочную кладку.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м2 облицовки'), 0, 0, 1.0000, 80, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.130', N'Окраска поверхности наружных стен', N'01. Очистка поверхности.02. Сглаживание торцом дерева.03. Расшивка трещин.04. Проолифка.05. Частичная подмазка с проолифкой подмазанных мест.06. Шлифовка подмазанных мест.07. Первая окраска.08. Вторая окраска.09. Вытягивание филенок.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 81, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.140', N'Штукатурные работы по наружным стенам', N'01. Подготовка поверхностей.02. Устройство из раствора или установка инвентарных маяков.03. Нанесение раствора на поверхности с разравниванием и обработкой накрывочного слоя.04. Очистка маяков от раствора.05. Уход за штукатуркой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 82, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.150', N'Монтаж окон (ПВХ и аллюминий)', N'01. Установка блоков оконных.02. Сверление отверстий.03. Заполнение зазора между рамой и стеной монтажной пеной и уплотнительными прокладками.12. Установка фурнитуры.13. Устройство подоконников.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 83, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.160', N'Установка металлических и противопожарных дверей', N'01. Установка и крепление металлической рамы.02. Навеска дверного полотна двери.03. Установка дверного доводчика.04. Установка дверной фурнитуры.05. Герметизация уплотнителем, терморасширяющейся лентой, монтажной пеной.06. Установка наличников.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 84, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.170', N'Кладка перегородок', N'01. Кладка перегородок из кирпича или блоков.02. Укладка перемычек.03. Крепление перегородок.04. Армирование кладки.05. Установка закладных деталей.06. Установка монтажных изделий.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 0.1250, 85, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.180', N'Штукатурные работы по внутренним стенам', N'01. Приготовление штукатурного раствора из сухих смесей.02. Огрунтовка поверхностей.03. Крепление штукатурной сетки в местах примыканий.04. Нанесение обрызга и  раствора вручную с затиркой.05. Разделка углов06. Разделка потолочных рустов.07. Оштукатуривание ниш отопления.11. Обмазка раствором коробок, наличников и плинтусов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 86, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.190')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.190', N'Устройство стяжки', N'01. Планировка основания.02. Укладка и разравнивание стяжки (бетонной, цементной или легкобетонной).03. Устройство швов.04. Уход за стяжкой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 87, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.200')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.200', N'Устройство ГКЛ облицовки стен', N'01. Разметка проектного положения металлического каркаса.02. Наклейка уплотнительной ленты на направляющие профили и прямые подвесы.03. Установка и крепление направляющих профилей и прямых подвесов дюбелями.04. Установка потолочных профилей в направля­ющие и закрепление их в подвесах.05. Устройство горизонтальных вставок из ПП профиля и крепление их со стойками одноуровневыми соединителями.06. Устройство оконных проемов.07. Наклейка разделительной ленты в местах сопряжения с поверхностью стен и потолка.08. Обшивка каркаса гипсоволокнистыми листами с креплением их самонарезающими винтами.09. Облицовка откосов гипсоволокнистыми листами с применением клея Перлфикс ГВ.10. Заделка внешних углов оконных проемов перфорированными уголками со шпатлеванием.11. Заделка швов между облицовкой откосов и оконной коробкой акриловым герметиком.12. Грунтование кромок ГВЛ.13. Заделка швов шпатлевкой с применением арматурной ленты, поперечных швов и углублений от винтов - без ленты.14. Грунтование поверхности.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 88, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.210')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.210', N'Устройство ГКЛ перегородок', N'01. Разметка  и установка металлического каркаса.02. Наклейка уплотнительной и разделительной лент.03. Установка гипсокартонных листов с креплением их самонарезающими винтами.04. Укладка изоляционного материала.05. Заделка швов шпаклевкой с применением армирующей ленты.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 89, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.220')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.220', N'Устройство силового промышленного пола', N'01. Планировка основания.02. Установка арматуры и арматурных сеток.03. Устройство подстилающих слоев с разравниванием и уплотнением.04. . Грунтовка сухих и свежих бетонных оснований.05. Нанесение сухой упрочняющей смеси на поверхность твердеющего бетона, непосредственно после его укладки.06.  Втирание состава в поверхность при помощи бетоноотделочных машин.07. Устройство деформационных швов.08. Установка плинтусов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 90, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.230')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.230', N'Устройство бетонного пола', N'01. Нанесение состава ( (Mapetop N, Mastertop 100, Mastertop 200, Mastertop 450 и т.п.) на поверхность твердеющего бетона, непосредственно после его укладки.02. Втирание в поверхность при помощи бетоноотделочных машин.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 91, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.240')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.240', N'Устройство пола из чугунных плит', N'01. Планировка основания.02. Устройство прослойки из бетона.03. Устройство покрытий из плит.04. Подготовка поверхности мест установки плинтусов.05. Нарезка плинтусов по размеру.06. Нанесение клеящего состава на поверхность стены и плинтусов.07. Установка плинтусов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 92, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.250')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.250', N'Шпатлевка внутренних стен и потолков', N'01. Нанесение шпатлевки.02. Шлифование поверхности.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 93, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.260')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.260', N'Чистовая отделка внутренних стен', N'01. Сортировка плиток.02. Облицовка поверхностей.03. Приготовление клея из сухой смеси.04. Перерубка плиток и подточка кромок.05. Заполнение швов.06. Распудривание облицованных поверхностей.07. Очистка и промывка поверхности облицовки.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 94, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.270')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.270', N'Оклейка обоями', N'01. Прочистка поверхности.02. Подмазка неровностей.03. Шлифовка подмазанных мест.04. Нанесение клея на стены и обои.05. Оклейка обоями.06. Приготовление клеевых составов.07. Обрезка и нарезка материалов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 95, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.280')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.280', N'Окраска поверхности внутренних стен', N'01. Очистка поверхности.02. Смачивание водой.03. Расшивка трещин.04. Сглаживание торцом дерева.05. Первая огрунтовка.06. Частичная подмазка.07. Шлифовка подмазанных мест.08. Первая сплошная шпатлевка.09. Шлифовка.10. Вторая сплошная шпатлевка.11. Шлифовка.12. Вторая огрунтовка.13. Третья огрунтовка с подцветкой.14. Окраска.15. Торцевание.16. Вытягивание филенок.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 96, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.290')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.290', N'Устройство сантехнических перегородок', N'01. Установка и крепление сантехнических перегородок из ПВХ материалов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 97, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.300')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.300', N'Устройство чистового пола', N'01. Планировка основания.02. Настилка покрытий плитками с подбором и сортировкой плиток, с устройством прослойки и с заделкой швов.03. Уход за покрытиями.04. Очистка и промывка покрытий.05. Устройство плинтусов из плиток.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 98, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.310')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.310', N'Монтаж пластиковых дверей', N'01. Установка и крепление дверной коробки (рамы).02. Заполнение зазора между дверной коробкой и стеной уплотняющими прокладками, монтажной пеной.03. Установка дверной фурнитуры.04. Устройство «порожка».05. Навеска створок.06. Установка дверного доводчика.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 99, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.320')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.320', N'Монтаж деревянных дверей', N'01. Установка блоков.02. Установка дверной фурнитуры.03. Установка дверного доводчика.04. Установка наличников.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 100, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.330')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.330', N'Покрытие (половое) из резиновой крошки', N'01. Наненсение грунтовки на очищенную поверхность подстилающего слоя. 2. Приготовление смеси.03. Укладка смеси на поверхность с разравниванием.04. Прикатка валиком.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 101, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.340')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.340', N'Устройство "Армстронг" и реечных', N'01. Установка элементов крепления.02. Сборка и установка каркасов.03. Облицовка плитами (рейками) потолков.04. Установка декоративного уголка.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 102, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.350')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.350', N'Устройство подвесных и ГКЛ потолков', N'01. Разметка проектного положения металлического каркаса.02. Наклейка уплотнительной ленты на направляющие профили и прямые подвесы.03. Установка и крепление направляющих профилей и прямых подвесов дюбелями.04. Установка подвесов с зажимами и тягами.05. Установка основных и несущих профилей.06. Соединение профилей между собой одноуровневыми соединителями.07. Наклейка разделительной ленты в местах сопряжения с поверхностью стен и перегородок.08. Обшивка каркаса гипсокартонными листами с креплением их самонарезающими винтами.09. Заделка продольных швов шпаклевкой с применением армирующей ленты, поперечных швов и углублений от винтов - без ленты.10. Грунтование поверхности.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 103, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.360')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.360', N'Устройство гидроизоляции пола и фундаментов', N'01. Подготовка основания.02. Приготовление праймера.03. Обмазочная изоляция праймером.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 104, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.370')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.370', N'Устройство основания из щебня', N'01. Планировка основания.02. Поливка грунта водой.03. Раскидывание, разравнивание и вдавливание гравия или щебня в грунт.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 105, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.380')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.380', N'Устройство бетонных подстилающих слоев', N'01. Планировка основания.02. Устройство подстилающего слоя с разравниванием и уплотнением.03. Устройство деформационных швов.04. Уход за подстилающими слоями.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 106, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.390')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.390', N'Монтаж ворот', N'01. Монтаж ворот.02. Облицовка ворот стальным профилированным листом.03. Уплотнение/утепление стыков прокладками.04. Установка приборов (навесы, страховочные крепления, шпингалет, фиксатор и т.д.).05. Монтаж фартука резинового.06. Установка и закрепление устройства самозакрывания (доводчик).', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 107, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.400')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.400', N'Установка входных металлических дверей', N'01. Монтаж конструкций с креплением на болтах, сварке.02. Антикоррозийное покрытие сварных швов.03. Установка дверного доводчика.04. Установка дверной фурнитуры.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 108, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.410')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.410', N'Монтаж стальных оконных блоков', N'01. Установка блоков оконных.02. Сверление отверстий.03. Заполнение стыков теплоизоляционными и герметическими материалами.04. Установка фурнитуры, нащельников, деталей обрамления.05. Устройство подоконников (при необходимости).06. Антикоррозийное покрытие сварных швов.07. Нарезка стекол.08. Очистка фальцев и проолифка.09. Вставка и протирка стекол.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 109, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.420')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.420', N'Устройство подстилающих слоев (песчаных , щебеночных)', N'01. Планировка основания.02. Устройство подстилающего слоя (песчаного, щебеночного) с разравниванием и уплотнением.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 110, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09.430')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.09.430', N'прочие работы КФО.09 по отделочным работам', N'01. Устройство нащельников Н1-Н5 из листовой оцинкованной стали.02. Заполнение пустот ребер настилов минеральной ватой.03. Устройство герметизации вертикальных стыков монтажной пеной.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.09'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 111, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.10.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.10.100', N'Монтаж технологического оборудования', N'01. Сборка и монтаж оборудования.02. Индивидуальное испытание оборудования.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.10'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 112, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.10.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.10.110', N'Монтаж вспомогательного оборудования', N'01. Сборка и установка оборудования02.  Индивидуальные испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.10'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 113, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.10.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.10.120', N'Монтаж грузоподьемного оборудования', N'01. Сборка и монтаж крана. 02.Индивидуальные испытания оборудования.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.10'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 114, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.11.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.11.100', N'Монтаж весового оборудования', N'01. Сборка и установка весов.02. Тарирование.03. Индивидуальное испытание оборудования.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.11'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 115, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.11.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.11.110', N'Устройство лифтового оборудования', N'01. Монтаж оборудования шахты и приямка.02. Монтаж оборудования машинного помещения.03. Монтаж кабины и противовеса, подвеска на канаты.04. Прокладка силовой электропроводки цепей управления, возбуждения, сигнализации в машинном помещении, шахте и кабине.05. Подготовка электроаппаратуры к включению и ее установка.06. Монтаж электроосвещения шахты.07. Устройство переговорной связи в объеме поставки оборудования.08. Регулировочные (пусконаладочные) работы механической части оборудования.09. Статические и динамические испытания.10. Участие в сдаче лифта в эксплуатацию (с представителями органа технического надзора).', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.11'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 лифт'), 1, 0, 1.0000, 116, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.11.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.11.120', N'Монтаж технологического оборудования', N'001. Градирня односекционная вентиляторная площадью до 200м2, производительностью до 2000 м3/ч. Сборка и монтаж технологического оборудования: вентилятора (корпуса вентилятора, электропривода, рабочего колеса); распылителей центробежно-струйных; водоуловительного устройства.02. Индивидуальные испытания оборудования.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.11'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'компл.'), 1, 0, 1.0000, 117, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.11.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.11.130', N'Прочие работы КФО.11 по монтажу технол. оборудования (шт)', N'01. Сборка и монтаж желоба.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.11'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 118, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.12.100', N'Антикоррощийная защита металлоконструкций (АКЗ)', N'01. Огрунтовка в 1 слой.02. Окраска в 1 слой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 119, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.12.110', N'Окраска бетонных отштукатупкенных поверхностей', N'01. Огрунтовка в 1 слой.02. Окраска в 1 слой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 120, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.12.120', N'Подготовка поверхности перед окрашиванием', N'01. Обеспыливание поверхности.02. Обезжиривание поверхностей.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 121, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.12.130', N'Установка подмостей и лесов', N'01. Устройство подмостей для окраски конструкций.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 122, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.12.140', N'Нанесение огнезащитного покрытия на воздуховоды', N'01. Разметка и нарезка оберточного материала.02. Обертывание насухо с промазкой швов.03. Крепление покрытия.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 123, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.12.150', N'Нанесение огнезащитного покрытия на металлоконструкции', N'01. Подготовка окрасочного агрегата.02. Приготовление состава.03. Защита прилегающих поверхностей пленкой.04. Нанесение грунтовки, огнезащитных составов, эмалей.05. Промывка, очистка окрасочного агрегата и шлангов.06. Проведение замеров толщины слоя.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 124, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.12.160', N'Прочие работы КФО.12 работы по АКЗ, ОГЗ', N'01. Обеспыливание поверхности тканей.02. Нанесение огнезащитного пропиточного состава “КЛОД-02” в четыре приема.03. Проведение контрольных проверок на горючесть.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.12'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 125, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.13.100', N'Антикоррозионная изоляция трубопроводов', N'01. Очистка изолируемых поверхностей.02. Покрытие грунтовкой.03. Покрытие слоем мастики толщиной 4 мм.04. Наложение слоя стеклохолста.05. Наружная обертка рулонными материалами.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 0, 0, 1.0000, 126, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.13.110', N'Изоляция горячих поверхностей (тр-д и оборудование)', N'01. Установка конструкций на трубопроводе с подгонкой по месту.02. Крепление конструкций.03. Изготовление и крепление диафрагм.04. Изготовление и приварка штырей или крючьев для крепления тепловой изоляции.05. Изготовление и закрепление пряжек, бандажей.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 127, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.13.120', N'Изоляция холодных поверхностей', N'01. Подготовка изолируемой поверхности.02. Распиловка плит.03. Установка реек каркаса с креплением.04. Разогрев битума.05. Покрытие изолируемой поверхности битумом.06. Укладка теплоизоляционных материалов с подгонкой и креплением.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 128, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.13.130', N'Изоляция ЗРА и фланцев', N'01. Изготовление полуфутляров из матов минераловатных прошивных и листов оцинкованной стали.02. Установка полуфутляров.03. Изготовление диафрагм.04. Установка диафрагм.05. Крепление диафрагм и полуфутляров бандажами или винтами.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 129, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.13.140', N'Изоляция горячих поверхностей вспененным материалом', N'01. Установка изделий на трубопровод с подгонкой и вырезами по месту.02. Промазка швов клеевым составом.03. Проклеивание швов самоклеящейся лентой.04. Крепление изделий зажимами.05. Изготовление и установка диафрагм.06. Окрашивание поверхности изоляции.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 130, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.13.150', N'прочие работы КФО.13 по теплоизоляции', N'01. Изоляция обмазочная "Изотерм" в 2 слоя.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.13'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 131, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.14.100', N'Монтаж стальных трубопроводов ОВК', N'01.Пермещение материалов, изделий и инструмента в пределах строительной площадки.02. Входной контроль и сортировка материалов и изделий.03. Разметка и изготовление деталей с резкой труб и нарезкой резьбы.04. Сборка элементов узлов на резьбе и сварке.05. Исправление дефектов.06. Зачистка сварных соединений трубопоровода.07. Прокладка трубопровода на сварке из стальных труб с установкой фасонных частей и фланцев.08. Гидравлическое испытание трубопровода.09. Промывка трубопровода водой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 132, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.14.110', N'Монтаж пластиковых трубопроводов ОВК', N'01. Прокладка трубопроводов из многослойных металл-полимерных труб с установкой фасонных частей и муфтовой арматуры.02. Гидравлическое испытание и промывка трубопровода водой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 133, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.14.120', N'Монтаж чугунных трубопроводов ОВК', N'01. Прокладка труб с заделкой раструбов.02. Гидравлическое испытание и промывка труб водой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 134, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.14.130', N'Монтаж емкостного оборудования ОВК', N'01. Установка водоподогревателей на готовое основание.02. Гидравлическое испытание водоподогревателей.03. Установка арматуры и контрольно-измерительных приборов.04. Насадка и приварка фланцев на концы труб.05. Соединение фланцев на болтах и прокладках.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 135, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.14.140', N'Монтаж насосного оборудования ОВК', N'01. Установка анкерных болтов.02. Установка агрегатов на готовое основание.03. Насадка и приварка фланцев на концы труб.04. Соединение фланцев на болтах и прокладках.05. Опробование насосов на холостом ходу.06. Гидравлическое испытание.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 136, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.14.150', N'Монтаж сантехники (унитазы, биде, писсуары, раковин и пр)', N'01. Установка канализационного отвода.02. Крепление инсталяции к стене и полу.03. Разметка мест сверления.04. Сверление отверстий.05. Регулировка, крепление корпуса (рамы) и сливного бака.06. Установка унитаза (писсуара).07. Подключение к системам водоснабжения и канализации.08. Установка кнопки смыва (крана смывного).', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'компл.'), 0, 0, 1.0000, 137, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.14.160', N'Монтаж трубопроводной арматуры ОВК', N'01. Насадка и приварка ответных фланцев на концы труб.02. Установка арматуры с соединением фланцев на болтах и прокладках.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 138, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.14.170', N'Монтаж узлов учета и регулирования ОВК', N'01. Установка контрольно-измерительных приборов.02. Гидравлические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'компл.'), 1, 0, 1.0000, 139, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.14.180', N'прочие работы КФО.14 по монтажу ОВК', N'01. Установка и заделка кронштейнов со сверлением отверстий.02. Установка регистров с присоединением их к трубопроводам.03. Тепловое испытание с проверкой отопительных приборов на прогрев и регулировкой.04. Гидравлическое испытание регистров.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.14'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 140, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.100', N'Монтаж технологических трубопроводов (кроме эстакад) до 57 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка, снятие фаски, прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 141, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.110', N'Монтаж технологических трубопроводов (кроме эстакад) от 325 до 820 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка, снятие фаски, прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 142, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.120', N'Монтаж технологических трубопроводов (кроме эстакад) от 57 мм до 273 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка, снятие фаски, прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 143, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.130', N'Монтаж технологических трубопроводов (кроме эстакад) от 820 мм до 1420 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка, снятие фаски, прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 144, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.140', N'Монтаж технологических трубопроводов (кроме эстакад) свыше 1420 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка, снятие фаски, прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 145, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.150', N'Монтаж технологических трубопроводов на эстакадах до 57 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка и прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 146, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.160', N'Монтаж технологических трубопроводов на эстакадах от 325 до 820 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка и прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 147, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.170', N'Монтаж технологических трубопроводов на эстакадах от 57 мм до 273 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка и прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 148, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.180', N'Монтаж технологических трубопроводов на эстакадах от 820 мм до 1420 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка и прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 149, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.190')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.190', N'Монтаж технологических трубопроводов на эстакадах свыше 1420 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка и прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 150, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.200')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.200', N'Монтаж технологической трубопроводной арматуры от 150 до 400 мм', N'01. Установка арматуры.02. Присоединение арматуры.03. Опробование арматуры.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 151, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.210')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.210', N'Монтаж технологической трубопроводной арматуры от 32 до 100 мм', N'01. Установка арматуры.02. Присоединение арматуры.03. Опробование арматуры.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 152, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.220')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.220', N'Монтаж технологической трубопроводной арматуры от 600 до 800 мм', N'01. Установка арматуры.02. Присоединение арматуры.03. Опробование арматуры.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 153, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.230')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.230', N'Монтаж технологической трубопроводной арматуры от 800 до 1000 мм', N'01. Установка арматуры.02. Присоединение арматуры.03. Опробование арматуры.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 154, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.240')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.240', N'Монтаж технологической трубопроводной арматуры свыше 1200 мм', N'01. Установка арматуры.02. Присоединение арматуры (без учета сварного соединения).03. Опробование арматуры.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 155, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.250')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.250', N'Монтаж прочих технологических стальных трубопроводов до 200 мм', N'01. Монтаж трубопровода на фланцевых или муфтовых соединениях с постановкой резиновых прокладок.02. Отличительная окраска.03. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 156, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.260')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.260', N'Монтаж прочих технологических стальных трубопроводов свыше 200 мм', N'01. Монтаж трубопровода на фланцевых или муфтовых соединениях с постановкой резиновых прокладок.02. Отличительная окраска.03. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 157, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.270')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.270', N'Монтаж прочих технологических чугунных трубопроводов до 200 мм', N'01. Опускание и укладка труб.02. Заделка раструбов смоляной прядью и асбестоцементным раствором или с применением резиновых уплотнительных манжет.03. Гидравлическое испытание трубопровода с устройством и разборкой временных упоров.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 158, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.280')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.280', N'Монтаж прочих технологических чугунных трубопроводов свыше 200 мм', N'01. Опускание и укладка труб.02. Заделка раструбов смоляной прядью и асбестоцементным раствором или с применением резиновых уплотнительных манжет.03. Гидравлическое испытание трубопровода с устройством и разборкой временных упоров.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 159, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.290')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.290', N'Монтаж прочих технологических ЖБ трубопроводов', N'01. Зачистка дна траншеи.02. Очистка поверхности труб.03. Приготовление грунтовки и разогревание мастики.04. Покрытие грунтовкой.05. Покрытие битумной мастикой за 2 раза.06. Опускание и укладка труб.07. Заделка раструбов смоляной прядью и асбестоцементным раствором.08. Гидравлическое испытание.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 160, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.300')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.300', N'Трубопровод с фитингами на резьбе до 108 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода.03. Очистка и прихватка труб.04. Монтаж трубопровода, установка и приварка бобышек для заземления.05. Отличительная окраска.06. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 161, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.310')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.310', N'Трубопровод с фитингами на резьбе до 25 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода.03. Очистка и прихватка труб.04. Монтаж трубопровода, установка и приварка бобышек для заземления.05. Отличительная окраска.06. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 162, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.320')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.320', N'Трубопровод с фитингами на резьбе свыше 108 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода.03. Очистка и прихватка труб.04. Монтаж трубопровода, установка и приварка бобышек для заземления.05. Отличительная окраска.06. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 163, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.330')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.330', N'Фланцевое соединение технологических труб от 159 до 530 мм', N'01. Осмотр поверхностей фланцев.02. Установка прокладок.03. Центрирование и соединение фланцевых стыков болтами.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'соединение'), 0, 0, 1.0000, 164, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.340')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.340', N'Фланцевое соединение технологических труб от 25 до 108 мм', N'01. Осмотр поверхностей фланцев.02. Установка прокладок.03. Центрирование и соединение фланцевых стыков болтами.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'соединение'), 0, 0, 1.0000, 165, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.350')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.350', N'Фланцевое соединение технологических труб свыше 1420 мм', N'01. Осмотр поверхностей фланцев.02. Установка прокладок.03. Центрирование и соединение фланцевых стыков болтами.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'соединение'), 0, 0, 1.0000, 166, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.360')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.360', N'Устройство пластиковых колодцев', N'01. Доработка траншеи под колодец.02. Устройство песчаного основания с трамбованием.03. Монтаж пластикового колодца.04. Разметка на корпусе колодца мест планируемого ввода коммуникаций и прорезание отверстий.05. Ввод коммункикаций сквозь адаптеры герметичного ввода.06. Засыпка котлована песком с послойным трамбванием.07. Монтаж опалубки под бетонное разгрузочное кольцо.08. Заливка опалубки бетоном.09. Заделка зазора между горловиной и бетонным разгрузочным кольцом смоляным канатом.10. Устройство оклеечной гидроизоляции из пороизола бетонного кольца.11. Установка люка.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 колодец'), 0, 0, 1.0000, 167, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.370')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.370', N'Монтаж трубных проводок при монтаже трубопроводов', N'01. Изготовление и монтаж блоков трубных проводок.02. Маркировка.03. Гидравлическое или пневматическое испытание.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 168, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.380')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.380', N'Врезки и присоединения трубопровода к действующей магистрали', N'01. Изготовление штуцера/патрубка.02. Разметка гнезд в трубах и вырезка отверстия под штуцер. 03.Установка и подгонка штуцера/патрубка по месту.04. Приварка штуцера/патрубка.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 врезка'), 0, 0, 1.0000, 169, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.390')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.390', N'Монтаж технологических трубопроводов (кроме эстакад) от 159 до 325 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка, снятие фаски, прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 170, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.400')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.400', N'Монтаж технологических трубопроводов на эстакадах от 159 до 325 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов03. Резка, очистка и прихватка труб.04. Монтаж трубопровода.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 171, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.410')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.410', N'Монтаж технологического трубопровода в траншее от 57 мм до 159 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов.03. Резка и очистка труб.04. Укладка звеньев и отдельных труб в траншеи, прихватка.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 1, 0, 1000.0000, 172, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.420')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.420', N'Монтаж технологического трубопровода в траншее от 159 до 325 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов.03. Резка и очистка труб.04. Укладка звеньев и отдельных труб в траншеи, прихватка.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 1, 0, 1000.0000, 173, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.430')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.430', N'Монтаж технологического трубопровода в траншее от 325 до 820 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов.03. Резка и очистка труб.04. Укладка звеньев и отдельных труб в траншеи, прихватка.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 1, 0, 1000.0000, 174, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.440')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.440', N'Монтаж технологического трубопровода в траншее от 820 мм до 1420 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов.03. Резка и очистка труб.04. Укладка звеньев и отдельных труб в траншеи, прихватка.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 1, 0, 1000.0000, 175, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.450')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.450', N'Монтаж технологического трубопровода в траншее свыше 1420 мм', N'01. Перемещение с погрузкой и выгрузкой.02. Разметка мест прокладки трубопровода, подбор элементов, труб, деталей и материалов.03. Резка и очистка труб.04. Укладка звеньев и отдельных труб в траншеи, прихватка.05. Выверка и закрепление смонтированного трубопровода.06. Отличительная окраска.07. Гидравлические и пневматические испытания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 1, 0, 1000.0000, 176, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.460')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.460', N'Фланцевое соединение технологических труб от 820 до 1420 мм', N'01. Осмотр поверхностей фланцев.02. Установка прокладок.03. Центрирование и соединение фланцевых стыков болтами.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'соединение'), 0, 0, 1.0000, 177, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.470')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.470', N'Монтаж прочих технологических пластиковых трубопроводов', N'01. Монтаж трубопровода (ПВХ, полиэтиленовый, полипропиленовый и пр.) с установкой фасонных частей.02. Гидравлические и пневматические испытания.03. Присыпка трубопровода слоем грунта толщиной до 20 см (при прокладке в траншее).', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 178, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.480')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.480', N'Монтаж прочих технологических асбестоцементных трубопроводов', N'01. Зачистка дна траншеи.02. Опускание и укладка труб.03. Установка муфт с заделкой смоляной прядью и асбестоцементным раствором.04. Гидравлическое испытание.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 1, 0, 1000.0000, 179, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.490')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.490', N'Устройство ГНБ при бестраншейной прокладке трубопроводов', N'01. Опускание, укладка и центрирование труб на направляющей раме.02. Сварка труб с приваркой ножа.03. Установка нажимных устройств.04. Продавливание трубы в грунт с обслуживанием механизмов.05. Наращивание нажимных устройств.06. Разработка и удаление грунта из трубы с подъемом на бровку котлована.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 1, 0, 1.0000, 180, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.500')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.500', N'Сварка ручная дуговая при монтаже технологических трубопроводов (длина сварного шва) до 108 мм', N'01. Включение сварочных машин и агрегатов и установление режимов сварки.02. Очистка кромок перед сваркой от пыли и грязи.03. Сварка соединений.04. Зачистка от шлака промежуточных и последнего слоев шва.05. Промер шва и клеймение сварных соединений.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'см сварного шва'), 0, 0, 1.0000, 181, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.510')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.510', N'Сварка ручная дуговая при монтаже технологических трубопроводов (длина сварного шва) от 108 до 325 мм', N'01. Включение сварочных машин и агрегатов и установление режимов сварки.02. Очистка кромок перед сваркой от пыли и грязи.03. Сварка соединений.04. Зачистка от шлака промежуточных и последнего слоев шва.05. Промер шва и клеймение сварных соединений.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'см сварного шва'), 0, 0, 1.0000, 182, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.520')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.520', N'Сварка ручная дуговая при монтаже технологических трубопроводов (длина сварного шва) от 530 до 1420 мм', N'01. Включение сварочных машин и агрегатов и установление режимов сварки.02. Очистка кромок перед сваркой от пыли и грязи.03. Сварка соединений.04. Зачистка от шлака промежуточных и последнего слоев шва.05. Промер шва и клеймение сварных соединений.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'см сварного шва'), 0, 0, 1.0000, 183, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.530')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.530', N'Сварка ручная дуговая при монтаже технологических трубопроводов (длина сварного шва) свыше 1420 мм', N'01. Включение сварочных машин и агрегатов и установление режимов сварки.02. Очистка кромок перед сваркой от пыли и грязи.03. Сварка соединений.04. Зачистка от шлака промежуточных и последнего слоев шва.05. Промер шва и клеймение сварных соединений.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'см сварного шва'), 0, 0, 1.0000, 184, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.540')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.540', N'Сварка ручная АРГОНодуговая при монтаже технологических трубопроводов (длина сварного шва) до 108 мм', N'01. Очистка кромок перед сваркой от пыли и грязи.02. Заготовка и очистка присадочной проволоки.03. Заготовка и заточка вольфрамовых электродов.04. Настройка горелки и сварочного кабеля.05. Подключение редуктора к баллону с аргоном, установка заглушек, заполнение трубы аргоном.06. Протирка стыка ацетоном перед сваркой.07. Проварка корневого шва.08. Включение сварочных машин и агрегатов и установление режимов сварки.09. Доварка стыка, заполнение разделки.10. Зачистка от шлака промежуточных и последнего слоев шва.11. Промер шва и клеймение сварных соединений.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'см сварного шва'), 0, 0, 1.0000, 185, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.550')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.550', N'Сварка ручная АРГОНодуговая при монтаже технологических трубопроводов (длина сварного шва) от 159 до 325 мм', N'01. Очистка кромок перед сваркой от пыли и грязи.02. Заготовка и очистка присадочной проволоки.03. Заготовка и заточка вольфрамовых электродов.04. Настройка горелки и сварочного кабеля.05. Подключение редуктора к баллону с аргоном, установка заглушек, заполнение трубы аргоном.06. Протирка стыка ацетоном перед сваркой.07. Проварка корневого шва.08. Включение сварочных машин и агрегатов и установление режимов сварки.09. Доварка стыка, заполнение разделки.10. Зачистка от шлака промежуточных и последнего слоев шва.11. Промер шва и клеймение сварных соединений.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'см сварного шва'), 0, 0, 1.0000, 186, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.560')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.560', N'Сварка ручная АРГОНодуговая при монтаже технологических трубопроводов (длина сварного шва) от 530 до 1420 мм', N'01. Очистка кромок перед сваркой от пыли и грязи.02. Заготовка и очистка присадочной проволоки.03. Заготовка и заточка вольфрамовых электродов.04. Настройка горелки и сварочного кабеля.05. Подключение редуктора к баллону с аргоном, установка заглушек, заполнение трубы аргоном.06. Протирка стыка ацетоном перед сваркой.07. Проварка корневого шва.08. Включение сварочных машин и агрегатов и установление режимов сварки.09. Доварка стыка, заполнение разделки.10. Зачистка от шлака промежуточных и последнего слоев шва.11. Промер шва и клеймение сварных соединений.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'см сварного шва'), 0, 0, 1.0000, 187, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.570')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.570', N'Сварка ручная АРГОНодуговая при монтаже технологических трубопроводов (длина сварного шва) свыше 1420 мм', N'01. Очистка кромок перед сваркой от пыли и грязи.02. Заготовка и очистка присадочной проволоки.03. Заготовка и заточка вольфрамовых электродов.04. Настройка горелки и сварочного кабеля.05. Подключение редуктора к баллону с аргоном, установка заглушек, заполнение трубы аргоном.06. Протирка стыка ацетоном перед сваркой.07. Проварка корневого шва.08. Включение сварочных машин и агрегатов и установление режимов сварки.09. Доварка стыка, заполнение разделки.10. Зачистка от шлака промежуточных и последнего слоев шва.11. Промер шва и клеймение сварных соединений.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'см сварного шва'), 0, 0, 1.0000, 188, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.580')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.580', N'Капиллярный контроль оборудования', N'01. Очитска поверхности с последующей сушкой.02. Заполнение полостей краской.03. Удаление излишков краски.04. Нанесение проявителя.05. Контроль проявившихся дефектов', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 189, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.590')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.590', N'Капиллярный контроль трубопроводов (кол-во стыков)', N'01. Очитска поверхности с последующей сушкой.02. Заполнение полостей краской.03. Удаление излишков краски.04. Нанесение проявителя.05. Контроль проявившихся дефектов', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'стык'), 0, 0, 1.0000, 190, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.600')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.600', N'Очистка трубопроводов перед испытаниями', N'01. Присоединение и отсоединение водопровода.02. Наполнение трубопровода водой.03. Промывка трубопровода до полного осветления воды.04. Спуск воды из трубопровода.05. Наполнение трубопровода хлорной водой.06. Спуск хлорной воды из трубопровода.07. Вторичное наполнение и промывка трубопровода водой после дезинфекции.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 0, 0, 1.0000, 191, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.620')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.620', N'Подогрев сварных соединений труб (кол-во стыков)', N'01. Подогрев (предварительный или сопутствующий) сварных соединений труб.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'стык'), 0, 0, 1.0000, 192, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.630')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.630', N'Термическая обработка сварных стыков (кол-во стыков)', N'01. Термическая обработка сварных стыков трубопроводов', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'стык'), 0, 0, 1.0000, 193, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.640')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.640', N'Ультразвуковая дефектоскопия трубопровода (длина шва)', N'01. Магнитопорошковый контроль.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м шва'), 0, 0, 1.0000, 194, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.650')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.650', N'Пневмоиспытание трубопроводов', N'01. Подъем давления в газопроводе.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 0, 0, 1.0000, 195, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.660')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.660', N'Рентгенографический контроль трубопровода', N'01. Рентгенографический контроль трубопровода.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'снимок'), 0, 0, 1.0000, 196, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15.670')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.15.670', N'Прочие работы КФО.15 по монтажу технол. трубопроводов', N'01. Опускание, установка арматуры и соединение фланцев.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.15'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 197, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.16.100', N'Монтаж воздуховодов', N'01. Сборка звеньев воздуховодов в блоки.02. Сверление отверстий под воздуховоды.03. Установка и заделка креплений.04. Подъем блоков и временная их подвеска.05. Установка блоков в проектное положение.06. Соединение блоков на болтах с постановкой прокладок.07. Установка дроссель-клапанов в патрубках, шиберов, сеток в рамках, заглушек питометражных лючков и скоб и креплений.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 198, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.16.110', N'Монтаж воздухораспределителей и жалюзийных вент. решеток', N'01. Установка воздухораспределителей с присоединением к воздуховодам.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 199, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.16.120', N'Монтаж воздушно отопительного оборудования', N'01. Насадка и приварка фланцев на концы трубопровода.02. Установка агрегатов.03. Соединение фланцев на болтах с постановкой прокладок.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 200, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.16.130', N'Монтаж калориферов', N'01. Сварка стыков трубопроводов.02. Установка калориферов на готовое основание.03. Соединение фланцев на болтах с постановкой прокладок.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 201, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.16.140', N'Монтаж основного вентиляционного оборудования', N'01. Установка вентиляторов с электро-двигателями на одной оси или на клиноременной передаче на готовое основание.02. Выверка вентиляторов.03. Опробование вентиляторов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 202, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.16.150', N'Установка воздушных клапанов и обратных заслонок', N'01. Установка клапанов или заслонок с выверкой и закреплением.02. Опробование работы заслонок или клапанов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 203, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.16.160', N'Установка фильтрующих устройств', N'01. Установка ячеек в каркас фильтров.02. Смачивание ячеек маслом.03. Установка кассет фильтров в рамы с присоединением к воздуховодам.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 204, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.16.170', N'Установка циклонов', N'01. Установка на постамент и закрепление.02. Присоединение к воздуховодам.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 205, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.16.180', N'Монтаж оборудования кондиционирования', N'01. Установка секций камеры, заслонок или клапанов.02. Соединение секций на фланцах с установкой прокладок и затяжкой болтов.03. Присоединение камер к трубопроводам.04. Опробование камеры.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.16'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 206, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.100', N'Монтаж кабеля сетей связи', N'01. Разработка грунта в отвал экскаваторами.02. Доработка грунта вручную.03. Устройство трубопроводов  из асбестоцементных труб с соединением полиэтиленовыми муфтами.04.  Прокладка кабеля в подземной канализации.05.  Монтаж муфт кабельных.06. Засыпка траншей и котлованов.07. Уплотнение грунта пневматическими трамбовками.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 1, 0, 1000.0000, 207, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.110', N'Монтаж линии ВОЛС', N'01. Разметка трассы (заготовка и установка колышков и вех, промер трассы).02. Разработка грунта навымет ( в том числе вручную вблизи существующего электрокабеля).03. Устройство постели.04. Подготовка к прокладке кабеля в траншеях: погрузка и разгрузка барабана ни тележку, размотка кабеля с тележки вдоль траншеи, подноска кабеля к траншее.05. Прокладка кабеля в траншеях: укладка кабеля на дно траншеи, установка временных знаков ни стыках и поворотах, маркировка кабеля. 06.Измерение на кабельной площадке затухания  волоконно-оптического кабеля. 07.Монтаж муфт прямых для волоконно-оптических кабелей в колодце.08. Обратная засыпка траншей.09. Уплотнение грунта.10. Укладка сигнальной ленты', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 0, 0, 1000.0000, 208, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.120', N'Монтаж камеры видеонаблюдения', N'01. Монтаж одного прибора (передатчик видеосигнала, приемник видеосигнала, универсальный вентилятор, блок силовых розеток, видеорегистратор, видеоусилитель, стекируемый коммутатор, диск HDD и пр.).02. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 209, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.130', N'Монтаж оборудования оповещения и связи', N'01. Монтаж радиостанции.02. Монтаж антенны, поставляемой комплектно с радиостанцией.03. Отдельно устанавливаемый преобразователь или блок питания.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'компл.'), 1, 0, 1.0000, 210, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.140', N'Монтаж приборов оповещения и связи', N'01. Разметка и просверливание отверстий.02. Нарезание резьбы.03. Заготовка косынок из изоляционного материала и укладка их по месту.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 211, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.150', N'Монтаж приборов охранно-пожарной сигнализации, оповещения и связи', N'01. Установка.02. Присоединение.03. Опробование.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 212, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.160', N'Монтаж противопожарного аэрозольного оборудования', N'01. Сверление отверстий для монтажа кронштейна потолочного.02. Установка потолочного кронштейна.03. Монтаж генератора в комплекте с УИП с защитным экраном.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'компл.'), 1, 0, 1.0000, 213, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.170', N'Монтаж противопожарного оборудования (водяное, пенное и газовое)', N'01. Установка.02. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 214, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.180', N'Монтаж противопожарного оборудования тросовой системы', N'01. Монтаж устройств.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 215, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.190')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.190', N'Прозвонка слаботочных кабелей', N'01. Подготовка концов кабеля к измерениям.02. Напайка наконечника.03. Измерение кабелей абонентских линий уплотняемых и не уплотняемых высокочастотной аппаратурой.04. Измерение сопротивления изоляции, омического сопротивления шлейфа, электрической емкости.05. Запайка концов кабеля.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'пара'), 0, 0, 1.0000, 216, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.200')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.200', N'Монтаж приборов и средств автоматизации', N'01. Монтаж термопреобразователя с гильзой.02. Монтаж датчиков.03. Монтаж шкафа и фланш-панели.04. Монтаж шкафа датчиков.05. Монтаж проводника заземляющий открыто по строительным основаниям из полосовой стали.06. Монтаж соединительного блока.07. Монтаж узла продувки с климатозащитным кожухом.08. Монтаж блока приема передатчика с кожухом.09. Монтаж поста аварийной сигнализации.10. Монтаж коробки.11. Монтаж стойки.12. Монтаж полки.13. Монтаж короба.14. Трубные проводки из бесшовных труб.15. Кабельные вводы.16. Заделка монтажной пеной зазора между трубой и стеной.17. Огрунтовка металлических поверхностей за два раза грунтовкой.18. Окраска металлических огрунтованных поверхностей эмалью 2 РАЗА.19. Изготовление и монтаж расширителя (гильз).20. Монтаж арматуры муфтовой с ручным приводом или без привода водопроводной на условное давление до 10 МПа, диаметр условного прохода 15 мм.21. Подключение к щиту.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'узел'), 1, 0, 1.0000, 217, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.210')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.210', N'Монтаж противопожарного порошкового оборудования', N'01. Монтаж модуля МПП.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 0.0500, 218, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17.220')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.17.220', N'Прочие работы КФО.17 по монтажу слаботочных сетей', N'01. Выравнивание дна котлована.02. Установка железобетонных конструкций.03. Заделка швов цементным раствором.04. Установка люка с устройством и оштукатуриванием горловины.05. Установка кронштейнов на ершах с окраской.06. Окраска металлических частей колодца.07. Установка консоли.08. Покрытие колодца битумом за 2 раза с разогреванием мастики.09. Обетонировка люков колодцев, толщина слоя 15 см.10. Пробивка отверстий для ввода труб.11. Устройство ввода труб в колодцы.12. Заделка кирпичом отверстий.13. Разработка грунта в отвал экскаваторами, группа грунтов 2.14. Доработка грунта вручную.15. Засыпка траншей и котлованов с перемещением грунта до 5 м бульдозерами.16. Уплотнение грунта пневматическими трамбовками.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.17'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 колодец'), 0, 0, 1.0000, 219, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.100', N'Монтаж заземления линий электропередач', N'01. Выполнение траншей для заземления.02. Укладка заземлителей.03. Правка и рубка стали с укладкой заземлителей в траншеи.04. Сварка стыков заземлителей и присоединение заземляющих проводников к стальным опорам и оттяжкам опор.05. Обратная засыпка траншей грунтом.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 м заземляющих устройств'), 0, 0, 1.0000, 220, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.110', N'Монтаж заземления', N'01. Изготовление заземлителей.02. Монтаж и заглубление заземлителя.03. Приварка.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 221, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.120', N'Монтаж кабелей и проводов внутри зданий', N'01. Сборка шинопроводов в блоки.02. Прокладка шинопроводов.03. Присоединение узлов питания.04. Прозвонка.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 1, 0, 1000.0000, 222, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.130', N'Монтаж силового кабеля (траншея, эстакада)', N'01. Установка и снятие лебедки.02. Установка и снятие барабана.03. Установка и снятие роликов.04. Прокладка кабеля.05. Проверка состояния изоляции кабеля до и после прокладки.06. Маркировка.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 1, 0, 1000.0000, 223, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.140', N'Монтаж трубных проводок внутри зданий при прокладке кабелей', N'01. Изготовление трубопровода, сборка деталей трубопровода в блоки.02. Установка коробок.03. Прокладка труб, крепление и затягивание проволоки', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 224, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.150', N'Подвеска проводов ВЛ', N'01. Сборка изоляторов  в гирлянды для сложных опор.02. Раскатка проводов.03. Соединение проводов.04. Подъем проводов на опоры.05. Натягивание и визирование проводов.06. Крепление проводов и устройство перемычек.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 км линии (3 провода) при 10 опорах'), 1, 0, 1000.0000, 225, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.160', N'Покрытие кабеля, проложенного в траншее', N'01. Покрытие кабеля.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 226, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.170', N'Провода и грозозащитные тросы вл 35-750 Кв', N'01. Приготовление состава.02. Нанесение антикоррозионного покрытия.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 км троса'), 1, 0, 1.0000, 227, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.180', N'Монтаж кабельных лотков', N'01. Разметка мест установки и креплений.02. Установка основания короба с раскроем и подгонкой основания.03. Установка оснований соединительных элементов (углов поворота, пересечений, отводов).04. Надвижка крышек короба и соединительных элементов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 228, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.190')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.190', N'Кабельные линии специальных установок для горнорудных выработок', N'01. Замер трассы и определение длины кабеля.02. Подкатка барабана с кабелем к месту работы.03. Снятие боковой обшивки барабана и проверка состояния изоляции кабеля.04. Подвеска барабана с кабелем при помощи тали.05. Установка и снятие роликов.06. Раскатка кабеля с перерезкой его и временной заделкой концов после перерезки.07. Укладка кабеля на установленные конструкции с креплением и обходом препятствий.08. Установка ограждений кабеля.09. Съем барабана.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 229, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.200')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.200', N'Монтаж осветительных приборов', N'01. Изготовление и установка конструкций.02. Установка коробок клеммных.03. Монтаж фотореле.04. Заземление.05. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 230, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.210')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.210', N'Монтаж распределительных устройств внутри зданий', N'01. Изготовление и установка конструкций.02. Монтаж блочного вводно-распределительного устройства.03. Разводка по устройствам и подключение жил кабелей или проводов.04. Заземление.05. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 231, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.220')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.220', N'Монтаж столбовых трансформаторов', N'01. Развозка конструкций и материалов оснастки опор по пикетам.02. Выгрузка конструкций и материалов на пикетах с выкладкой.03. Бурение котлованов.04. Установка и выверка стоек с засыпкой пазух котлованов.05. Установка металлоконструкций и оборудования на стойках.06. Установка и закрепление трансформатора.07. Ошиновка оборудования.08. Монтаж ответвлений к ВЛ.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 232, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.230')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.230', N'Установка железобетонных опор под ВЛ', N'01. Развозка конструкций и материалов оснастки опор по пикетам.02. Выгрузка конструкций и материалов на пикетах с выкладкой.03. Сборка опор.04. Установка изоляторов.05. Присоединение заземляющих проводников к стальным деталям опор.06. Бурение котлованов с доработкой грунта вручную.07. Установка конструкций ригелей.08. Установка и выверка опор с засыпкой котлованов и трамбованием грунта.09. Нумерация опор и закрепление предупредительных плакатов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'опора'), 0, 0, 1.0000, 233, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.240')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.240', N'Установка деревянных опор для линий электропередач', N'01. Развозка конструкций и материалов оснастки опор по пикетам.02. Выгрузка конструкций на пикетах с выкладкой.03. Заготовка и сборка опор.04. Устройство приставок.05. Установка изоляторов.06. Бурение котлованов с доработкой грунта вручную.07. Установка и выверка опор.08. Засыпка котлованов с послойным трамбованием грунта.09. Нумерация опор и закрепление предупредительных плакатов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 опора'), 0, 0, 1.0000, 234, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.250')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.250', N'Установка стальных опор под ВЛ', N'01. Бурение котлована диаметром до 1 м.02. Разравнивание и трамбование основания.03. Сборка трубчатого фундамента, установка, выверка и закрепление конструкций.04. Очистка поверхности от грязи.05. Грунтовка и гидроизоляция.06. Устройство отмостки из перемятого грунта.07. Установка и заглубление заземлителей.08. Приварка заземляющих проводников к заземлителям.09. Сборка болтовых конструкций и присоединение сварных секций, закрепление опор.10. Электродуговая сварка при монтаже метал. опор-обварка гаек на высоту 5-6 м от основания для предотвращения хищения элементов опор.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 235, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18.260')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.18.260', N'Прочие работы КФО.18 по монтажу силового кабеля и провода', N'01. Устройство постели.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.18'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 236, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.100', N'Монтаж аккумуляторов', N'01. Установка.02. Сборка.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 237, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.110', N'Монтаж конденсаторов', N'01. Установка.02. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 238, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.120', N'Монтаж КТП', N'01. Спуск оборудования в клети на платформах или в вагонетках с погрузкой и выгрузкой.02. Перемещение оборудования до проектных отметок в вагонетках или на платформах при помощи электролебедок, включая выгрузку, по горным выработкам с рельсовыми путями и углом наклона до 13 градусов.03. Установка шкафа высокого напряжения.04. Установка и соединение блоков шкафов и шин низковольтного распределительного устройства.05. Установка силового трансформатора.06. Соединение трансформатора со стороны низкого и высокого напряжения.07. Прокладка, окраска и приварка шин заземления.08. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'компл.'), 1, 0, 1.0000, 239, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.130', N'Монтаж опор электрообуродования', N'01. Монтаж шинных опор.02. Прокладка, приварка и окраска шин заземления.03. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 240, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.140', N'Монтаж предохранителей', N'01. Установка.02. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 241, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.150', N'Монтаж преобразовательных устройств', N'01. Установка.02. Установка перемычки заземления.03. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шкаф'), 1, 0, 1.0000, 242, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.160', N'Монтаж разрядников', N'01. Установка.02. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'компл.'), 0, 0, 1.0000, 243, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.170', N'Монтаж разъединителей и изоляторов', N'01. Установка.02. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 244, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.180', N'Монтаж распределительных устройств (РУ)', N'01. Установка.02. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 245, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.190')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.190', N'Монтаж реактора бетонного', N'01. Установка.02. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 компл. (3 фазы)'), 0, 0, 1.0000, 246, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.200')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.200', N'Монтаж трансформаторов', N'01. Установка трансформатора.02. Прокладка шин.03. Установка шинодержателей.04. Присоединение.05. Установка выключателей, переключателей, арматуры светосигнальной, реле и т.п. в релейный отсек с присоединением.06. Изготовление и установка металлических конструкций под оборудование.07. Устройство заземления оборудования.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 247, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.210')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.210', N'Монтаж щитов и электроустановочных шкафов', N'01. Установка клеммного бокса.02. Установка монтажной платы.03. Монтаж DIN- рейки на кронштейнах.04. Монтаж клеммной колодки.05. Монтаж кабельного зажима с контргайкой.06. Монтаж кронштейна.07. Заземление.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 248, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.220')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.220', N'Монтаж электрической машины', N'01. Подготовка к испытанию, сдаче под наладку и пуску.02. Оконцевание и присоединение жил к зажимам.03. Установка гибкой перемычки.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 249, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.230')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.230', N'Монтаж электроустановочных изделий', N'01. Установка и регулировка выключателя.02. Установка и регулировка привода.03. Соединение выключателя с приводом.04. Изготовление и установка тяг и тягоуловителей.05. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 1, 0, 1.0000, 250, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.240')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.240', N'Расключение силового кабеля', N'01. Комплектование проводов.02. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 жила'), 0, 0, 1.0000, 251, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.250')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.250', N'Электромонтажные работы крана мостового', N'01. Электромонтаж крана.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 252, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.260')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.260', N'Монтаж шинопроводов', N'01. Изготовление шинного моста.02. Установка опорных изоляторов.03. Установка шинного моста.04. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 253, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.270')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.270', N'Монтаж шинопроводов и токопроводов', N'01. Заготовка, окраска и прокладка шин.02. Установка шинодержателей.03. Присоединение к сборным шинам и аппаратам.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 254, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.280')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.280', N'Монтаж низковольтных комплексных устройств', N'01. Установка.02. Заземление.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 255, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.290')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.290', N'Монтаж токопроводов', N'01. Изготовление и прокладка токопровода.02. Установка шинодержателей и компенсаторов.03. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м (3 фазы)'), 0, 0, 1.0000, 256, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19.300')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.19.300', N'Прочие работы КФО.19 по монтажу электросилового оборудования', N'01. Установка.02. Присоединение.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.19'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 257, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.100', N'Устройство автодорог (асфальт, бетон)', N'01. Устройство подстилающих и выравнивающих слоев оснований из песка толщиной до 25 см и щебня до 25 см.02. Розлив вяжущих материалов.03. Устройство оснований цементобетонных толщиной до 20 см .04. Устройство покрытий из асфальтобетонных смесей, нижний и верхний слой общей толщиной до 10 см.05. Установка бортовых камней.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 258, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.110', N'Устройство дорог из ЖБ плит', N'01. Планировка и прикатка земляного полотна с поливом водой.02. Укладка плит.03. Заделка гнезд между плитами, правка и сварка выпусков арматуры.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 м3 сборных железобетонных конструкций'), 0, 0, 1.0000, 259, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.120', N'Устройство проездов из щебня', N'01. Устройство оснований.02. Устройство покрытий щебеночных (общая толщина основания и покрытия до 50 см).', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 260, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.130', N'Устройство тротуаров', N'01. Устройство оснований из песка толщиной до 10 см, щебня толщиной до 15 см, цементобетонных толщиной до 16 см .02. Устройство покрытий дорожек и тротуаров (асфальтобетонного покрытия толщиной до 3 см или из тротуарной плитки).03. Установка бортовых камней бетонных.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 261, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.140', N'Установка бортовых камней', N'01. Отрывка траншеи.02. Устройство подушки из щебня.03. Устройство бетонного основания с уплотнением, установкой и снятием опалубки.04. Установка бортовых камней с подтеской, заливкой швов и их расшивкой.05. Очистка бортовых камней от наплывов.06. Засыпка грунта с обратной стороны борта.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 0.0400, 262, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.150', N'Устройство озеленения', N'01. Подготовка почвы для устройства партерного и обыкновенного газона с внесением растительной земли слоем 10 см вручную или механизированным способом.02. Посев газонов партерных, мавританских и обыкновенных вручную.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 263, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.160', N'Устройство отмостки', N'01. Устройство щебеночного основания отмостки толщиной до 200мм.02. Устройство отмостки из бетона В15.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 264, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.170', N'Устройство дорожних знаков', N'01. Бурение ям бурильно-крановыми машинами.02. Установка бесфундаментных стоек.03. Окраска стоек.04. Разделка оснований дорожных знаков, засыпка ям.05. Навеска металлических табличек, переход к следующему знаку.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 265, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.180', N'Посадка деревьев и кустарников', N'01. Подготовка стандартных посадочных мест для деревьев и кустарников с круглым комом земли механизированным способом.02. Посадка деревьев и кустарников с комом земли размером до 0,8x0,6 м. .', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 266, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.190')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.190', N'Монтаж малых архитектурные форм', N'01. Копание ям под стойки.02. Установка монтажных изделий.03. Крепление стоек с обетонировкой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 267, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20.200')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.20.200', N'прочие работы КФО.20 по дорогам и благоустройству', N'01. Установка фундамента под светофор переездной сигнализации.02. Сборка и установка светофора.03. Заготовка и установка жгутов коммутации в светофоре.04. Установка фундамента под автошлагбаум.05. Установка автошлагбаума.06. Установка и подключение ревунов (сирены) и фонарей.07. Установка и крепление привода автошлагбаума.08. Окрашивание автошлагбаума с мачтой, головками, щитом и кожухом механизма.09. Засыпка котлована грунтом.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.20'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 268, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.21.100', N'Футеровочные работы', N'01. Резка плитки.02. Приготовление грунтовки, шпатлевки и замазки.03. Огрунтовка основания.04. Шпатлевка.05. Футеровка плиткой или кирпичом плашмя.06. Затирка швов.07. Контроль качества.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 269, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.21.110', N'Футеровочные работы', N'01. Сортировка штучных материалов.02. Резка фасонных керамических изделий.03. Сушка и просеивание инертных наполнителей.04. Приготовление раствора жидкого стекла.05. Смешивание отвердителя с наполнителем.06. Приготовление грунтовки и шпатлевки.07. Огрунтовка основания.08. Шпатлевка основания.09. Кладка.10. Затирка швов.11. Контроль качества.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 270, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.21.120', N'Кладка элементов тепловых агрегатов', N'01. Сортировка с подборкой изделий или блоков по размерам насухо.02. Нанесение раствора на изделия.03. Укладка изделий с осаживанием.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 1, 0, 1.0000, 271, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.21.130', N'Нанесение упрочняющего покрытия (по типу Торкрет-бетон)', N'01. Заготовка и установка каркаса из проволоки.02. Торкретирование.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 272, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.21.140', N'Обмуровочные работы', N'01. Нарезка полос из фанеры (полистирола).02. Укладка фанеры (полистирола) в температурные швы.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м2 поверхности'), 0, 0, 1.0000, 273, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.21.150', N'Кладка доменных печей', N'01. Сортировка с подборкой изделий или блоков по размерам насухо.02. Пригоночная теска изделий или шлифовка блоков.03. Нанесение раствора на изделия или пасты на блоки.04. Укладка изделий или блоков с осаживанием.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 274, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.21.160', N'Прочие работы КФО.21 по кладке доменных печей и агрегатов', N'01. Разметка мест крепления плит.02. Приварка анкеров.03. Навеска плит.04. Сверление отверстий в плитах для анкеров.05. Монтаж плит с креплением на болтах и сварке.06. Заделка гнезд в местах установки шайб.07. Заделка швов между плитами.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.21'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 275, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.100', N'Демонтаж деревянных конструкций', N'01. Разборка деревянных конструкций со снятием креплений.02. Разборка засыпок, смазок и изоляционных прокладок.03. Отбивка штукатурки.04. Снятие оконных, дверных, воротных переплетов.05. Выемка коробок со снятием наличников, отбивкой откосов и четвертей в проемах.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 276, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.110', N'Демонтаж полов', N'01. Разборка покрытий полов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 277, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.120', N'Демонтаж дорог, площадок и тротуаров', N'01. Разборка покрытий полов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 278, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.130', N'Демонтаж ограждающих конструкций', N'01. Демонтаж конструкций до габаритов транспортировки в соответствии с нормами приемки при утилизации отходов, с разделением металла от утеплителя.02. Любые иные сопутствующие работы', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 279, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.140', N'Демонтаж облицовки поверхностей', N'01. Разборка облицовки с очисткой от раствора.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 280, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.150', N'Демонтаж кровли', N'01. Снятие материала покрытия.02. Скатывание рулонных материалов, полученных от разборки.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 281, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.160', N'Демонтаж лестниц, ступеней, маршей', N'01. Установка и последующая разборка лесов и подмостей.02. Пробивка борозд и гнезд в местах заделки ступеней, площадок, балок.03. Разборка поручней и лестничных решеток.04. Разборка косоуров и балок площадок.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 282, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.170')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.170', N'Ремонт полов', N'01. Обеспыливание поверхности.02. Нанесение антикоррозийного состава в 2 слоя на оголенную арматуру.03. Увлажнение поверхности.04. Очистка поверхности пола.05. Огрунтовка бетонных поверхностей грунтом БИРСС-Грунт-Универсал.06. Устройство покрытий из ремонтной смеси БИРСС РСМ-350-1.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 283, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.180')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.180', N'Ремонт дорог', N'01. Очистка оснований.02. Кирковка с поливкой водой, со сгребанием, перемещением и последующим разравниванием вскиркованного материала, россыпь и разравнивание новых материалов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 284, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.190')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.190', N'Ремонт потолков', N'01. Очистка поверхности щетками.02. Обеспыливание поверхности.03. Нанесение антикоррозийного состава в 2 слоя на оголенную арматуру.04. Огрунтовка бетонных поверхностей грунтом БИРСС-Грунт-Универсал.05. Приготовление ремонтной смеси.06. Восстановление защитного слоя ремонтной смесью толщиной до 30 мм.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 285, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.200')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.200', N'Ремонт штукатурных поверхностей', N'01. Насечка поверхности штрихами глубиной 3-5 мм.02. Очистка поверхности от пыли и грязи.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 286, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.210')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.210', N'Ремонт чистовой отделки (обои, ГКЛ, окраска)', N'01. Снятие обоев.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м²'), 0, 0, 1.0000, 287, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.220')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.220', N'Пробивка борозд', N'01. Разметка мест пробивки.02. Пробивка и зачистка борозд.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 288, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.230')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.230', N'Пробивка отверстий', N'01. Разметка мест пробивки.02. Пробивка гнезд и отверстий.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'отверстие'), 0, 0, 1.0000, 289, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.240')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.240', N'Пробивка проемов', N'01. Разметка мест пробивки.02. Пробивка проемов с зачисткой откосов.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 290, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.250')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.250', N'Сверление перфоратором или дрелью', N'01. Разметка мест сверления.02. Сверление отверстий.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'отверстие'), 0, 0, 1.0000, 291, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22.260')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.22.260', N'Сверление кольцевыми алмазными сверлами', N'01. Разметка мест сверления отверстий.02. Установка, выверка и крепление станка с присоединением к электро- и водопроводным сетям.03. Опускание привода со сверлом к месту сверления.04. Сверление отверстий.05. Подъем привода со сверлом.06. Удаление и уборка керна.07. Установка удлинителей при сверлении отверстий на глубину свыше 500 мм.08. Смена сверл.09. Перестановка станка.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.22'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'отверстие'), 0, 0, 1.0000, 292, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.23.100', N'Демонтаж железобетонных и кирпичных конструкций', N'01. Разборка конструкций.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 293, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.23.110', N'Демонтаж зданий и сооружений', N'01. Разборка зданий с применением строительных машин и механизмов.02. Выборка годных материалов с очисткой и укладкой в штабели.03. Перевозка материалов, полученных от разборки, на промежуточный склад.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 294, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.23.120', N'Демонтаж огнеупорной кладки', N'01. Разборка кладки.02. Очистка годного кирпича.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 295, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.23.130', N'Ремонт кладки и перегородок', N'01. Кладка отдельных участков стен или заделка проемов кирпичом с перевязкой новой кладки со старой.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 1, 0, 1.0000, 296, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.23.140', N'Ремонт фундаментов', N'01. Поперечное перепиливание и окорка досок.02. Изготовление щитов со сборкой на гвоздях.03. Обработка щитов мастикой с обшивкой толем.04. Установка щитов по месту.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 297, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.23.150', N'Заделка отверстий, гнезд и борозд', N'01. Расчистка отверстий, гнезд и борозд.02. Установка опалубки.03. Укладка арматуры.04. Укладка бетона.05. Заделка отверстий, гнезд и борозд.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 298, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.23.160', N'Усиление конструкций железобетоном', N'01. Ограждение места работ деревянными щитами.02. Заготовка и установка временных креплений.03. Очистка и подготовка поверхности усиливаемых конструкций.04. Установка и разборка опалубки.05. Установка арматуры.06. Укладка бетона.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.23'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 1, 0, 1.0000, 299, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.24.100', N'Демонтаж металлоконструкций', N'01. Демонтаж металлоконструкций и оборудования (за исключением электрооборудования) без сохранения с нарезкой до габаритов транспортировки в соответствии с нормами приемки при утилизации отходов, с разделением на марки стали при необходимости.02. Очистка от огнеупоров, технологических отходов  и др.материалов.03. Любые иные сопутствующие работы', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 300, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.24.110', N'Демонтаж подвесных, подкрановых путей', N'01. Демонтаж конструкций без сохранения с нарезкой до габаритов транспортировки в соответствии с нормами приемки при утилизации отходов, с разделением на марки стали при необходимости.02. Любые иные сопутствующие работы', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 301, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.24.120', N'Демонтаж ЖД путей', N'01. Очистка каналов от грязи и ила со сгребанием в кучи и выкидкой на поверхность вручную.02. Разболчивание стыков.03. Разборка пути звеньями с погрузкой звеньев на платформы и выгрузкой звеньев на базе.04. Планировка балластной призмы.05. Расшивка звеньев с сортировкой и укладкой материалов в штабеля.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'км'), 0, 0, 1.0000, 302, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.24.130', N'Демонтаж опор линий электропередач', N'01. Снятие и опускание проводов.02. Сматывание проводов в бухты.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 опора (2 провода)'), 0, 0, 1.0000, 303, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.24.140', N'Усиление конструкций металлом', N'01. Установка инвентарных лесов.02. Установка тупиков на подкрановых балках.03. Очистка балки от пыли и грязи.04. Изготовление металлоконструкций.05. Подъем и установка металлоконструкций.06. Разборка лесов.07. Очистка рабочей площадки.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 304, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.24.150', N'Ремонт проемов с помощью перемычек', N'01. Пробивка гнезд, борозд, отверстий.02. Заготовка и установка металлических элементов.03. Заделка гнезд и борозд.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24'), (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 1, 0, 1.0000, 305, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.24.160', N'Удаление козловых образований', N'01. Удаление козловых образований', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 0, 0, 1.0000, 306, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24.270')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.24.270', N'Прочие работы КФО.24 по ремонту и демонтажу (м2)', N'01. Разработка грунта вручную с зачисткой дна.02. Устройство и разборка креплений.03. Обратная засыпка грунта с трамбованием ручными трамбовками.04. Удаление лишнего грунта.05. Устройство ходов для ручных тележек.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.24'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 307, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.25.100', N'Балластировка пути', N'01. Дозировка балласта, подъемка пути и стрелочных переводов.02. Выправка и рихтовка пути со сплошной подбивкой шпал и уплотнением балласта.03. Стабилизация пути и оправка балластной призмы.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 308, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25.110')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.25.110', N'Монтаж контактной сети', N'01. Завеска монтажных роликов на консоли, фиксация положения консолей, перевод несущего троса из монтажных роликов на консоли.02. Регулировка положения консолей в плане, изготовление и установка звеньевых струн на несущий трос с применением коушей.03. Раскатка проводов (тросов) «поверху» и их стыкование.04. Временная фиксация контактных проводов на кривых участках пути.05. Установка фиксаторных кронштейнов или ушек, армирование фиксаторов изоляторами, монтаж фиксаторов, регулировка подвесок.06. Завеска монтажных роликов на консоли, фиксация положения консолей, перевод несущего троса из монтажных роликов на консоли.07. Монтаж и изготовление поперечных электрических соединителей, фиксация гирлянд изоляторов на кривых участках пути.08. Монтаж и изготовление средней анкеровки контактного провода.09. Монтаж вспомогательного троса рессорной струны и закрепление на нем звеньевых струн, изготовление струн.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 309, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25.120')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.25.120', N'Монтаж линейного ж/д оборудования', N'01. Установка стативов.02. Установка кабельростов.03. Соединение стативов между собой с креплением.04. Прокладка проводов по стативам и кабельростам.05. Выправка и вязка жгутов проводов.06. Расшивка и подключение концов проводов.07. Присоединение к приборам электрических проводок пайкой и под винт.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 310, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25.130')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.25.130', N'Установка светофора, указателя', N'01. Сборка светофорных головок (в том числе наборных головок).02. Сборка светофоров на железобетонной или металлической мачте.03. Разработка ям, котлован, траншей.04. Установка смонтированных светофоров в разработанные ямы, котлованы, траншеи с выправкой и засыпкой их грунтом.05. Сборка и установка светофорных литерных табличек, щитков.06. Заготовка и установка жгутов коммутации в светофоре.07. Установка и монтаж трансформаторных ящиков.08. Окрашивание светофоров.09. Заготовка заземляющих проводов.10. Монтаж заземлений светофоров, светофорных мостиков, консолей и других напольных устройств на металлической или железобетонной опоре.11. Установка и подключение звонка (сирены) на мачте светофора переездной сигнализации.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт.'), 0, 0, 1.0000, 311, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25.140')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.25.140', N'Устройство верхнего строения ж/д пути', N'01. Сборка и транспортировка звеньев пути/укладочных элементов.02. Укладка пути звеньями/отдельными элементами, крепление рельсов к шпалам, сболчивание стыков (при необходимости).03. Установка противоугонов.04. Балластировка пути и стрелочных переводов.05. Выправка пути перед сдачей в постоянную эксплуатацию.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 0, 0, 1.0000, 312, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25.150')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.25.150', N'Устройство ж/д переезда', N'01. Устройство дренажной призмы.02. Укладка лежней (брусьев).03. Устройство желоба, изготовление и укладка деревянных подкладок и съемных брусьев.04. Устройство основания под плиты.05. Укладка плит ( железобетонных , резинобетонных, резиновых).06. Укладка асфальтобетона.07. Установка путевых и сигнальных элементов ограждения с окраской.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'1 переезд'), 0, 0, 1.0000, 313, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25.160')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.25.160', N'Устройство стрелочного перевода', N'01. Сборка стрелочных переводов блоками, тип рельсов Р65, марка перевода 1/9.02. Укладка стрелочных переводов блоками, тип рельсов Р65, марка перевода 1/9.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.25'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'компл.'), 0, 0, 1.0000, 314, SYSUTCDATETIME());
END;
GO

IF NOT EXISTS (SELECT 1 FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.26.100')
BEGIN
    INSERT INTO [PhysicalVolumesKfo] ([Code], [Name], [Description], [ParentId], [ClassifierId], [UnitId], [IsAssessment], [IsArchived], [Coefficient], [SortOrder], [CreatedAt])
    VALUES (N'KQ.26.100', N'Разработка грунта при горновскрышных работах', N'01. Экскавация грунта.02. Укладка грунта в отвал.', (SELECT [Id] FROM [PhysicalVolumesKfo] WHERE [Code] = N'KQ.26'), NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, 1.0000, 315, SYSUTCDATETIME());
END;
GO

-- Наследование ClassifierId от родителя
WHILE 1 = 1
BEGIN
    UPDATE pv
    SET [ClassifierId] = p.[ClassifierId], [ModifiedAt] = SYSUTCDATETIME()
    FROM [PhysicalVolumesKfo] pv
    INNER JOIN [PhysicalVolumesKfo] p ON p.[Id] = pv.[ParentId]
    WHERE pv.[ClassifierId] IS NULL
      AND p.[ClassifierId] IS NOT NULL;

    IF @@ROWCOUNT = 0
        BREAK;
END;
GO

