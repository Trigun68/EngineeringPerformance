-- ============================================================================
-- Refresh Equipments from «Спр_Оборудование» (replace by CatalogKeyHash)
-- Источник: source.xlsx
-- ============================================================================
SET NOCOUNT ON;
BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM [UnitsOfMeasurement] WHERE [Name] = N'шт')
BEGIN
    INSERT INTO [UnitsOfMeasurement] ([Name], [Description], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'шт', N'', 1000, 0, SYSUTCDATETIME());
END;

-- 1. Upsert catalog rows from source
IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочно-модульная насосная станция водоснабжения производительностью18-54 м3/ч, напор 30 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Блочно-модульная насосная станция водоснабжения производительностью18-54 м3/ч, напор 30 м', N'Тип/марка: 296204-4Y0000-ОП-ПЗ.ОЛ3
ID по каталогу: 314562', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 0, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочно-модульная насосная станция водоснабжения производительностью18-54 м3/ч, напор 30 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Блочно-модульная насосная станция водоснабжения производительностью18-54 м3/ч, напор 30 м', [Description] = N'Тип/марка: 296204-4Y0000-ОП-ПЗ.ОЛ3
ID по каталогу: 314562', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 0, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочно-модульная насосная станция водоснабжения производительностью18-54 м3/ч, напор 30 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочно-модульная станция водяного/пенного пожаротушения', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Блочно-модульная станция водяного/пенного пожаротушения', N'Тип/марка: 296204-4М0000-ОП-П31-ПТ.ОЛ
ID по каталогу: 307321', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 1, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочно-модульная станция водяного/пенного пожаротушения', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Блочно-модульная станция водяного/пенного пожаротушения', [Description] = N'Тип/марка: 296204-4М0000-ОП-П31-ПТ.ОЛ
ID по каталогу: 307321', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 1, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочно-модульная станция водяного/пенного пожаротушения', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочный тепловой пункт с зависимой схемой отопления', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Блочный тепловой пункт с зависимой схемой отопления', N'Тип/марка: 295093-ТП.ОЛ
ID по каталогу: 260409', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 2, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочный тепловой пункт с зависимой схемой отопления', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Блочный тепловой пункт с зависимой схемой отопления', [Description] = N'Тип/марка: 295093-ТП.ОЛ
ID по каталогу: 260409', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 2, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочный тепловой пункт с зависимой схемой отопления', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочный тепловой пункт с зависимой схемой системы отопления и с закрытой схемой ГВС', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Блочный тепловой пункт с зависимой схемой системы отопления и с закрытой схемой ГВС', N'Тепловой пункт АБК
Тип/марка: 292460-ТМ.ОЛ1
ID по каталогу: 176327', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 3, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочный тепловой пункт с зависимой схемой системы отопления и с закрытой схемой ГВС', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Блочный тепловой пункт с зависимой схемой системы отопления и с закрытой схемой ГВС', [Description] = N'Тепловой пункт АБК
Тип/марка: 292460-ТМ.ОЛ1
ID по каталогу: 176327', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 3, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочный тепловой пункт с зависимой схемой системы отопления и с закрытой схемой ГВС', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Вытяжная установка L=3500 м3/ч, Р=600 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Вытяжная установка L=3500 м3/ч, Р=600 Па', N'1 и2 этажи
ID по каталогу: 221015', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 4, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Вытяжная установка L=3500 м3/ч, Р=600 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Вытяжная установка L=3500 м3/ч, Р=600 Па', [Description] = N'1 и2 этажи
ID по каталогу: 221015', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 4, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Вытяжная установка L=3500 м3/ч, Р=600 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Вытяжной канальный вентилятор L=1060 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Вытяжной канальный вентилятор L=1060 м3/ч', N'АБК 1 этаж - 4 шт., АБК 2 этаж - 2 шт., АБК 3 этаж - 1 шт. душевые
Тип/марка: 293224-ИОС4.УС.ОЛ
ID по каталогу: 194281
ID обор. полный: 283424', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 5, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Вытяжной канальный вентилятор L=1060 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Вытяжной канальный вентилятор L=1060 м3/ч', [Description] = N'АБК 1 этаж - 4 шт., АБК 2 этаж - 2 шт., АБК 3 этаж - 1 шт. душевые
Тип/марка: 293224-ИОС4.УС.ОЛ
ID по каталогу: 194281
ID обор. полный: 283424', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 5, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Вытяжной канальный вентилятор L=1060 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Дымосос ДПЭ-7(1ЦМ) в комплекте с рукавом рукавом всасывающим 5м, воздуховодом специальным одинарным и рукавом напорным 10м, дополнительно: - узлом стыковочным УС-1вв EI30 (для напорного рукава) врезным размером 200х200мм - 1шт.; - узлом стыковочным УС-1в EI30 (однонаправленный, только отсос) врезным размером 200х200 -1шт.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Дымосос ДПЭ-7(1ЦМ) в комплекте с рукавом рукавом всасывающим 5м, воздуховодом специальным одинарным и рукавом напорным 10м, дополнительно: - узлом стыковочным УС-1вв EI30 (для напорного рукава) врезным размером 200х200мм - 1шт.; - узлом стыковочным УС-1в EI30 (однонаправленный, только отсос) врезным размером 200х200 -1шт.', N'Электропомещения
ID по каталогу: 204692', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 6, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Дымосос ДПЭ-7(1ЦМ) в комплекте с рукавом рукавом всасывающим 5м, воздуховодом специальным одинарным и рукавом напорным 10м, дополнительно: - узлом стыковочным УС-1вв EI30 (для напорного рукава) врезным размером 200х200мм - 1шт.; - узлом стыковочным УС-1в EI30 (однонаправленный, только отсос) врезным размером 200х200 -1шт.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Дымосос ДПЭ-7(1ЦМ) в комплекте с рукавом рукавом всасывающим 5м, воздуховодом специальным одинарным и рукавом напорным 10м, дополнительно: - узлом стыковочным УС-1вв EI30 (для напорного рукава) врезным размером 200х200мм - 1шт.; - узлом стыковочным УС-1в EI30 (однонаправленный, только отсос) врезным размером 200х200 -1шт.', [Description] = N'Электропомещения
ID по каталогу: 204692', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 6, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Дымосос ДПЭ-7(1ЦМ) в комплекте с рукавом рукавом всасывающим 5м, воздуховодом специальным одинарным и рукавом напорным 10м, дополнительно: - узлом стыковочным УС-1вв EI30 (для напорного рукава) врезным размером 200х200мм - 1шт.; - узлом стыковочным УС-1в EI30 (однонаправленный, только отсос) врезным размером 200х200 -1шт.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Емкость подземная дренажная (40 м3)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Емкость подземная дренажная (40 м3)', N'Тип/марка: 296204-4С0000-ОП-ПЗ.ОЛ28
ID по каталогу: 309375', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 7, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Емкость подземная дренажная (40 м3)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Емкость подземная дренажная (40 м3)', [Description] = N'Тип/марка: 296204-4С0000-ОП-ПЗ.ОЛ28
ID по каталогу: 309375', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 7, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Емкость подземная дренажная (40 м3)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор DN1000 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно с шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Затвор DN1000 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно с шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', N'Тип/марка: 290474-ВК.ОЛ1
ID по каталогу: 197882', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 8, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор DN1000 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно с шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Затвор DN1000 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно с шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', [Description] = N'Тип/марка: 290474-ВК.ОЛ1
ID по каталогу: 197882', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 8, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор DN1000 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно с шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор DN800 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно со шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Затвор DN800 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно со шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', N'Тип/марка: 293381-НВ.ОЛ4
ID по каталогу: 292124', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 9, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор DN800 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно со шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Затвор DN800 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно со шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', [Description] = N'Тип/марка: 293381-НВ.ОЛ4
ID по каталогу: 292124', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 9, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор DN800 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно со шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор шиберный 1000х1000', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Затвор шиберный 1000х1000', N'Тип/марка: 295093-ТХ3.ОЛ1
ID по каталогу: 268916', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 10, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор шиберный 1000х1000', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Затвор шиберный 1000х1000', [Description] = N'Тип/марка: 295093-ТХ3.ОЛ1
ID по каталогу: 268916', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 10, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор шиберный 1000х1000', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор шиберный ножевой DN800', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Затвор шиберный ножевой DN800', N'Тип/марка: 296204-4V0000-ОП-ПЗ.ОЛ5
ID по каталогу: 317403', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 11, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор шиберный ножевой DN800', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Затвор шиберный ножевой DN800', [Description] = N'Тип/марка: 296204-4V0000-ОП-ПЗ.ОЛ5
ID по каталогу: 317403', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 11, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор шиберный ножевой DN800', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Комплект приточных установок (1 раб., 1 рез.) L=13000 м3/ч с компрессорно-конденсаторными блоками и клапанами', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Комплект приточных установок (1 раб., 1 рез.) L=13000 м3/ч с компрессорно-конденсаторными блоками и клапанами', N'Помещения персонала
Тип/марка: Спецификация №4100101319 от 17.06.2021г.
(291553-11-ОВ.ОЛ)
ID по каталогу: 180345', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 12, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Комплект приточных установок (1 раб., 1 рез.) L=13000 м3/ч с компрессорно-конденсаторными блоками и клапанами', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Комплект приточных установок (1 раб., 1 рез.) L=13000 м3/ч с компрессорно-конденсаторными блоками и клапанами', [Description] = N'Помещения персонала
Тип/марка: Спецификация №4100101319 от 17.06.2021г.
(291553-11-ОВ.ОЛ)
ID по каталогу: 180345', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 12, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Комплект приточных установок (1 раб., 1 рез.) L=13000 м3/ч с компрессорно-конденсаторными блоками и клапанами', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран Ду32 мм Ру16 кгс/см2 муф.', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран Ду32 мм Ру16 кгс/см2 муф.', N'Здание склада слябов, встроенные помещения
Тип/марка: 11б27п1
ID по каталогу: 167095', NULL, NULL, 1, 0, NULL, 13, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран Ду32 мм Ру16 кгс/см2 муф.', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран Ду32 мм Ру16 кгс/см2 муф.', [Description] = N'Здание склада слябов, встроенные помещения
Тип/марка: 11б27п1
ID по каталогу: 167095', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 13, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран Ду32 мм Ру16 кгс/см2 муф.', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой г/п 50 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А5, ГЗМ - крюк;', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой г/п 50 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А5, ГЗМ - крюк;', N'Тип/марка: 296204-4А0000-ОП-ПЗ.ОЛ4
ID по каталогу: 304998', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 14, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой г/п 50 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А5, ГЗМ - крюк;', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой г/п 50 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А5, ГЗМ - крюк;', [Description] = N'Тип/марка: 296204-4А0000-ОП-ПЗ.ОЛ4
ID по каталогу: 304998', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 14, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой г/п 50 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А5, ГЗМ - крюк;', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой грейферный двухбалочный г/п 10+10 т Пролет 30 м; ГЗМ - грейфер + крюк; Назначение – выгрузка окалины.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой грейферный двухбалочный г/п 10+10 т Пролет 30 м; ГЗМ - грейфер + крюк; Назначение – выгрузка окалины.', N'ID по каталогу: 308433', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 15, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой грейферный двухбалочный г/п 10+10 т Пролет 30 м; ГЗМ - грейфер + крюк; Назначение – выгрузка окалины.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой грейферный двухбалочный г/п 10+10 т Пролет 30 м; ГЗМ - грейфер + крюк; Назначение – выгрузка окалины.', [Description] = N'ID по каталогу: 308433', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 15, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой грейферный двухбалочный г/п 10+10 т Пролет 30 м; ГЗМ - грейфер + крюк; Назначение – выгрузка окалины.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой грейферный двухбалочный г/п 5 т Пролет 16 м; Назначение – выгрузка окалины.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой грейферный двухбалочный г/п 5 т Пролет 16 м; Назначение – выгрузка окалины.', N'ID по каталогу: 308432', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 16, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой грейферный двухбалочный г/п 5 т Пролет 16 м; Назначение – выгрузка окалины.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой грейферный двухбалочный г/п 5 т Пролет 16 м; Назначение – выгрузка окалины.', [Description] = N'ID по каталогу: 308432', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 16, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой грейферный двухбалочный г/п 5 т Пролет 16 м; Назначение – выгрузка окалины.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный г/п 16 т Пролет 34 м, Н=22 м, ГЗМ - крюк; Назначение – для ремонтных работ', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный г/п 16 т Пролет 34 м, Н=22 м, ГЗМ - крюк; Назначение – для ремонтных работ', N'Тип/марка: 296204-4B0000-ОП-ПЗ.ОЛ11
ID по каталогу: 308505', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 17, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный г/п 16 т Пролет 34 м, Н=22 м, ГЗМ - крюк; Назначение – для ремонтных работ', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный г/п 16 т Пролет 34 м, Н=22 м, ГЗМ - крюк; Назначение – для ремонтных работ', [Description] = N'Тип/марка: 296204-4B0000-ОП-ПЗ.ОЛ11
ID по каталогу: 308505', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 17, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный г/п 16 т Пролет 34 м, Н=22 м, ГЗМ - крюк; Назначение – для ремонтных работ', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 10 т', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный опорный г/п 10 т', N'ID по каталогу: 309546', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 18, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 10 т', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный опорный г/п 10 т', [Description] = N'ID по каталогу: 309546', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 18, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 10 т', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 110/25 т Пролет 28 м, Н=13 м, ГЗМ - крюк; Назначение – транспортировка валков и валковых опор', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный опорный г/п 110/25 т Пролет 28 м, Н=13 м, ГЗМ - крюк; Назначение – транспортировка валков и валковых опор', N'Тип/марка: 296204-4D0000-ОП-ПЗ.ОЛ12
ID по каталогу: 306903', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 19, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 110/25 т Пролет 28 м, Н=13 м, ГЗМ - крюк; Назначение – транспортировка валков и валковых опор', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный опорный г/п 110/25 т Пролет 28 м, Н=13 м, ГЗМ - крюк; Назначение – транспортировка валков и валковых опор', [Description] = N'Тип/марка: 296204-4D0000-ОП-ПЗ.ОЛ12
ID по каталогу: 306903', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 19, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 110/25 т Пролет 28 м, Н=13 м, ГЗМ - крюк; Назначение – транспортировка валков и валковых опор', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 120/25 т Пролет 22 м, Н=19 м, ГЗМ - двурогий крюк; Назначение – транспортировка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный опорный г/п 120/25 т Пролет 22 м, Н=19 м, ГЗМ - двурогий крюк; Назначение – транспортировка оборудования', N'Тип/марка: 296204-4C0000-ОП-ПЗ.ОЛ20
ID по каталогу: 308515', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 20, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 120/25 т Пролет 22 м, Н=19 м, ГЗМ - двурогий крюк; Назначение – транспортировка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный опорный г/п 120/25 т Пролет 22 м, Н=19 м, ГЗМ - двурогий крюк; Назначение – транспортировка оборудования', [Description] = N'Тип/марка: 296204-4C0000-ОП-ПЗ.ОЛ20
ID по каталогу: 308515', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 20, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 120/25 т Пролет 22 м, Н=19 м, ГЗМ - двурогий крюк; Назначение – транспортировка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 120/32 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный опорный г/п 120/32 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования', N'Тип/марка: 296204-4C0000-ОП-ПЗ.ОЛ18
ID по каталогу: 308513', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 21, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 120/32 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный опорный г/п 120/32 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования', [Description] = N'Тип/марка: 296204-4C0000-ОП-ПЗ.ОЛ18
ID по каталогу: 308513', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 21, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 120/32 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 28 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 28 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', N'Тип/марка: 296204-4B0000-ОП-ПЗ.ОЛ16
ID по каталогу: 308509', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 22, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 28 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 28 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', [Description] = N'Тип/марка: 296204-4B0000-ОП-ПЗ.ОЛ16
ID по каталогу: 308509', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 22, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 28 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 37 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 37 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', N'ID по каталогу: 308503', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 23, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 37 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 37 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', [Description] = N'ID по каталогу: 308503', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 23, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 37 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 28 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 28 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', N'Тип/марка: 296204-4E0000-ОП-ПЗ.ОЛ17
ID по каталогу: 306904', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 24, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 28 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 28 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', [Description] = N'Тип/марка: 296204-4E0000-ОП-ПЗ.ОЛ17
ID по каталогу: 306904', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 24, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 28 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 34 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 34 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', N'Тип/марка: 296204-4E0000-ОП-ПЗ.ОЛ7
ID по каталогу: 306907', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 25, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 34 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 34 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', [Description] = N'Тип/марка: 296204-4E0000-ОП-ПЗ.ОЛ7
ID по каталогу: 306907', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 25, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 34 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 60/20 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования и горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный опорный г/п 60/20 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования и горячекатаных рулонов', N'Тип/марка: 296204-4C0000-ОП-ПЗ.ОЛ19
ID по каталогу: 308514', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 26, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 60/20 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования и горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный опорный г/п 60/20 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования и горячекатаных рулонов', [Description] = N'Тип/марка: 296204-4C0000-ОП-ПЗ.ОЛ19
ID по каталогу: 308514', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 26, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 60/20 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования и горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 60/25 т Пролет 28 м, Н=13 м, ГЗМ - однорогий крюк; Назначение – транспортировка валков и валковых опор', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой двухбалочный опорный г/п 60/25 т Пролет 28 м, Н=13 м, ГЗМ - однорогий крюк; Назначение – транспортировка валков и валковых опор', N'Тип/марка: 296204-4D0000-ОП-ПЗ.ОЛ13
ID по каталогу: 308506', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 27, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 60/25 т Пролет 28 м, Н=13 м, ГЗМ - однорогий крюк; Назначение – транспортировка валков и валковых опор', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой двухбалочный опорный г/п 60/25 т Пролет 28 м, Н=13 м, ГЗМ - однорогий крюк; Назначение – транспортировка валков и валковых опор', [Description] = N'Тип/марка: 296204-4D0000-ОП-ПЗ.ОЛ13
ID по каталогу: 308506', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 27, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 60/25 т Пролет 28 м, Н=13 м, ГЗМ - однорогий крюк; Назначение – транспортировка валков и валковых опор', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой клещевой двухбалочный г/п 120 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А8, ГЗМ - клещи г/п 120 т с электромеханическим приводом, крюк основной траверсы г/п 80 т; Назначение – транспортировка слябов (выгрузка/загрузка на рольганг).', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой клещевой двухбалочный г/п 120 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А8, ГЗМ - клещи г/п 120 т с электромеханическим приводом, крюк основной траверсы г/п 80 т; Назначение – транспортировка слябов (выгрузка/загрузка на рольганг).', N'Тип/марка: 296204-4А0000-ОП-ПЗ.ОЛ2
ID по каталогу: 304996', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 28, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой клещевой двухбалочный г/п 120 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А8, ГЗМ - клещи г/п 120 т с электромеханическим приводом, крюк основной траверсы г/п 80 т; Назначение – транспортировка слябов (выгрузка/загрузка на рольганг).', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой клещевой двухбалочный г/п 120 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А8, ГЗМ - клещи г/п 120 т с электромеханическим приводом, крюк основной траверсы г/п 80 т; Назначение – транспортировка слябов (выгрузка/загрузка на рольганг).', [Description] = N'Тип/марка: 296204-4А0000-ОП-ПЗ.ОЛ2
ID по каталогу: 304996', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 28, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой клещевой двухбалочный г/п 120 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А8, ГЗМ - клещи г/п 120 т с электромеханическим приводом, крюк основной траверсы г/п 80 т; Назначение – транспортировка слябов (выгрузка/загрузка на рольганг).', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой ручной однобалочный подвесной г/п 1,0т, пролет крана- 4,2 м, общая длина крана-5,4м, расстояние от оси тягового колеса 3700мм Категория размещения У3. Исполнение взрывозащищенное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой ручной однобалочный подвесной г/п 1,0т, пролет крана- 4,2 м, общая длина крана-5,4м, расстояние от оси тягового колеса 3700мм Категория размещения У3. Исполнение взрывозащищенное.', N'Тип/марка: 295243-02-06-ТХ.ОЛ7
ID по каталогу: 303535
ID обор. полный: 77811', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 29, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой ручной однобалочный подвесной г/п 1,0т, пролет крана- 4,2 м, общая длина крана-5,4м, расстояние от оси тягового колеса 3700мм Категория размещения У3. Исполнение взрывозащищенное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой ручной однобалочный подвесной г/п 1,0т, пролет крана- 4,2 м, общая длина крана-5,4м, расстояние от оси тягового колеса 3700мм Категория размещения У3. Исполнение взрывозащищенное.', [Description] = N'Тип/марка: 295243-02-06-ТХ.ОЛ7
ID по каталогу: 303535
ID обор. полный: 77811', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 29, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой ручной однобалочный подвесной г/п 1,0т, пролет крана- 4,2 м, общая длина крана-5,4м, расстояние от оси тягового колеса 3700мм Категория размещения У3. Исполнение взрывозащищенное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной г/п 3,2т, пролет 9м, высота подъема 8 м; полная длина 11 м. Температура окружающей среды -20...+40 град.С Исполнение общепромышленное. Режим работы - А3.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой электрический однобалочный подвесной г/п 3,2т, пролет 9м, высота подъема 8 м; полная длина 11 м. Температура окружающей среды -20...+40 град.С Исполнение общепромышленное. Режим работы - А3.', N'ID по каталогу: 76480', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 30, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной г/п 3,2т, пролет 9м, высота подъема 8 м; полная длина 11 м. Температура окружающей среды -20...+40 град.С Исполнение общепромышленное. Режим работы - А3.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой электрический однобалочный подвесной г/п 3,2т, пролет 9м, высота подъема 8 м; полная длина 11 м. Температура окружающей среды -20...+40 град.С Исполнение общепромышленное. Режим работы - А3.', [Description] = N'ID по каталогу: 76480', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 30, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной г/п 3,2т, пролет 9м, высота подъема 8 м; полная длина 11 м. Температура окружающей среды -20...+40 град.С Исполнение общепромышленное. Режим работы - А3.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной общепромышленного исполнения. В комплекте с талью производства Болгария Климатическое исполнение У3. Режим работы А3. Температура окружающей среды -20 +40', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой электрический однобалочный подвесной общепромышленного исполнения. В комплекте с талью производства Болгария Климатическое исполнение У3. Режим работы А3. Температура окружающей среды -20 +40', N'ID по каталогу: 65798', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 31, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной общепромышленного исполнения. В комплекте с талью производства Болгария Климатическое исполнение У3. Режим работы А3. Температура окружающей среды -20 +40', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой электрический однобалочный подвесной общепромышленного исполнения. В комплекте с талью производства Болгария Климатическое исполнение У3. Режим работы А3. Температура окружающей среды -20 +40', [Description] = N'ID по каталогу: 65798', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 31, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной общепромышленного исполнения. В комплекте с талью производства Болгария Климатическое исполнение У3. Режим работы А3. Температура окружающей среды -20 +40', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной общепромышленного испонения. Таль с малой строительной высотой Т320-511', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мостовой электрический однобалочный подвесной общепромышленного испонения. Таль с малой строительной высотой Т320-511', N'Тип/марка: 3,2-7,8- 6 - 6 380-У3 (1Т -Т320-511 (6,3)2РМсв- 1ПТО)
ID по каталогу: 10857', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 32, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной общепромышленного испонения. Таль с малой строительной высотой Т320-511', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мостовой электрический однобалочный подвесной общепромышленного испонения. Таль с малой строительной высотой Т320-511', [Description] = N'Тип/марка: 3,2-7,8- 6 - 6 380-У3 (1Т -Т320-511 (6,3)2РМсв- 1ПТО)
ID по каталогу: 10857', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 32, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной общепромышленного испонения. Таль с малой строительной высотой Т320-511', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мотсовой электрический однобалочный г/п 5 т. Пролет 15 м, консоли 2х0,6 м, высота подъема 12 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран мотсовой электрический однобалочный г/п 5 т. Пролет 15 м, консоли 2х0,6 м, высота подъема 12 м', N'Тип/марка: 294341-ОПР1.ОЛ5
ID по каталогу: 252671', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 33, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мотсовой электрический однобалочный г/п 5 т. Пролет 15 м, консоли 2х0,6 м, высота подъема 12 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран мотсовой электрический однобалочный г/п 5 т. Пролет 15 м, консоли 2х0,6 м, высота подъема 12 м', [Description] = N'Тип/марка: 294341-ОПР1.ОЛ5
ID по каталогу: 252671', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 33, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мотсовой электрический однобалочный г/п 5 т. Пролет 15 м, консоли 2х0,6 м, высота подъема 12 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 10 т Пролет 15 м, Н=14 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран подвесной однобалочный г/п 10 т Пролет 15 м, Н=14 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', N'Тип/марка: 296204-4К0000-ОП-ПЗ1.ОЛ2
ID по каталогу: 306501
ID обор. полный: 306504', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 34, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 10 т Пролет 15 м, Н=14 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран подвесной однобалочный г/п 10 т Пролет 15 м, Н=14 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', [Description] = N'Тип/марка: 296204-4К0000-ОП-ПЗ1.ОЛ2
ID по каталогу: 306501
ID обор. полный: 306504', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 34, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 10 т Пролет 15 м, Н=14 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 10 т Пролет 6 м, Н=18 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран подвесной однобалочный г/п 10 т Пролет 6 м, Н=18 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', N'Тип/марка: 296204-4А0000-ОП-ПЗ.ОЛ5
ID по каталогу: 304999', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 35, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 10 т Пролет 6 м, Н=18 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран подвесной однобалочный г/п 10 т Пролет 6 м, Н=18 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', [Description] = N'Тип/марка: 296204-4А0000-ОП-ПЗ.ОЛ5
ID по каталогу: 304999', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 35, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 10 т Пролет 6 м, Н=18 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 12 м, Н=9 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран подвесной однобалочный г/п 5 т Пролет 12 м, Н=9 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', N'Тип/марка: 296204-4К0000-ОП-ПЗ1.ОЛ1
ID по каталогу: 306504
ID обор. полный: 306501', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 36, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 12 м, Н=9 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран подвесной однобалочный г/п 5 т Пролет 12 м, Н=9 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', [Description] = N'Тип/марка: 296204-4К0000-ОП-ПЗ1.ОЛ1
ID по каталогу: 306504
ID обор. полный: 306501', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 36, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 12 м, Н=9 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 22 м, Н=912м, ГЗМ - крюк; Назначение – ремонт и обслуживание фильтров.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран подвесной однобалочный г/п 5 т Пролет 22 м, Н=912м, ГЗМ - крюк; Назначение – ремонт и обслуживание фильтров.', N'Тип/марка: 296204-4H0000-ОП-ПЗ.ОЛ1
ID по каталогу: 307122', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 37, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 22 м, Н=912м, ГЗМ - крюк; Назначение – ремонт и обслуживание фильтров.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран подвесной однобалочный г/п 5 т Пролет 22 м, Н=912м, ГЗМ - крюк; Назначение – ремонт и обслуживание фильтров.', [Description] = N'Тип/марка: 296204-4H0000-ОП-ПЗ.ОЛ1
ID по каталогу: 307122', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 37, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 22 м, Н=912м, ГЗМ - крюк; Назначение – ремонт и обслуживание фильтров.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 6 м, Н=12 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран подвесной однобалочный г/п 5 т Пролет 6 м, Н=12 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', N'Тип/марка: 296204-4Е0000-ОП-ПЗ.ОЛ8
ID по каталогу: 308499', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 38, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 6 м, Н=12 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран подвесной однобалочный г/п 5 т Пролет 6 м, Н=12 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', [Description] = N'Тип/марка: 296204-4Е0000-ОП-ПЗ.ОЛ8
ID по каталогу: 308499', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 38, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 6 м, Н=12 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п3,2т Пролет 12 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран подвесной однобалочный г/п3,2т Пролет 12 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', N'Тип/марка: 296204-4С0000-ОП-ПЗ.ОЛ29
ID по каталогу: 309378', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 39, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п3,2т Пролет 12 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран подвесной однобалочный г/п3,2т Пролет 12 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', [Description] = N'Тип/марка: 296204-4С0000-ОП-ПЗ.ОЛ29
ID по каталогу: 309378', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 39, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п3,2т Пролет 12 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п3,2т Пролет 9 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран подвесной однобалочный г/п3,2т Пролет 9 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', N'Тип/марка: 296204-4С0000-ОП-ПЗ.ОЛ30
ID по каталогу: 309379', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 40, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п3,2т Пролет 9 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран подвесной однобалочный г/п3,2т Пролет 9 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', [Description] = N'Тип/марка: 296204-4С0000-ОП-ПЗ.ОЛ30
ID по каталогу: 309379', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 40, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п3,2т Пролет 9 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран полукозловой электрический г/п 25 т Пролет 10 м (при ширине пролёта - 30 м); Н=7,3 м, ГЗМ - крюк; Назначение – обслуживание валковых опор.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран полукозловой электрический г/п 25 т Пролет 10 м (при ширине пролёта - 30 м); Н=7,3 м, ГЗМ - крюк; Назначение – обслуживание валковых опор.', N'Тип/марка: 296204-4D0000-ОП-ПЗ.ОЛ14
ID по каталогу: 308507', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 41, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран полукозловой электрический г/п 25 т Пролет 10 м (при ширине пролёта - 30 м); Н=7,3 м, ГЗМ - крюк; Назначение – обслуживание валковых опор.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран полукозловой электрический г/п 25 т Пролет 10 м (при ширине пролёта - 30 м); Н=7,3 м, ГЗМ - крюк; Назначение – обслуживание валковых опор.', [Description] = N'Тип/марка: 296204-4D0000-ОП-ПЗ.ОЛ14
ID по каталогу: 308507', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 41, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран полукозловой электрический г/п 25 т Пролет 10 м (при ширине пролёта - 30 м); Н=7,3 м, ГЗМ - крюк; Назначение – обслуживание валковых опор.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран полукозловой электрический магнитный г/п на магнитах 50 т Пролет 14 м (при ширине пролета склада 33 м), Н=3,25 м, ГЗМ - грузовые электромагниты 4 шт. с общей грузо-подъемностью 50 т; Назначение – транспортировка и кантовка сляба на площадке обработки слябов.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран полукозловой электрический магнитный г/п на магнитах 50 т Пролет 14 м (при ширине пролета склада 33 м), Н=3,25 м, ГЗМ - грузовые электромагниты 4 шт. с общей грузо-подъемностью 50 т; Назначение – транспортировка и кантовка сляба на площадке обработки слябов.', N'Тип/марка: 296204-4А0000-ОП-ПЗ.ОЛ3
ID по каталогу: 304997', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 42, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран полукозловой электрический магнитный г/п на магнитах 50 т Пролет 14 м (при ширине пролета склада 33 м), Н=3,25 м, ГЗМ - грузовые электромагниты 4 шт. с общей грузо-подъемностью 50 т; Назначение – транспортировка и кантовка сляба на площадке обработки слябов.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран полукозловой электрический магнитный г/п на магнитах 50 т Пролет 14 м (при ширине пролета склада 33 м), Н=3,25 м, ГЗМ - грузовые электромагниты 4 шт. с общей грузо-подъемностью 50 т; Назначение – транспортировка и кантовка сляба на площадке обработки слябов.', [Description] = N'Тип/марка: 296204-4А0000-ОП-ПЗ.ОЛ3
ID по каталогу: 304997', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 42, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран полукозловой электрический магнитный г/п на магнитах 50 т Пролет 14 м (при ширине пролета склада 33 м), Н=3,25 м, ГЗМ - грузовые электромагниты 4 шт. с общей грузо-подъемностью 50 т; Назначение – транспортировка и кантовка сляба на площадке обработки слябов.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран трехходовой для подключения манометра Ду15 Ру16 с ручкой материал - латунь рабочая среда - вода температура среды - -40°C до +40°C герметичность - класс "D" по ГОСТ 9544; климатическое исполнение – У3 по гост 15150 тип присоединения - резьба внутренняя 1/2"', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран трехходовой для подключения манометра Ду15 Ру16 с ручкой материал - латунь рабочая среда - вода температура среды - -40°C до +40°C герметичность - класс "D" по ГОСТ 9544; климатическое исполнение – У3 по гост 15150 тип присоединения - резьба внутренняя 1/2"', N'Тип/марка: 11б18бк
ID по каталогу: 291975', NULL, NULL, 1, 0, NULL, 43, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран трехходовой для подключения манометра Ду15 Ру16 с ручкой материал - латунь рабочая среда - вода температура среды - -40°C до +40°C герметичность - класс "D" по ГОСТ 9544; климатическое исполнение – У3 по гост 15150 тип присоединения - резьба внутренняя 1/2"', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран трехходовой для подключения манометра Ду15 Ру16 с ручкой материал - латунь рабочая среда - вода температура среды - -40°C до +40°C герметичность - класс "D" по ГОСТ 9544; климатическое исполнение – У3 по гост 15150 тип присоединения - резьба внутренняя 1/2"', [Description] = N'Тип/марка: 11б18бк
ID по каталогу: 291975', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 43, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран трехходовой для подключения манометра Ду15 Ру16 с ручкой материал - латунь рабочая среда - вода температура среды - -40°C до +40°C герметичность - класс "D" по ГОСТ 9544; климатическое исполнение – У3 по гост 15150 тип присоединения - резьба внутренняя 1/2"', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой DN80 PN16 стальной фланцевый полнопроходный со стальной рукояткой в комплекте с ответными фланцами, крепежом и прокладкам материал - сталь углеродистая рабочая среда - вода температура среды - -40...+200С герметичность - класс "А" по ГОСТ 9544 климатическое исполнение – У по гост 15150', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровой DN80 PN16 стальной фланцевый полнопроходный со стальной рукояткой в комплекте с ответными фланцами, крепежом и прокладкам материал - сталь углеродистая рабочая среда - вода температура среды - -40...+200С герметичность - класс "А" по ГОСТ 9544 климатическое исполнение – У по гост 15150', N'Здание склада слябов
Тип/марка: КШТ15.80.16 Ф/Ф
ID по каталогу: 298360', NULL, NULL, 1, 0, NULL, 44, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой DN80 PN16 стальной фланцевый полнопроходный со стальной рукояткой в комплекте с ответными фланцами, крепежом и прокладкам материал - сталь углеродистая рабочая среда - вода температура среды - -40...+200С герметичность - класс "А" по ГОСТ 9544 климатическое исполнение – У по гост 15150', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровой DN80 PN16 стальной фланцевый полнопроходный со стальной рукояткой в комплекте с ответными фланцами, крепежом и прокладкам материал - сталь углеродистая рабочая среда - вода температура среды - -40...+200С герметичность - класс "А" по ГОСТ 9544 климатическое исполнение – У по гост 15150', [Description] = N'Здание склада слябов
Тип/марка: КШТ15.80.16 Ф/Ф
ID по каталогу: 298360', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 44, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой DN80 PN16 стальной фланцевый полнопроходный со стальной рукояткой в комплекте с ответными фланцами, крепежом и прокладкам материал - сталь углеродистая рабочая среда - вода температура среды - -40...+200С герметичность - класс "А" по ГОСТ 9544 климатическое исполнение – У по гост 15150', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой стальной муфтовый Ду = 25 мм, Ру = 1,6 МПа Раб. среда - вода.', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровой стальной муфтовый Ду = 25 мм, Ру = 1,6 МПа Раб. среда - вода.', N'ID по каталогу: 170979', NULL, NULL, 1, 0, NULL, 45, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой стальной муфтовый Ду = 25 мм, Ру = 1,6 МПа Раб. среда - вода.', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровой стальной муфтовый Ду = 25 мм, Ру = 1,6 МПа Раб. среда - вода.', [Description] = N'ID по каталогу: 170979', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 45, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой стальной муфтовый Ду = 25 мм, Ру = 1,6 МПа Раб. среда - вода.', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой фланцевый Ду 40 мм; Ру1,6 МПа. Среда - вода. Температура до150 °С. Корпус и шар - нержавеющая сталь; Уплотнение - фторопласт. Ручное управление. Класс герметичности "А". Климатическое исполнение - У2. С КОФ, прокладками и крепежом.', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровой фланцевый Ду 40 мм; Ру1,6 МПа. Среда - вода. Температура до150 °С. Корпус и шар - нержавеющая сталь; Уплотнение - фторопласт. Ручное управление. Класс герметичности "А". Климатическое исполнение - У2. С КОФ, прокладками и крепежом.', N'ID по каталогу: 188811', NULL, NULL, 1, 0, NULL, 46, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой фланцевый Ду 40 мм; Ру1,6 МПа. Среда - вода. Температура до150 °С. Корпус и шар - нержавеющая сталь; Уплотнение - фторопласт. Ручное управление. Класс герметичности "А". Климатическое исполнение - У2. С КОФ, прокладками и крепежом.', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровой фланцевый Ду 40 мм; Ру1,6 МПа. Среда - вода. Температура до150 °С. Корпус и шар - нержавеющая сталь; Уплотнение - фторопласт. Ручное управление. Класс герметичности "А". Климатическое исполнение - У2. С КОФ, прокладками и крепежом.', [Description] = N'ID по каталогу: 188811', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 46, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой фланцевый Ду 40 мм; Ру1,6 МПа. Среда - вода. Температура до150 °С. Корпус и шар - нержавеющая сталь; Уплотнение - фторопласт. Ручное управление. Класс герметичности "А". Климатическое исполнение - У2. С КОФ, прокладками и крепежом.', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка - бабочка. Присоединение резьба В-В. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка - бабочка. Присоединение резьба В-В. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', N'Здание склада слябов, встроенные помещения
Тип/марка: 11Б27п1
ID по каталогу: 254262', NULL, NULL, 1, 0, NULL, 47, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка - бабочка. Присоединение резьба В-В. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка - бабочка. Присоединение резьба В-В. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', [Description] = N'Здание склада слябов, встроенные помещения
Тип/марка: 11Б27п1
ID по каталогу: 254262', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 47, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка - бабочка. Присоединение резьба В-В. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', N'Тип/марка: 11Б27п1
ID по каталогу: 189541', NULL, NULL, 1, 0, NULL, 48, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', [Description] = N'Тип/марка: 11Б27п1
ID по каталогу: 189541', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 48, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 20, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровый латунный муфтовый DN 20, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', N'Тип/марка: 11Б27п1
ID по каталогу: 189280', NULL, NULL, 1, 0, NULL, 49, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 20, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровый латунный муфтовый DN 20, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', [Description] = N'Тип/марка: 11Б27п1
ID по каталогу: 189280', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 49, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 20, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 25, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровый латунный муфтовый DN 25, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', N'Тип/марка: 11Б27п1
ID по каталогу: 189281', NULL, NULL, 1, 0, NULL, 50, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 25, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровый латунный муфтовый DN 25, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', [Description] = N'Тип/марка: 11Б27п1
ID по каталогу: 189281', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 50, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 25, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду25; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2005 Класс А -, Материал (корпуса) Латунь ЛЦ40СД -, Температура рабочей среды Вода до 150. гр.С', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровый муфтовый проходной Ду25; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2005 Класс А -, Материал (корпуса) Латунь ЛЦ40СД -, Температура рабочей среды Вода до 150. гр.С', N'Здание склада слябов, встроенные помещения
Тип/марка: 11Б27п1
ID по каталогу: 96139', NULL, NULL, 1, 0, NULL, 51, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду25; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2005 Класс А -, Материал (корпуса) Латунь ЛЦ40СД -, Температура рабочей среды Вода до 150. гр.С', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровый муфтовый проходной Ду25; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2005 Класс А -, Материал (корпуса) Латунь ЛЦ40СД -, Температура рабочей среды Вода до 150. гр.С', [Description] = N'Здание склада слябов, встроенные помещения
Тип/марка: 11Б27п1
ID по каталогу: 96139', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 51, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду25; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2005 Класс А -, Материал (корпуса) Латунь ЛЦ40СД -, Температура рабочей среды Вода до 150. гр.С', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду32; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровый муфтовый проходной Ду32; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', N'Тип/марка: 11Б27п1
ID по каталогу: 237098', NULL, NULL, 1, 0, NULL, 52, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду32; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровый муфтовый проходной Ду32; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', [Description] = N'Тип/марка: 11Б27п1
ID по каталогу: 237098', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 52, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду32; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду50; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровый муфтовый проходной Ду50; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', N'Тип/марка: 11Б27п1
ID по каталогу: 180634', NULL, NULL, 1, 0, NULL, 53, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду50; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровый муфтовый проходной Ду50; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', [Description] = N'Тип/марка: 11Б27п1
ID по каталогу: 180634', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 53, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду50; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый стальной муфтовый материал корпуса- сталь рабочая среда - техническая вода температура рабочей среды +5...+40 град С герметичность - класс "А" по ГОСТ 9544-2015. климатическое исполнение У1.', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран шаровый стальной муфтовый материал корпуса- сталь рабочая среда - техническая вода температура рабочей среды +5...+40 град С герметичность - класс "А" по ГОСТ 9544-2015. климатическое исполнение У1.', N'Тип/марка: КШ Ду 50 Ру16 кгс/см2
ID по каталогу: 300184', NULL, NULL, 1, 0, NULL, 54, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый стальной муфтовый материал корпуса- сталь рабочая среда - техническая вода температура рабочей среды +5...+40 град С герметичность - класс "А" по ГОСТ 9544-2015. климатическое исполнение У1.', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран шаровый стальной муфтовый материал корпуса- сталь рабочая среда - техническая вода температура рабочей среды +5...+40 град С герметичность - класс "А" по ГОСТ 9544-2015. климатическое исполнение У1.', [Description] = N'Тип/марка: КШ Ду 50 Ру16 кгс/см2
ID по каталогу: 300184', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 54, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый стальной муфтовый материал корпуса- сталь рабочая среда - техническая вода температура рабочей среды +5...+40 град С герметичность - класс "А" по ГОСТ 9544-2015. климатическое исполнение У1.', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический мостовой опорный г/п 15т.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран электрический мостовой опорный г/п 15т.', N'Тип/марка: 296204-4I0000-ОП-ПЗ2.ОЛ
ID по каталогу: 313921
ID обор. полный: 309751', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 55, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический мостовой опорный г/п 15т.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран электрический мостовой опорный г/п 15т.', [Description] = N'Тип/марка: 296204-4I0000-ОП-ПЗ2.ОЛ
ID по каталогу: 313921
ID обор. полный: 309751', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 55, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический мостовой опорный г/п 15т.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арматуры и др. в помещении комплекса водоподготовки.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арматуры и др. в помещении комплекса водоподготовки.', N'Тип/марка: 296204-4J0000-ОП-ПЗ2.ОЛ
ID по каталогу: 309308', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 56, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арматуры и др. в помещении комплекса водоподготовки.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арматуры и др. в помещении комплекса водоподготовки.', [Description] = N'Тип/марка: 296204-4J0000-ОП-ПЗ2.ОЛ
ID по каталогу: 309308', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 56, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арматуры и др. в помещении комплекса водоподготовки.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арма-туры и др. в помещении насосной станции.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арма-туры и др. в помещении насосной станции.', N'Тип/марка: 296204-4L0000-ОП-ПЗ2.ОЛ
ID по каталогу: 308843', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 57, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арма-туры и др. в помещении насосной станции.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арма-туры и др. в помещении насосной станции.', [Description] = N'Тип/марка: 296204-4L0000-ОП-ПЗ2.ОЛ
ID по каталогу: 308843', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 57, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арма-туры и др. в помещении насосной станции.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран-балка г/п 10т для обслуживания площадки балансиров', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Кран-балка г/п 10т для обслуживания площадки балансиров', N'Тип/марка: Спецификация №4100068799 от 10.09.2019 (23.09.2019г)
291534-01-ТЗ1
ID по каталогу: 176060', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 58, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран-балка г/п 10т для обслуживания площадки балансиров', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Кран-балка г/п 10т для обслуживания площадки балансиров', [Description] = N'Тип/марка: Спецификация №4100068799 от 10.09.2019 (23.09.2019г)
291534-01-ТЗ1
ID по каталогу: 176060', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 58, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран-балка г/п 10т для обслуживания площадки балансиров', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Лебедка ручная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Лебедка ручная', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 59, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Лебедка ручная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Лебедка ручная', [Description] = N'', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 59, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Лебедка ручная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ленточнопильный станок с ЧПУ', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Ленточнопильный станок с ЧПУ', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 60, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ленточнопильный станок с ЧПУ', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Ленточнопильный станок с ЧПУ', [Description] = N'', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 60, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ленточнопильный станок с ЧПУ', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Машина для ударных испытаний, в комплекте с устройством для охлаждения образцов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Машина для ударных испытаний, в комплекте с устройством для охлаждения образцов', N'Машина для ударных испытаний (ручная)', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 61, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Машина для ударных испытаний, в комплекте с устройством для охлаждения образцов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Машина для ударных испытаний, в комплекте с устройством для охлаждения образцов', [Description] = N'Машина для ударных испытаний (ручная)', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 61, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Машина для ударных испытаний, в комплекте с устройством для охлаждения образцов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Машина переносная газовой резки', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Машина переносная газовой резки', N'Тип/марка: НОРД-С 300 (АТМ157)
ID по каталогу: 307111', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 62, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Машина переносная газовой резки', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Машина переносная газовой резки', [Description] = N'Тип/марка: НОРД-С 300 (АТМ157)
ID по каталогу: 307111', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 62, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Машина переносная газовой резки', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Механическая задвижка DN 820 с электроприводом', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Механическая задвижка DN 820 с электроприводом', N'ID по каталогу: 210480', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 63, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Механическая задвижка DN 820 с электроприводом', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Механическая задвижка DN 820 с электроприводом', [Description] = N'ID по каталогу: 210480', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 63, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Механическая задвижка DN 820 с электроприводом', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная автоматическая насосная станция. Рабочая среда - питьевая вода.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Модульная автоматическая насосная станция. Рабочая среда - питьевая вода.', N'Тип/марка: 296204-ОЛ1
ID по каталогу: 284315', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 64, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная автоматическая насосная станция. Рабочая среда - питьевая вода.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Модульная автоматическая насосная станция. Рабочая среда - питьевая вода.', [Description] = N'Тип/марка: 296204-ОЛ1
ID по каталогу: 284315', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 64, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная автоматическая насосная станция. Рабочая среда - питьевая вода.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная вытяжная установка L=19000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Модульная вытяжная установка L=19000 м3/ч', N'Электротехническое помещение
Тип/марка: Спецификация №4100096387 от 18.03.2021г.
 (291553-ИОС4.УС.ОЛ22 изм.1)
ID по каталогу: 192564', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 65, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная вытяжная установка L=19000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Модульная вытяжная установка L=19000 м3/ч', [Description] = N'Электротехническое помещение
Тип/марка: Спецификация №4100096387 от 18.03.2021г.
 (291553-ИОС4.УС.ОЛ22 изм.1)
ID по каталогу: 192564', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 65, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная вытяжная установка L=19000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная компрессорная станция (МКС). Производительность станции по осушенному сжатому воздуху на выходе - 332 Нм3/мин.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Модульная компрессорная станция (МКС). Производительность станции по осушенному сжатому воздуху на выходе - 332 Нм3/мин.', N'ID по каталогу: 304125', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 66, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная компрессорная станция (МКС). Производительность станции по осушенному сжатому воздуху на выходе - 332 Нм3/мин.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Модульная компрессорная станция (МКС). Производительность станции по осушенному сжатому воздуху на выходе - 332 Нм3/мин.', [Description] = N'ID по каталогу: 304125', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 66, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная компрессорная станция (МКС). Производительность станции по осушенному сжатому воздуху на выходе - 332 Нм3/мин.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная приточная установка, расход воздуха L=500 м3/ч, сторона обслуживания - снизу, со встроенной автоматикой.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Модульная приточная установка, расход воздуха L=500 м3/ч, сторона обслуживания - снизу, со встроенной автоматикой.', N'Встроенные помещения
Тип/марка: 290300-01-ОВ3.ОЛ1
ID по каталогу: 279895', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 67, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная приточная установка, расход воздуха L=500 м3/ч, сторона обслуживания - снизу, со встроенной автоматикой.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Модульная приточная установка, расход воздуха L=500 м3/ч, сторона обслуживания - снизу, со встроенной автоматикой.', [Description] = N'Встроенные помещения
Тип/марка: 290300-01-ОВ3.ОЛ1
ID по каталогу: 279895', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 67, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная приточная установка, расход воздуха L=500 м3/ч, сторона обслуживания - снизу, со встроенной автоматикой.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная станция осушки сжатого воздуха Устанавливается на улице. В составе: адсорбционный осушитель с горячей регенерацией Atlas Copco BD3000 - 2 шт. (1 раб.,1 рез.), набор фильтров ( PD, BD,фильтр вихревой) на каждый осушитель Класс чистоты выздуха на входе - 7:9:1 по ГОСТ Р ИСО 8573-1-2016 Класс чистоты воздуха на выходе - 1:2:1 по ГОСТ Р ИСО 8573-1-2016 Рабочее давление 6-8 бар Температура воздуха на входе до +40°С Производительность - 10000 м3/час', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Модульная станция осушки сжатого воздуха Устанавливается на улице. В составе: адсорбционный осушитель с горячей регенерацией Atlas Copco BD3000 - 2 шт. (1 раб.,1 рез.), набор фильтров ( PD, BD,фильтр вихревой) на каждый осушитель Класс чистоты выздуха на входе - 7:9:1 по ГОСТ Р ИСО 8573-1-2016 Класс чистоты воздуха на выходе - 1:2:1 по ГОСТ Р ИСО 8573-1-2016 Рабочее давление 6-8 бар Температура воздуха на входе до +40°С Производительность - 10000 м3/час', N'Тип/марка: 371.83СБНУ
ID по каталогу: 283775', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 68, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная станция осушки сжатого воздуха Устанавливается на улице. В составе: адсорбционный осушитель с горячей регенерацией Atlas Copco BD3000 - 2 шт. (1 раб.,1 рез.), набор фильтров ( PD, BD,фильтр вихревой) на каждый осушитель Класс чистоты выздуха на входе - 7:9:1 по ГОСТ Р ИСО 8573-1-2016 Класс чистоты воздуха на выходе - 1:2:1 по ГОСТ Р ИСО 8573-1-2016 Рабочее давление 6-8 бар Температура воздуха на входе до +40°С Производительность - 10000 м3/час', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Модульная станция осушки сжатого воздуха Устанавливается на улице. В составе: адсорбционный осушитель с горячей регенерацией Atlas Copco BD3000 - 2 шт. (1 раб.,1 рез.), набор фильтров ( PD, BD,фильтр вихревой) на каждый осушитель Класс чистоты выздуха на входе - 7:9:1 по ГОСТ Р ИСО 8573-1-2016 Класс чистоты воздуха на выходе - 1:2:1 по ГОСТ Р ИСО 8573-1-2016 Рабочее давление 6-8 бар Температура воздуха на входе до +40°С Производительность - 10000 м3/час', [Description] = N'Тип/марка: 371.83СБНУ
ID по каталогу: 283775', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 68, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная станция осушки сжатого воздуха Устанавливается на улице. В составе: адсорбционный осушитель с горячей регенерацией Atlas Copco BD3000 - 2 шт. (1 раб.,1 рез.), набор фильтров ( PD, BD,фильтр вихревой) на каждый осушитель Класс чистоты выздуха на входе - 7:9:1 по ГОСТ Р ИСО 8573-1-2016 Класс чистоты воздуха на выходе - 1:2:1 по ГОСТ Р ИСО 8573-1-2016 Рабочее давление 6-8 бар Температура воздуха на входе до +40°С Производительность - 10000 м3/час', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульное бытовое помещение и ремонтная мастерская', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Модульное бытовое помещение и ремонтная мастерская', N'ID по каталогу: 304453', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 69, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульное бытовое помещение и ремонтная мастерская', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Модульное бытовое помещение и ремонтная мастерская', [Description] = N'ID по каталогу: 304453', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 69, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульное бытовое помещение и ремонтная мастерская', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульное здание АБК', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Модульное здание АБК', N'ID по каталогу: 305300', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 70, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульное здание АБК', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Модульное здание АБК', [Description] = N'ID по каталогу: 305300', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 70, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульное здание АБК', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительная емкость для сбора бытовых стоков V=15 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Накопительная емкость для сбора бытовых стоков V=15 м3', N'Тип/марка: 296204-4Y0000-ОП-ПЗ.ОЛ5
ID по каталогу: 314759', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 71, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительная емкость для сбора бытовых стоков V=15 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Накопительная емкость для сбора бытовых стоков V=15 м3', [Description] = N'Тип/марка: 296204-4Y0000-ОП-ПЗ.ОЛ5
ID по каталогу: 314759', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 71, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительная емкость для сбора бытовых стоков V=15 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительная емкость для сбора дождевых стоков V=35 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Накопительная емкость для сбора дождевых стоков V=35 м3', N'Тип/марка: 296204-4Y0000-ОП-ПЗ.ОЛ6
ID по каталогу: 314760', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 72, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительная емкость для сбора дождевых стоков V=35 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Накопительная емкость для сбора дождевых стоков V=35 м3', [Description] = N'Тип/марка: 296204-4Y0000-ОП-ПЗ.ОЛ6
ID по каталогу: 314760', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 72, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительная емкость для сбора дождевых стоков V=35 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительный бак для сбора масла', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Накопительный бак для сбора масла', N'ОТ401', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 73, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительный бак для сбора масла', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Накопительный бак для сбора масла', [Description] = N'ОТ401', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 73, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительный бак для сбора масла', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос для аварийных стоков', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насос для аварийных стоков', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 74, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос для аварийных стоков', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насос для аварийных стоков', [Description] = N'', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 74, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос для аварийных стоков', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос дренажный погружной моноблочный для грязной воды подача (номин.) - 10 м3/ч; напор - 10 м; мощность двигателя - 1 кВт; частота вращения - 3000 об/мин; ток- 8 А; напряжение сети- 220 В; Комплект поставки: электродвигатель, поплавковый выключатель, шнур питания L=10м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насос дренажный погружной моноблочный для грязной воды подача (номин.) - 10 м3/ч; напор - 10 м; мощность двигателя - 1 кВт; частота вращения - 3000 об/мин; ток- 8 А; напряжение сети- 220 В; Комплект поставки: электродвигатель, поплавковый выключатель, шнур питания L=10м', N'Тип/марка: ГНОМ 10-10Д
ID по каталогу: 177595', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 75, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос дренажный погружной моноблочный для грязной воды подача (номин.) - 10 м3/ч; напор - 10 м; мощность двигателя - 1 кВт; частота вращения - 3000 об/мин; ток- 8 А; напряжение сети- 220 В; Комплект поставки: электродвигатель, поплавковый выключатель, шнур питания L=10м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насос дренажный погружной моноблочный для грязной воды подача (номин.) - 10 м3/ч; напор - 10 м; мощность двигателя - 1 кВт; частота вращения - 3000 об/мин; ток- 8 А; напряжение сети- 220 В; Комплект поставки: электродвигатель, поплавковый выключатель, шнур питания L=10м', [Description] = N'Тип/марка: ГНОМ 10-10Д
ID по каталогу: 177595', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 75, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос дренажный погружной моноблочный для грязной воды подача (номин.) - 10 м3/ч; напор - 10 м; мощность двигателя - 1 кВт; частота вращения - 3000 об/мин; ток- 8 А; напряжение сети- 220 В; Комплект поставки: электродвигатель, поплавковый выключатель, шнур питания L=10м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос дренажныйй погружной моноблочный для грязной воды с поплавковым выключателем Q= 6 м3/ч, Н=10 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насос дренажныйй погружной моноблочный для грязной воды с поплавковым выключателем Q= 6 м3/ч, Н=10 м', N'Тип/марка: ГНОМ 6-10Д
ID по каталогу: 215243', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 76, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос дренажныйй погружной моноблочный для грязной воды с поплавковым выключателем Q= 6 м3/ч, Н=10 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насос дренажныйй погружной моноблочный для грязной воды с поплавковым выключателем Q= 6 м3/ч, Н=10 м', [Description] = N'Тип/марка: ГНОМ 6-10Д
ID по каталогу: 215243', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 76, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос дренажныйй погружной моноблочный для грязной воды с поплавковым выключателем Q= 6 м3/ч, Н=10 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос подачи осветленной воды к горизонтальному отстойнику', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насос подачи осветленной воды к горизонтальному отстойнику', N'Р401
Тип/марка: 50x3,5 Ст3сп', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 77, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос подачи осветленной воды к горизонтальному отстойнику', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насос подачи осветленной воды к горизонтальному отстойнику', [Description] = N'Р401
Тип/марка: 50x3,5 Ст3сп', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 77, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос подачи осветленной воды к горизонтальному отстойнику', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный артезианский погружной Q=600 м3/ч, напор 28 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насос центробежный артезианский погружной Q=600 м3/ч, напор 28 м', N'Тип/марка: 294538-ВК.ОЛ2
ID по каталогу: 237735
ID обор. полный: 309316', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 78, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный артезианский погружной Q=600 м3/ч, напор 28 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насос центробежный артезианский погружной Q=600 м3/ч, напор 28 м', [Description] = N'Тип/марка: 294538-ВК.ОЛ2
ID по каталогу: 237735
ID обор. полный: 309316', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 78, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный артезианский погружной Q=600 м3/ч, напор 28 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный горизонтальный двухстороннего входа Д4000-95 с электродвигателем АОД -1600-6У1', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насос центробежный горизонтальный двухстороннего входа Д4000-95 с электродвигателем АОД -1600-6У1', N'ID по каталогу: 10827', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 79, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный горизонтальный двухстороннего входа Д4000-95 с электродвигателем АОД -1600-6У1', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насос центробежный горизонтальный двухстороннего входа Д4000-95 с электродвигателем АОД -1600-6У1', [Description] = N'ID по каталогу: 10827', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 79, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный горизонтальный двухстороннего входа Д4000-95 с электродвигателем АОД -1600-6У1', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный горизонтальный консольный Q=1100 м3/ч, напор 45-50 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насос центробежный горизонтальный консольный Q=1100 м3/ч, напор 45-50 м', N'Тип/марка: 294538-ВК.ОЛ1
ID по каталогу: 236059
ID обор. полный: 309910', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 80, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный горизонтальный консольный Q=1100 м3/ч, напор 45-50 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насос центробежный горизонтальный консольный Q=1100 м3/ч, напор 45-50 м', [Description] = N'Тип/марка: 294538-ВК.ОЛ1
ID по каталогу: 236059
ID обор. полный: 309910', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 80, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный горизонтальный консольный Q=1100 м3/ч, напор 45-50 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный, двустороннего входа - производительность Q=450 м3/ч; - напор Н=45 м; - давление на входе не более 0,6 МПа; - индекс обточки рабочего колеса а (уменьшенный диаметр рабочего колеса); - номинальный диаметр всасывающего патрубка 250 мм; - номинальный диаметр напорного патрубка 150 мм; - мощность 90 кВт; - напряжение 380 В; - частота вращения 980 об/мин.; - частота 50 Hz; - род тока - переменный; - комплект поставки: ответные фланцы, крепеж, прокладки, электродвигатель, рама с крепежом к насосу, установочными винтами, регулировочными подкладками', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насос центробежный, двустороннего входа - производительность Q=450 м3/ч; - напор Н=45 м; - давление на входе не более 0,6 МПа; - индекс обточки рабочего колеса а (уменьшенный диаметр рабочего колеса); - номинальный диаметр всасывающего патрубка 250 мм; - номинальный диаметр напорного патрубка 150 мм; - мощность 90 кВт; - напряжение 380 В; - частота вращения 980 об/мин.; - частота 50 Hz; - род тока - переменный; - комплект поставки: ответные фланцы, крепеж, прокладки, электродвигатель, рама с крепежом к насосу, установочными винтами, регулировочными подкладками', N'ID по каталогу: 116965', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 81, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный, двустороннего входа - производительность Q=450 м3/ч; - напор Н=45 м; - давление на входе не более 0,6 МПа; - индекс обточки рабочего колеса а (уменьшенный диаметр рабочего колеса); - номинальный диаметр всасывающего патрубка 250 мм; - номинальный диаметр напорного патрубка 150 мм; - мощность 90 кВт; - напряжение 380 В; - частота вращения 980 об/мин.; - частота 50 Hz; - род тока - переменный; - комплект поставки: ответные фланцы, крепеж, прокладки, электродвигатель, рама с крепежом к насосу, установочными винтами, регулировочными подкладками', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насос центробежный, двустороннего входа - производительность Q=450 м3/ч; - напор Н=45 м; - давление на входе не более 0,6 МПа; - индекс обточки рабочего колеса а (уменьшенный диаметр рабочего колеса); - номинальный диаметр всасывающего патрубка 250 мм; - номинальный диаметр напорного патрубка 150 мм; - мощность 90 кВт; - напряжение 380 В; - частота вращения 980 об/мин.; - частота 50 Hz; - род тока - переменный; - комплект поставки: ответные фланцы, крепеж, прокладки, электродвигатель, рама с крепежом к насосу, установочными винтами, регулировочными подкладками', [Description] = N'ID по каталогу: 116965', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 81, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный, двустороннего входа - производительность Q=450 м3/ч; - напор Н=45 м; - давление на входе не более 0,6 МПа; - индекс обточки рабочего колеса а (уменьшенный диаметр рабочего колеса); - номинальный диаметр всасывающего патрубка 250 мм; - номинальный диаметр напорного патрубка 150 мм; - мощность 90 кВт; - напряжение 380 В; - частота вращения 980 об/мин.; - частота 50 Hz; - род тока - переменный; - комплект поставки: ответные фланцы, крепеж, прокладки, электродвигатель, рама с крепежом к насосу, установочными винтами, регулировочными подкладками', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос циркуляционный G=207 м3/ч; H=50 м; с электродвигателем U=380В, P=45кВт в комплекте с ответными фланцами, прокладками и крепежом Рабочая среда: сетевая вода (Pp=1,6 МПа; Тр=95°С)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насос циркуляционный G=207 м3/ч; H=50 м; с электродвигателем U=380В, P=45кВт в комплекте с ответными фланцами, прокладками и крепежом Рабочая среда: сетевая вода (Pp=1,6 МПа; Тр=95°С)', N'ID по каталогу: 265059', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 82, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос циркуляционный G=207 м3/ч; H=50 м; с электродвигателем U=380В, P=45кВт в комплекте с ответными фланцами, прокладками и крепежом Рабочая среда: сетевая вода (Pp=1,6 МПа; Тр=95°С)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насос циркуляционный G=207 м3/ч; H=50 м; с электродвигателем U=380В, P=45кВт в комплекте с ответными фланцами, прокладками и крепежом Рабочая среда: сетевая вода (Pp=1,6 МПа; Тр=95°С)', [Description] = N'ID по каталогу: 265059', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 82, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос циркуляционный G=207 м3/ч; H=50 м; с электродвигателем U=380В, P=45кВт в комплекте с ответными фланцами, прокладками и крепежом Рабочая среда: сетевая вода (Pp=1,6 МПа; Тр=95°С)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос циркуляционный, DN25, PN25, длина 250 мм, присоединение — фланец', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насос циркуляционный, DN25, PN25, длина 250 мм, присоединение — фланец', N'Тип/марка: RMV 1-5F
ID по каталогу: 278829', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 83, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос циркуляционный, DN25, PN25, длина 250 мм, присоединение — фланец', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насос циркуляционный, DN25, PN25, длина 250 мм, присоединение — фланец', [Description] = N'Тип/марка: RMV 1-5F
ID по каталогу: 278829', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 83, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос циркуляционный, DN25, PN25, длина 250 мм, присоединение — фланец', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насосная станция питьевого водоснабжения производительностью 0,1-0,8 м3/ч, напор 10 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насосная станция питьевого водоснабжения производительностью 0,1-0,8 м3/ч, напор 10 м', N'Тип/марка: 296204-4Y0000-ОП-ПЗ.ОЛ4
ID по каталогу: 314714', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 84, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насосная станция питьевого водоснабжения производительностью 0,1-0,8 м3/ч, напор 10 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насосная станция питьевого водоснабжения производительностью 0,1-0,8 м3/ч, напор 10 м', [Description] = N'Тип/марка: 296204-4Y0000-ОП-ПЗ.ОЛ4
ID по каталогу: 314714', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 84, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насосная станция питьевого водоснабжения производительностью 0,1-0,8 м3/ч, напор 10 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насосный агрегат консольный. Подача Q=100 м3/час, Н=80 м.в.ст. N=45,0 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Насосный агрегат консольный. Подача Q=100 м3/час, Н=80 м.в.ст. N=45,0 кВт', N'Тип/марка: К100 - 65 -250
ID по каталогу: 88497', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 85, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насосный агрегат консольный. Подача Q=100 м3/час, Н=80 м.в.ст. N=45,0 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Насосный агрегат консольный. Подача Q=100 м3/час, Н=80 м.в.ст. N=45,0 кВт', [Description] = N'Тип/марка: К100 - 65 -250
ID по каталогу: 88497', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 85, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насосный агрегат консольный. Подача Q=100 м3/час, Н=80 м.в.ст. N=45,0 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка (1 - рабочая, 1 - резервная) L=22000 м3/ч, Рсети=900 Па, с водяным калорифером, сторона обслуживания слева. В комплекте: - приточная установка - 2 шт.; - узел обвязки - 2 шт.; - шкаф автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка (1 - рабочая, 1 - резервная) L=22000 м3/ч, Рсети=900 Па, с водяным калорифером, сторона обслуживания слева. В комплекте: - приточная установка - 2 шт.; - узел обвязки - 2 шт.; - шкаф автоматики', N'Помещение для вытяжных вентиляторов
Тип/марка: 290349-02-ОВ.ОЛ
ID по каталогу: 125627', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 86, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка (1 - рабочая, 1 - резервная) L=22000 м3/ч, Рсети=900 Па, с водяным калорифером, сторона обслуживания слева. В комплекте: - приточная установка - 2 шт.; - узел обвязки - 2 шт.; - шкаф автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка (1 - рабочая, 1 - резервная) L=22000 м3/ч, Рсети=900 Па, с водяным калорифером, сторона обслуживания слева. В комплекте: - приточная установка - 2 шт.; - узел обвязки - 2 шт.; - шкаф автоматики', [Description] = N'Помещение для вытяжных вентиляторов
Тип/марка: 290349-02-ОВ.ОЛ
ID по каталогу: 125627', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 86, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка (1 - рабочая, 1 - резервная) L=22000 м3/ч, Рсети=900 Па, с водяным калорифером, сторона обслуживания слева. В комплекте: - приточная установка - 2 шт.; - узел обвязки - 2 шт.; - шкаф автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 1 600 м3/ч, с электрокалорифером', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L= 1 600 м3/ч, с электрокалорифером', N'Электропомещение №2
Тип/марка: 294082-ИОС4.ОЛ5
ID по каталогу: 245970', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 87, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 1 600 м3/ч, с электрокалорифером', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L= 1 600 м3/ч, с электрокалорифером', [Description] = N'Электропомещение №2
Тип/марка: 294082-ИОС4.ОЛ5
ID по каталогу: 245970', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 87, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 1 600 м3/ч, с электрокалорифером', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 2500 м3/ч, Нсети = 700Па С электрокалорифером, с воздухоохлажением. С автоматикой.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L= 2500 м3/ч, Нсети = 700Па С электрокалорифером, с воздухоохлажением. С автоматикой.', N'Помещения персонала
Тип/марка: Спецификация № 4100186467 от 13.11.2024г. 
(согласно 294814-ОВ.ОЛ5 изм.1)
ID по каталогу: 275027', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 88, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 2500 м3/ч, Нсети = 700Па С электрокалорифером, с воздухоохлажением. С автоматикой.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L= 2500 м3/ч, Нсети = 700Па С электрокалорифером, с воздухоохлажением. С автоматикой.', [Description] = N'Помещения персонала
Тип/марка: Спецификация № 4100186467 от 13.11.2024г. 
(согласно 294814-ОВ.ОЛ5 изм.1)
ID по каталогу: 275027', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 88, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 2500 м3/ч, Нсети = 700Па С электрокалорифером, с воздухоохлажением. С автоматикой.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 26 000 м3/ч, H.сети=500 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L= 26 000 м3/ч, H.сети=500 Па', N'1 этаж
Тип/марка: 292209-ОПР1.ОЛ1
ID по каталогу: 263125', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 89, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 26 000 м3/ч, H.сети=500 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L= 26 000 м3/ч, H.сети=500 Па', [Description] = N'1 этаж
Тип/марка: 292209-ОПР1.ОЛ1
ID по каталогу: 263125', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 89, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 26 000 м3/ч, H.сети=500 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 410 м3/ч, H.сети=300 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L= 410 м3/ч, H.сети=300 Па', N'Помещения персонала
Тип/марка: 294545-02-ОВ3.ОЛ
ID по каталогу: 276904', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 90, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 410 м3/ч, H.сети=300 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L= 410 м3/ч, H.сети=300 Па', [Description] = N'Помещения персонала
Тип/марка: 294545-02-ОВ3.ОЛ
ID по каталогу: 276904', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 90, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 410 м3/ч, H.сети=300 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 5 000 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L= 5 000 м3/ч, с водяным воздухонагревателем', N'Склад горюче-смазочных материалов 1 и 2
Тип/марка: 294082-ИОС4.ОЛ3
ID по каталогу: 245968', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 91, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 5 000 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L= 5 000 м3/ч, с водяным воздухонагревателем', [Description] = N'Склад горюче-смазочных материалов 1 и 2
Тип/марка: 294082-ИОС4.ОЛ3
ID по каталогу: 245968', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 91, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 5 000 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 5 200 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L= 5 200 м3/ч, с водяным воздухонагревателем', N'Тип/марка: 294082-ИОС4.ОЛ6
ID по каталогу: 254909
ID обор. полный: 274024', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 92, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 5 200 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L= 5 200 м3/ч, с водяным воздухонагревателем', [Description] = N'Тип/марка: 294082-ИОС4.ОЛ6
ID по каталогу: 254909
ID обор. полный: 274024', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 92, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 5 200 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 500 м3/ч, H.сети=300 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L= 500 м3/ч, H.сети=300 Па', N'Встроенные помещения
Тип/марка: 294545-02-ОВ4.ОЛ
ID по каталогу: 276907', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 93, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 500 м3/ч, H.сети=300 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L= 500 м3/ч, H.сети=300 Па', [Description] = N'Встроенные помещения
Тип/марка: 294545-02-ОВ4.ОЛ
ID по каталогу: 276907', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 93, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 500 м3/ч, H.сети=300 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 600 м3/ч, с электрокалорифером', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L= 600 м3/ч, с электрокалорифером', N'ID по каталогу: 262103', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 94, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 600 м3/ч, с электрокалорифером', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L= 600 м3/ч, с электрокалорифером', [Description] = N'ID по каталогу: 262103', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 94, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 600 м3/ч, с электрокалорифером', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 650 м3/ч, Нсети = 400Па с электрокалорифером.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L= 650 м3/ч, Нсети = 400Па с электрокалорифером.', N'Помещения персонала
Тип/марка: Спецификация № 4100186467 от 13.11.2024г. 
(согласно 294814-ОВ.ОЛ2 изм.1)
ID по каталогу: 263730', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 95, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 650 м3/ч, Нсети = 400Па с электрокалорифером.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L= 650 м3/ч, Нсети = 400Па с электрокалорифером.', [Description] = N'Помещения персонала
Тип/марка: Спецификация № 4100186467 от 13.11.2024г. 
(согласно 294814-ОВ.ОЛ2 изм.1)
ID по каталогу: 263730', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 95, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 650 м3/ч, Нсети = 400Па с электрокалорифером.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 80000 м3/ч, Р=1500 Па с холодильной машиной с комплектом автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L= 80000 м3/ч, Р=1500 Па с холодильной машиной с комплектом автоматики', N'Подвальные помещения
Тип/марка: 294857-ОВ.ОЛ
ID по каталогу: 234038
ID обор. полный: 143930', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 96, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 80000 м3/ч, Р=1500 Па с холодильной машиной с комплектом автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L= 80000 м3/ч, Р=1500 Па с холодильной машиной с комплектом автоматики', [Description] = N'Подвальные помещения
Тип/марка: 294857-ОВ.ОЛ
ID по каталогу: 234038
ID обор. полный: 143930', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 96, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 80000 м3/ч, Р=1500 Па с холодильной машиной с комплектом автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=1000 м3/ч', N'Помещения персонала
Тип/марка: Спецификация № 4100098194 (291553-ИОС4.УС.ОЛ2)
ID по каталогу: 179713', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 97, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=1000 м3/ч', [Description] = N'Помещения персонала
Тип/марка: Спецификация № 4100098194 (291553-ИОС4.УС.ОЛ2)
ID по каталогу: 179713', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 97, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=11 200 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=11 200 м3/ч, с водяным воздухонагревателем', N'Тип/марка: Договор №100000000000287933 Спецификация 4100132092 от 18.10.2022 (294173-01-ОВ.ОЛ4)
ID по каталогу: 222190', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 98, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=11 200 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=11 200 м3/ч, с водяным воздухонагревателем', [Description] = N'Тип/марка: Договор №100000000000287933 Спецификация 4100132092 от 18.10.2022 (294173-01-ОВ.ОЛ4)
ID по каталогу: 222190', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 98, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=11 200 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=15000 м3/ч, P=900 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=15000 м3/ч, P=900 Па', N'Электропомещения
Тип/марка: 295639-ОВ.ОЛ
ID по каталогу: 270382', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 99, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=15000 м3/ч, P=900 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=15000 м3/ч, P=900 Па', [Description] = N'Электропомещения
Тип/марка: 295639-ОВ.ОЛ
ID по каталогу: 270382', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 99, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=15000 м3/ч, P=900 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1600м3/ч, P=900 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=1600м3/ч, P=900 Па', N'Убежище
Тип/марка: Договор № 100000000000290062 Спецификация 4160000025 от 02.02.2022г.
(293170-ИОС4.УС.ОЛ15)
ID по каталогу: 209527', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 100, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1600м3/ч, P=900 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=1600м3/ч, P=900 Па', [Description] = N'Убежище
Тип/марка: Договор № 100000000000290062 Спецификация 4160000025 от 02.02.2022г.
(293170-ИОС4.УС.ОЛ15)
ID по каталогу: 209527', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 100, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1600м3/ч, P=900 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1750 куб.м/ч Рсети=500Па с резервным вентилятором', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=1750 куб.м/ч Рсети=500Па с резервным вентилятором', N'АБК 1 этаж. Здравпункт
Тип/марка: 292460-ОВ2.ОЛ2
ID по каталогу: 197903
ID обор. полный: 195034', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 101, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1750 куб.м/ч Рсети=500Па с резервным вентилятором', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=1750 куб.м/ч Рсети=500Па с резервным вентилятором', [Description] = N'АБК 1 этаж. Здравпункт
Тип/марка: 292460-ОВ2.ОЛ2
ID по каталогу: 197903
ID обор. полный: 195034', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 101, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1750 куб.м/ч Рсети=500Па с резервным вентилятором', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=21000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=21000 м3/ч', N'Тип/марка: 295272-ОВ2.ОЛ4
ID по каталогу: 250293', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 102, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=21000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=21000 м3/ч', [Description] = N'Тип/марка: 295272-ОВ2.ОЛ4
ID по каталогу: 250293', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 102, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=21000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=2200 куб.м/ч Рсети=450 Па подвесная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=2200 куб.м/ч Рсети=450 Па подвесная', N'АБК 1 этаж. Гардеробные
Тип/марка: 292956-01-ОВ2.ОЛ2 (П2)
ID по каталогу: 195034', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 103, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=2200 куб.м/ч Рсети=450 Па подвесная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=2200 куб.м/ч Рсети=450 Па подвесная', [Description] = N'АБК 1 этаж. Гардеробные
Тип/марка: 292956-01-ОВ2.ОЛ2 (П2)
ID по каталогу: 195034', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 103, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=2200 куб.м/ч Рсети=450 Па подвесная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=28000 м3/ч, Hсети=700 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=28000 м3/ч, Hсети=700 Па', N'Гидравлическая насосная станция
Тип/марка: 291553-13-ОВ.ОЛ
ID по каталогу: 229820', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 104, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=28000 м3/ч, Hсети=700 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=28000 м3/ч, Hсети=700 Па', [Description] = N'Гидравлическая насосная станция
Тип/марка: 291553-13-ОВ.ОЛ
ID по каталогу: 229820', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 104, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=28000 м3/ч, Hсети=700 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=30000 м3/ч, P=1000 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=30000 м3/ч, P=1000 Па', N'Убежище
Тип/марка: Договор № 100000000000287933 Спецификация 4100129817 от 14.09.2022г.
(293170-ИОС4.УС.ОЛ10)
ID по каталогу: 221727
ID обор. полный: 240234', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 105, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=30000 м3/ч, P=1000 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=30000 м3/ч, P=1000 Па', [Description] = N'Убежище
Тип/марка: Договор № 100000000000287933 Спецификация 4100129817 от 14.09.2022г.
(293170-ИОС4.УС.ОЛ10)
ID по каталогу: 221727
ID обор. полный: 240234', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 105, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=30000 м3/ч, P=1000 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=3250 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=3250 м3/ч', N'1 и 2 этажи
ID по каталогу: 174925', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 106, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=3250 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=3250 м3/ч', [Description] = N'1 и 2 этажи
ID по каталогу: 174925', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 106, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=3250 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=43 600 м3/ч с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=43 600 м3/ч с водяным воздухонагревателем', N'Тип/марка: 294082-ИОС4.ОЛ1
ID по каталогу: 260763', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 107, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=43 600 м3/ч с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=43 600 м3/ч с водяным воздухонагревателем', [Description] = N'Тип/марка: 294082-ИОС4.ОЛ1
ID по каталогу: 260763', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 107, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=43 600 м3/ч с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=5570м3/ч, Р=500 Па с встроенной автоматикой, в комплекте с узлом регулирования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=5570м3/ч, Р=500 Па с встроенной автоматикой, в комплекте с узлом регулирования', N'АБК 2 этаж - 1 шт., АБК 3 этаж - 1 шт. офисняе помещения, гардероб, др. помещения
Тип/марка: YAMAL-Mini-LX-005-УХЛ4 V2406077
ID по каталогу: 275439', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 108, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=5570м3/ч, Р=500 Па с встроенной автоматикой, в комплекте с узлом регулирования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=5570м3/ч, Р=500 Па с встроенной автоматикой, в комплекте с узлом регулирования', [Description] = N'АБК 2 этаж - 1 шт., АБК 3 этаж - 1 шт. офисняе помещения, гардероб, др. помещения
Тип/марка: YAMAL-Mini-LX-005-УХЛ4 V2406077
ID по каталогу: 275439', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 108, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=5570м3/ч, Р=500 Па с встроенной автоматикой, в комплекте с узлом регулирования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=7200 м3/ч, Р=600 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=7200 м3/ч, Р=600 Па', N'АБК 1 этаж. Столовая
Тип/марка: Договор № 100000000000294186 Спецификация 4100129469 от 07.09.2022г.
(293797-ОВ.ОЛ5)
ID по каталогу: 221004', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 109, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=7200 м3/ч, Р=600 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=7200 м3/ч, Р=600 Па', [Description] = N'АБК 1 этаж. Столовая
Тип/марка: Договор № 100000000000294186 Спецификация 4100129469 от 07.09.2022г.
(293797-ОВ.ОЛ5)
ID по каталогу: 221004', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 109, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=7200 м3/ч, Р=600 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=8800 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=8800 м3/ч', N'Убежище
Тип/марка: 293457-ИОС4.УС.ОЛ
ID по каталогу: 199702
ID обор. полный: 209527', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 110, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=8800 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=8800 м3/ч', [Description] = N'Убежище
Тип/марка: 293457-ИОС4.УС.ОЛ
ID по каталогу: 199702
ID обор. полный: 209527', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 110, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=8800 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=90 000 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка L=90 000 м3/ч, с водяным воздухонагревателем', N'ВШМ
Тип/марка: 291553-ИОС4.УС.ОЛ3
ID по каталогу: 154262', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 111, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=90 000 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка L=90 000 м3/ч, с водяным воздухонагревателем', [Description] = N'ВШМ
Тип/марка: 291553-ИОС4.УС.ОЛ3
ID по каталогу: 154262', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 111, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=90 000 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П1. L=5570м3/ч, P=650Па, в комплекте с автоматикой, шкафом управления, со смесительным узлом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка П1. L=5570м3/ч, P=650Па, в комплекте с автоматикой, шкафом управления, со смесительным узлом.', N'2 этаж
ID по каталогу: 283408', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 112, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П1. L=5570м3/ч, P=650Па, в комплекте с автоматикой, шкафом управления, со смесительным узлом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка П1. L=5570м3/ч, P=650Па, в комплекте с автоматикой, шкафом управления, со смесительным узлом.', [Description] = N'2 этаж
ID по каталогу: 283408', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 112, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П1. L=5570м3/ч, P=650Па, в комплекте с автоматикой, шкафом управления, со смесительным узлом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П6, клапаны ВЕ3, компрессорно-конденсаторный блок. L=4 500 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка П6, клапаны ВЕ3, компрессорно-конденсаторный блок. L=4 500 м3/ч', N'Электропомещения №1, 2, встроенные помещения
Тип/марка: 295369-ЭМ1.ОЛ1
ID по каталогу: 274024', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 113, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П6, клапаны ВЕ3, компрессорно-конденсаторный блок. L=4 500 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка П6, клапаны ВЕ3, компрессорно-конденсаторный блок. L=4 500 м3/ч', [Description] = N'Электропомещения №1, 2, встроенные помещения
Тип/марка: 295369-ЭМ1.ОЛ1
ID по каталогу: 274024', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 113, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П6, клапаны ВЕ3, компрессорно-конденсаторный блок. L=4 500 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П9 (1 раб./1 резер.) L=3 000 м3/ч, компрессорно-конденсаторный блок (1 раб. /1 резер.)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточная установка П9 (1 раб./1 резер.) L=3 000 м3/ч, компрессорно-конденсаторный блок (1 раб. /1 резер.)', N'Тип/марка: Спецификация № 4100187146 от 24.11.2024г. 
(согласно 295093-ОВ8.ОЛ2)
ID по каталогу: 274035', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 114, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П9 (1 раб./1 резер.) L=3 000 м3/ч, компрессорно-конденсаторный блок (1 раб. /1 резер.)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточная установка П9 (1 раб./1 резер.) L=3 000 м3/ч, компрессорно-конденсаторный блок (1 раб. /1 резер.)', [Description] = N'Тип/марка: Спецификация № 4100187146 от 24.11.2024г. 
(согласно 295093-ОВ8.ОЛ2)
ID по каталогу: 274035', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 114, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П9 (1 раб./1 резер.) L=3 000 м3/ч, компрессорно-конденсаторный блок (1 раб. /1 резер.)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточные установки L= 80000 м3/ч, Р=1500 Па с комплектом автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Приточные установки L= 80000 м3/ч, Р=1500 Па с комплектом автоматики', N'Участок линии стана
Тип/марка: 294857-ОВ.ОЛ1
ID по каталогу: 234283', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 115, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточные установки L= 80000 м3/ч, Р=1500 Па с комплектом автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Приточные установки L= 80000 м3/ч, Р=1500 Па с комплектом автоматики', [Description] = N'Участок линии стана
Тип/марка: 294857-ОВ.ОЛ1
ID по каталогу: 234283', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 115, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточные установки L= 80000 м3/ч, Р=1500 Па с комплектом автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Протяжный станок для ударных испытаний образцов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Протяжный станок для ударных испытаний образцов', N'Протяжной станок', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 116, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Протяжный станок для ударных испытаний образцов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Протяжный станок для ударных испытаний образцов', [Description] = N'Протяжной станок', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 116, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Протяжный станок для ударных испытаний образцов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пульт цифровой диспетчерской громкоговорящей связи на 24 кнопки (без WiFi модуля)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Пульт цифровой диспетчерской громкоговорящей связи на 24 кнопки (без WiFi модуля)', N'Тип/марка: DIS-IP2
ID по каталогу: 241954', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 117, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пульт цифровой диспетчерской громкоговорящей связи на 24 кнопки (без WiFi модуля)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Пульт цифровой диспетчерской громкоговорящей связи на 24 кнопки (без WiFi модуля)', [Description] = N'Тип/марка: DIS-IP2
ID по каталогу: 241954', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 117, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пульт цифровой диспетчерской громкоговорящей связи на 24 кнопки (без WiFi модуля)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iн=320А, арт. 109881 - 1шт. Выключатели распределения: ВА57-35-340010-250А-2500-690AC-УХЛ3-КЭАЗ, Iн=250 А, арт. 108600 - 2 шт.; ВА57-35-340010-160А-1600-690AC-УХЛ3-КЭАЗ, Iн=160 А, арт. 108586 - 2 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 2 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 2 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники снизу : СКПО-40 - 8шт., СКПО-22 - 4шт.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iн=320А, арт. 109881 - 1шт. Выключатели распределения: ВА57-35-340010-250А-2500-690AC-УХЛ3-КЭАЗ, Iн=250 А, арт. 108600 - 2 шт.; ВА57-35-340010-160А-1600-690AC-УХЛ3-КЭАЗ, Iн=160 А, арт. 108586 - 2 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 2 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 2 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники снизу : СКПО-40 - 8шт., СКПО-22 - 4шт.', N'ID по каталогу: 239169', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 118, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iн=320А, арт. 109881 - 1шт. Выключатели распределения: ВА57-35-340010-250А-2500-690AC-УХЛ3-КЭАЗ, Iн=250 А, арт. 108600 - 2 шт.; ВА57-35-340010-160А-1600-690AC-УХЛ3-КЭАЗ, Iн=160 А, арт. 108586 - 2 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 2 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 2 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники снизу : СКПО-40 - 8шт., СКПО-22 - 4шт.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iн=320А, арт. 109881 - 1шт. Выключатели распределения: ВА57-35-340010-250А-2500-690AC-УХЛ3-КЭАЗ, Iн=250 А, арт. 108600 - 2 шт.; ВА57-35-340010-160А-1600-690AC-УХЛ3-КЭАЗ, Iн=160 А, арт. 108586 - 2 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 2 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 2 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники снизу : СКПО-40 - 8шт., СКПО-22 - 4шт.', [Description] = N'ID по каталогу: 239169', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 118, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iн=320А, арт. 109881 - 1шт. Выключатели распределения: ВА57-35-340010-250А-2500-690AC-УХЛ3-КЭАЗ, Iн=250 А, арт. 108600 - 2 шт.; ВА57-35-340010-160А-1600-690AC-УХЛ3-КЭАЗ, Iн=160 А, арт. 108586 - 2 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 2 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 2 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники снизу : СКПО-40 - 8шт., СКПО-22 - 4шт.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-200А-2000-400AC-УХЛ3-КЭАЗ, Iн=200А, арт. 109314 - 1шт. Выключатели распределения: ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iн=80 А, арт. 108428 - 1 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 1 шт.; ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 1 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверху : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -6шт., СКПО-22 -2 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х50)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-200А-2000-400AC-УХЛ3-КЭАЗ, Iн=200А, арт. 109314 - 1шт. Выключатели распределения: ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iн=80 А, арт. 108428 - 1 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 1 шт.; ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 1 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверху : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -6шт., СКПО-22 -2 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х50)', N'ID по каталогу: 197663', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 119, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-200А-2000-400AC-УХЛ3-КЭАЗ, Iн=200А, арт. 109314 - 1шт. Выключатели распределения: ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iн=80 А, арт. 108428 - 1 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 1 шт.; ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 1 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверху : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -6шт., СКПО-22 -2 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х50)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-200А-2000-400AC-УХЛ3-КЭАЗ, Iн=200А, арт. 109314 - 1шт. Выключатели распределения: ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iн=80 А, арт. 108428 - 1 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 1 шт.; ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 1 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверху : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -6шт., СКПО-22 -2 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х50)', [Description] = N'ID по каталогу: 197663', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 119, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-200А-2000-400AC-УХЛ3-КЭАЗ, Iн=200А, арт. 109314 - 1шт. Выключатели распределения: ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iн=80 А, арт. 108428 - 1 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 1 шт.; ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 1 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверху : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -6шт., СКПО-22 -2 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х50)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-250А-2500-400AC-УХЛ3-КЭАЗ, Iн=250А, арт. 109319 - 1шт. Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 3 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверхуу : СКПО-40 - 1шт., СКПО-32 -6 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -6 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х70)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-250А-2500-400AC-УХЛ3-КЭАЗ, Iн=250А, арт. 109319 - 1шт. Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 3 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверхуу : СКПО-40 - 1шт., СКПО-32 -6 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -6 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х70)', N'Тип/марка: ПР8503-1054-4УХЛ2
ID по каталогу: 263659', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 120, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-250А-2500-400AC-УХЛ3-КЭАЗ, Iн=250А, арт. 109319 - 1шт. Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 3 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверхуу : СКПО-40 - 1шт., СКПО-32 -6 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -6 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х70)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-250А-2500-400AC-УХЛ3-КЭАЗ, Iн=250А, арт. 109319 - 1шт. Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 3 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверхуу : СКПО-40 - 1шт., СКПО-32 -6 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -6 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х70)', [Description] = N'Тип/марка: ПР8503-1054-4УХЛ2
ID по каталогу: 263659', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 120, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-250А-2500-400AC-УХЛ3-КЭАЗ, Iн=250А, арт. 109319 - 1шт. Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 3 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверхуу : СКПО-40 - 1шт., СКПО-32 -6 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -6 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х70)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-100А-1000-400AC-УХЛ3-КЭАЗ, Iр=100А, арт. №109286; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 5 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 1 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 12 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-FRLS-5х50.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-100А-1000-400AC-УХЛ3-КЭАЗ, Iр=100А, арт. №109286; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 5 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 1 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 12 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-FRLS-5х50.', N'Тип/марка: ПР8503-1054-2УХЛ3
ID по каталогу: 245483', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 121, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-100А-1000-400AC-УХЛ3-КЭАЗ, Iр=100А, арт. №109286; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 5 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 1 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 12 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-FRLS-5х50.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-100А-1000-400AC-УХЛ3-КЭАЗ, Iр=100А, арт. №109286; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 5 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 1 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 12 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-FRLS-5х50.', [Description] = N'Тип/марка: ПР8503-1054-2УХЛ3
ID по каталогу: 245483', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 121, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-100А-1000-400AC-УХЛ3-КЭАЗ, Iр=100А, арт. №109286; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 5 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 1 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 12 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-FRLS-5х50.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-160А-1600-400AC-УХЛ3-КЭАЗ, Iр=160А, арт. №109307; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 4 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 2 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 8 шт., PG 29 (DKC, арт. 53100) - 4 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-5х95.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-160А-1600-400AC-УХЛ3-КЭАЗ, Iр=160А, арт. №109307; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 4 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 2 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 8 шт., PG 29 (DKC, арт. 53100) - 4 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-5х95.', N'Тип/марка: 295272-ОВ2.ОЛ4
ID по каталогу: 245482', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 122, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-160А-1600-400AC-УХЛ3-КЭАЗ, Iр=160А, арт. №109307; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 4 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 2 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 8 шт., PG 29 (DKC, арт. 53100) - 4 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-5х95.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-160А-1600-400AC-УХЛ3-КЭАЗ, Iр=160А, арт. №109307; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 4 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 2 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 8 шт., PG 29 (DKC, арт. 53100) - 4 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-5х95.', [Description] = N'Тип/марка: 295272-ОВ2.ОЛ4
ID по каталогу: 245482', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 122, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-160А-1600-400AC-УХЛ3-КЭАЗ, Iр=160А, арт. №109307; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 4 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 2 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 8 шт., PG 29 (DKC, арт. 53100) - 4 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-5х95.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля снизу. Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iр=320А, арт. №109881; Выключатели распределения: ВА57-35-340010-200А-2000-690AC-УХЛ3-КЭАЗ, Iр=200А - 1 шт., арт. №108594; ВА57-35-340010-100А-1000-690AC-УХЛ3-КЭАЗ, Iр=100А - 1 шт., арт. №108566; ВА57-31-340010-100А-1200-690AC-УХЛ3-КЭАЗ, Iр=100А - 3 шт., арт. №108427; ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iр=80А - 2 шт., арт. №108428; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iр=63А - 4 шт., арт. №108440; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 1 шт., арт. №108434. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники снизу: PG 48 (DKC, арт. 53400) - 3 шт., PG 42 (DKC, арт. 53300) - 10 шт., PG 36 (DKC, арт. 53200) - 1 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х95).', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля снизу. Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iр=320А, арт. №109881; Выключатели распределения: ВА57-35-340010-200А-2000-690AC-УХЛ3-КЭАЗ, Iр=200А - 1 шт., арт. №108594; ВА57-35-340010-100А-1000-690AC-УХЛ3-КЭАЗ, Iр=100А - 1 шт., арт. №108566; ВА57-31-340010-100А-1200-690AC-УХЛ3-КЭАЗ, Iр=100А - 3 шт., арт. №108427; ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iр=80А - 2 шт., арт. №108428; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iр=63А - 4 шт., арт. №108440; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 1 шт., арт. №108434. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники снизу: PG 48 (DKC, арт. 53400) - 3 шт., PG 42 (DKC, арт. 53300) - 10 шт., PG 36 (DKC, арт. 53200) - 1 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х95).', N'Тип/марка: ПР8501-1093-4УХЛ3
ID по каталогу: 245481', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 123, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля снизу. Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iр=320А, арт. №109881; Выключатели распределения: ВА57-35-340010-200А-2000-690AC-УХЛ3-КЭАЗ, Iр=200А - 1 шт., арт. №108594; ВА57-35-340010-100А-1000-690AC-УХЛ3-КЭАЗ, Iр=100А - 1 шт., арт. №108566; ВА57-31-340010-100А-1200-690AC-УХЛ3-КЭАЗ, Iр=100А - 3 шт., арт. №108427; ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iр=80А - 2 шт., арт. №108428; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iр=63А - 4 шт., арт. №108440; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 1 шт., арт. №108434. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники снизу: PG 48 (DKC, арт. 53400) - 3 шт., PG 42 (DKC, арт. 53300) - 10 шт., PG 36 (DKC, арт. 53200) - 1 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х95).', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля снизу. Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iр=320А, арт. №109881; Выключатели распределения: ВА57-35-340010-200А-2000-690AC-УХЛ3-КЭАЗ, Iр=200А - 1 шт., арт. №108594; ВА57-35-340010-100А-1000-690AC-УХЛ3-КЭАЗ, Iр=100А - 1 шт., арт. №108566; ВА57-31-340010-100А-1200-690AC-УХЛ3-КЭАЗ, Iр=100А - 3 шт., арт. №108427; ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iр=80А - 2 шт., арт. №108428; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iр=63А - 4 шт., арт. №108440; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 1 шт., арт. №108434. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники снизу: PG 48 (DKC, арт. 53400) - 3 шт., PG 42 (DKC, арт. 53300) - 10 шт., PG 36 (DKC, арт. 53200) - 1 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х95).', [Description] = N'Тип/марка: ПР8501-1093-4УХЛ3
ID по каталогу: 245481', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 123, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля снизу. Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iр=320А, арт. №109881; Выключатели распределения: ВА57-35-340010-200А-2000-690AC-УХЛ3-КЭАЗ, Iр=200А - 1 шт., арт. №108594; ВА57-35-340010-100А-1000-690AC-УХЛ3-КЭАЗ, Iр=100А - 1 шт., арт. №108566; ВА57-31-340010-100А-1200-690AC-УХЛ3-КЭАЗ, Iр=100А - 3 шт., арт. №108427; ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iр=80А - 2 шт., арт. №108428; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iр=63А - 4 шт., арт. №108440; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 1 шт., арт. №108434. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники снизу: PG 48 (DKC, арт. 53400) - 3 шт., PG 42 (DKC, арт. 53300) - 10 шт., PG 36 (DKC, арт. 53200) - 1 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х95).', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Расширительный бак объёмом 15 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Расширительный бак объёмом 15 м3', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 124, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Расширительный бак объёмом 15 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Расширительный бак объёмом 15 м3', [Description] = N'', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 124, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Расширительный бак объёмом 15 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Резак РПМ3 РПК-М L=1300 мм', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Резак РПМ3 РПК-М L=1300 мм', N'ID по каталогу: 307094', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 125, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Резак РПМ3 РПК-М L=1300 мм', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Резак РПМ3 РПК-М L=1300 мм', [Description] = N'ID по каталогу: 307094', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 125, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Резак РПМ3 РПК-М L=1300 мм', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Резак РПМ3-РПК-М L=2000 мм', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Резак РПМ3-РПК-М L=2000 мм', N'ID по каталогу: 307091', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 126, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Резак РПМ3-РПК-М L=2000 мм', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Резак РПМ3-РПК-М L=2000 мм', [Description] = N'ID по каталогу: 307091', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 126, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Резак РПМ3-РПК-М L=2000 мм', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ресивер (воздухосборник) емкостью 25 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Ресивер (воздухосборник) емкостью 25 м3', N'Тип/марка: 296204-4N0000-ОП-ПЗ1.ОЛ2
ID по каталогу: 304127', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 127, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ресивер (воздухосборник) емкостью 25 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Ресивер (воздухосборник) емкостью 25 м3', [Description] = N'Тип/марка: 296204-4N0000-ОП-ПЗ1.ОЛ2
ID по каталогу: 304127', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 127, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ресивер (воздухосборник) емкостью 25 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ресивер диаметром 1,6 м, объемом 10 м3, расчетное давление 0,9 Мпа, в комплекте с арматурой и КИП', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Ресивер диаметром 1,6 м, объемом 10 м3, расчетное давление 0,9 Мпа, в комплекте с арматурой и КИП', N'Тип/марка: В-10-0,9-3-УХЛ1
ID по каталогу: 297953', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 128, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ресивер диаметром 1,6 м, объемом 10 м3, расчетное давление 0,9 Мпа, в комплекте с арматурой и КИП', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Ресивер диаметром 1,6 м, объемом 10 м3, расчетное давление 0,9 Мпа, в комплекте с арматурой и КИП', [Description] = N'Тип/марка: В-10-0,9-3-УХЛ1
ID по каталогу: 297953', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 128, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ресивер диаметром 1,6 м, объемом 10 м3, расчетное давление 0,9 Мпа, в комплекте с арматурой и КИП', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная червячная передвижная г/п 0,5 т высота подъема 7 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль ручная червячная передвижная г/п 0,5 т высота подъема 7 м', N'Тип/марка: 289775-07-ВК1.ОЛ3
ID по каталогу: 228508', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 129, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная червячная передвижная г/п 0,5 т высота подъема 7 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль ручная червячная передвижная г/п 0,5 т высота подъема 7 м', [Description] = N'Тип/марка: 289775-07-ВК1.ОЛ3
ID по каталогу: 228508', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 129, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная червячная передвижная г/п 0,5 т высота подъема 7 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная червячная передвижная г/п 2 т, высота подъема- 7 м. Исполнение - общепромышленное', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль ручная червячная передвижная г/п 2 т, высота подъема- 7 м. Исполнение - общепромышленное', N'ID по каталогу: 248721', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 130, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная червячная передвижная г/п 2 т, высота подъема- 7 м. Исполнение - общепромышленное', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль ручная червячная передвижная г/п 2 т, высота подъема- 7 м. Исполнение - общепромышленное', [Description] = N'ID по каталогу: 248721', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 130, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная червячная передвижная г/п 2 т, высота подъема- 7 м. Исполнение - общепромышленное', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная шестеренная передвижная во взрывобезопасном исполнении', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль ручная шестеренная передвижная во взрывобезопасном исполнении', N'Тип/марка: ТРШБ-2
ID по каталогу: 77811', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 131, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная шестеренная передвижная во взрывобезопасном исполнении', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль ручная шестеренная передвижная во взрывобезопасном исполнении', [Description] = N'Тип/марка: ТРШБ-2
ID по каталогу: 77811', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 131, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная шестеренная передвижная во взрывобезопасном исполнении', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 1 т, Н=6м, Общепромышленное исполнение. Упраление с пола. Условие работы У3, в помещении, температура эксплуатации от -20оС до +40оС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль электрическая г/п 1 т, Н=6м, Общепромышленное исполнение. Упраление с пола. Условие работы У3, в помещении, температура эксплуатации от -20оС до +40оС.', N'ID по каталогу: 247186', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 132, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 1 т, Н=6м, Общепромышленное исполнение. Упраление с пола. Условие работы У3, в помещении, температура эксплуатации от -20оС до +40оС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль электрическая г/п 1 т, Н=6м, Общепромышленное исполнение. Упраление с пола. Условие работы У3, в помещении, температура эксплуатации от -20оС до +40оС.', [Description] = N'ID по каталогу: 247186', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 132, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 1 т, Н=6м, Общепромышленное исполнение. Упраление с пола. Условие работы У3, в помещении, температура эксплуатации от -20оС до +40оС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 10т. Высота подъема 21м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль электрическая г/п 10т. Высота подъема 21м', N'Тип/марка: 296204-4C0000-ОП-ПЗ.ОЛ25
ID по каталогу: 307865', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 133, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 10т. Высота подъема 21м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль электрическая г/п 10т. Высота подъема 21м', [Description] = N'Тип/марка: 296204-4C0000-ОП-ПЗ.ОЛ25
ID по каталогу: 307865', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 133, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 10т. Высота подъема 21м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 5т. Высота подъема 21м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль электрическая г/п 5т. Высота подъема 21м', N'Тип/марка: 3,2-10,8 - 9 -9 -380-У3
ID по каталогу: 307864', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 134, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 5т. Высота подъема 21м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль электрическая г/п 5т. Высота подъема 21м', [Description] = N'Тип/марка: 3,2-10,8 - 9 -9 -380-У3
ID по каталогу: 307864', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 134, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 5т. Высота подъема 21м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 5т. Профиль подкранового пути 36А Высота подъема 9м.Взрывозащищенное исполнение.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль электрическая г/п 5т. Профиль подкранового пути 36А Высота подъема 9м.Взрывозащищенное исполнение.', N'Тип/марка: 295243-02-06-ТХ.ОЛ1
ID по каталогу: 303461', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 135, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 5т. Профиль подкранового пути 36А Высота подъема 9м.Взрывозащищенное исполнение.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль электрическая г/п 5т. Профиль подкранового пути 36А Высота подъема 9м.Взрывозащищенное исполнение.', [Description] = N'Тип/марка: 295243-02-06-ТХ.ОЛ1
ID по каталогу: 303461', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 135, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 5т. Профиль подкранового пути 36А Высота подъема 9м.Взрывозащищенное исполнение.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая грузоподъемностью 1 т. Климатическое исполнение-У3. Управление с пола. Температура эксплуатации от +5 до +40. Высота подъема 12 м. Общепромышленное испонение', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль электрическая грузоподъемностью 1 т. Климатическое исполнение-У3. Управление с пола. Температура эксплуатации от +5 до +40. Высота подъема 12 м. Общепромышленное испонение', N'ID по каталогу: 307932', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 136, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая грузоподъемностью 1 т. Климатическое исполнение-У3. Управление с пола. Температура эксплуатации от +5 до +40. Высота подъема 12 м. Общепромышленное испонение', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль электрическая грузоподъемностью 1 т. Климатическое исполнение-У3. Управление с пола. Температура эксплуатации от +5 до +40. Высота подъема 12 м. Общепромышленное испонение', [Description] = N'ID по каталогу: 307932', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 136, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая грузоподъемностью 1 т. Климатическое исполнение-У3. Управление с пола. Температура эксплуатации от +5 до +40. Высота подъема 12 м. Общепромышленное испонение', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая грузоподъемностью 3,2 т, высота подъема 6 м.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль электрическая грузоподъемностью 3,2 т, высота подъема 6 м.', N'ID по каталогу: 245831', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 137, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая грузоподъемностью 3,2 т, высота подъема 6 м.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль электрическая грузоподъемностью 3,2 т, высота подъема 6 м.', [Description] = N'ID по каталогу: 245831', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 137, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая грузоподъемностью 3,2 т, высота подъема 6 м.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая канатная передвижная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль электрическая канатная передвижная', N'ID по каталогу: 10710', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 138, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая канатная передвижная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль электрическая канатная передвижная', [Description] = N'ID по каталогу: 10710', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 138, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая канатная передвижная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая передвижная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль электрическая передвижная', N'Венткамера
Тип/марка: Опросный лист 289600-10-ТХ.ОЛ.3
ID по каталогу: 66213', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 139, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая передвижная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль электрическая передвижная', [Description] = N'Венткамера
Тип/марка: Опросный лист 289600-10-ТХ.ОЛ.3
ID по каталогу: 66213', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 139, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая передвижная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая с неприводным механизмом передвижения, г/п 5т Высота подъема 5,5 м.Взрывозащищенное исполнение. Профиль пути 30А.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Таль электрическая с неприводным механизмом передвижения, г/п 5т Высота подъема 5,5 м.Взрывозащищенное исполнение. Профиль пути 30А.', N'Тип/марка: 295243-02-06-ТХ.ОЛ3
ID по каталогу: 303518', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 140, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая с неприводным механизмом передвижения, г/п 5т Высота подъема 5,5 м.Взрывозащищенное исполнение. Профиль пути 30А.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Таль электрическая с неприводным механизмом передвижения, г/п 5т Высота подъема 5,5 м.Взрывозащищенное исполнение. Профиль пути 30А.', [Description] = N'Тип/марка: 295243-02-06-ТХ.ОЛ3
ID по каталогу: 303518', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 140, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая с неприводным механизмом передвижения, г/п 5т Высота подъема 5,5 м.Взрывозащищенное исполнение. Профиль пути 30А.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка моторная шлейфовая г/п 2 т', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Тележка моторная шлейфовая г/п 2 т', N'Тип/марка: 295296-01-ТХ2.ОЛ-4
ID по каталогу: 293157', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 141, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка моторная шлейфовая г/п 2 т', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Тележка моторная шлейфовая г/п 2 т', [Description] = N'Тип/марка: 295296-01-ТХ2.ОЛ-4
ID по каталогу: 293157', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 141, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка моторная шлейфовая г/п 2 т', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 10 т для печного участка Назначение: Для перевозки оборудования и материалов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Тележка передаточная г/п 10 т для печного участка Назначение: Для перевозки оборудования и материалов', N'Тип/марка: 296204-4B0000-ОП-ПЗ.ОЛ23
ID по каталогу: 308517', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 142, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 10 т для печного участка Назначение: Для перевозки оборудования и материалов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Тележка передаточная г/п 10 т для печного участка Назначение: Для перевозки оборудования и материалов', [Description] = N'Тип/марка: 296204-4B0000-ОП-ПЗ.ОЛ23
ID по каталогу: 308517', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 142, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 10 т для печного участка Назначение: Для перевозки оборудования и материалов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 100 т для ВШМ Назначение: Перевозка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Тележка передаточная г/п 100 т для ВШМ Назначение: Перевозка оборудования', N'Тип/марка: 296204-4D0000-ОП-ПЗ.ОЛ15
ID по каталогу: 308508', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 143, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 100 т для ВШМ Назначение: Перевозка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Тележка передаточная г/п 100 т для ВШМ Назначение: Перевозка оборудования', [Description] = N'Тип/марка: 296204-4D0000-ОП-ПЗ.ОЛ15
ID по каталогу: 308508', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 143, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 100 т для ВШМ Назначение: Перевозка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 120 т для склада рулонов Назначение: Перевозка горячекатанных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Тележка передаточная г/п 120 т для склада рулонов Назначение: Перевозка горячекатанных рулонов', N'Тип/марка: 296204-4E0000-ОП-ПЗ.ОЛ9
ID по каталогу: 308501', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 144, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 120 т для склада рулонов Назначение: Перевозка горячекатанных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Тележка передаточная г/п 120 т для склада рулонов Назначение: Перевозка горячекатанных рулонов', [Description] = N'Тип/марка: 296204-4E0000-ОП-ПЗ.ОЛ9
ID по каталогу: 308501', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 144, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 120 т для склада рулонов Назначение: Перевозка горячекатанных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 120 т для склада слябов Назначение: Перевозка стопок слябов или клещевого захвата на складе слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Тележка передаточная г/п 120 т для склада слябов Назначение: Перевозка стопок слябов или клещевого захвата на складе слябов', N'Тип/марка: 296204-4А0000-ОП-ПЗ.ОЛ6
ID по каталогу: 305000', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 145, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 120 т для склада слябов Назначение: Перевозка стопок слябов или клещевого захвата на складе слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Тележка передаточная г/п 120 т для склада слябов Назначение: Перевозка стопок слябов или клещевого захвата на складе слябов', [Description] = N'Тип/марка: 296204-4А0000-ОП-ПЗ.ОЛ6
ID по каталогу: 305000', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 145, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 120 т для склада слябов Назначение: Перевозка стопок слябов или клещевого захвата на складе слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тепловая завеса L=1000 мм, N=6 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Тепловая завеса L=1000 мм, N=6 кВт', N'Входные двери в здание
ID по каталогу: 272551', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 146, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тепловая завеса L=1000 мм, N=6 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Тепловая завеса L=1000 мм, N=6 кВт', [Description] = N'Входные двери в здание
ID по каталогу: 272551', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 146, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тепловая завеса L=1000 мм, N=6 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тепловая завеса L=1500 мм, N=9 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Тепловая завеса L=1500 мм, N=9 кВт', N'Входные двери в здание
ID по каталогу: 272565
ID обор. полный: 181405', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 147, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тепловая завеса L=1500 мм, N=9 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Тепловая завеса L=1500 мм, N=9 кВт', [Description] = N'Входные двери в здание
ID по каталогу: 272565
ID обор. полный: 181405', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 147, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тепловая завеса L=1500 мм, N=9 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Трансивер', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Трансивер', N'Тип/марка: Модуль оптический Cisco QSFP-100G-LR4-S
ID по каталогу: 315305', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 148, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Трансивер', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Трансивер', [Description] = N'Тип/марка: Модуль оптический Cisco QSFP-100G-LR4-S
ID по каталогу: 315305', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 148, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Трансивер', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка вакуумной пылеуборки АССклин в комплекте с одним сменным фильтром', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Установка вакуумной пылеуборки АССклин в комплекте с одним сменным фильтром', N'ID по каталогу: 203569', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 149, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка вакуумной пылеуборки АССклин в комплекте с одним сменным фильтром', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Установка вакуумной пылеуборки АССклин в комплекте с одним сменным фильтром', [Description] = N'ID по каталогу: 203569', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 149, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка вакуумной пылеуборки АССклин в комплекте с одним сменным фильтром', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка вакуумной пылеуборки ЦиклАСС в комплекте с одним сменным фильтром', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Установка вакуумной пылеуборки ЦиклАСС в комплекте с одним сменным фильтром', N'Кабельный подвал
ID по каталогу: 204640', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 150, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка вакуумной пылеуборки ЦиклАСС в комплекте с одним сменным фильтром', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Установка вакуумной пылеуборки ЦиклАСС в комплекте с одним сменным фильтром', [Description] = N'Кабельный подвал
ID по каталогу: 204640', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 150, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка вакуумной пылеуборки ЦиклАСС в комплекте с одним сменным фильтром', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка приточная 20500м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Установка приточная 20500м3/ч', N'Электротехническое помещение
Тип/марка: Спецификация №4100101319 от 17.06.2021 
291553-04-ОВ.ОЛ2
ID по каталогу: 174842', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 151, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка приточная 20500м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Установка приточная 20500м3/ч', [Description] = N'Электротехническое помещение
Тип/марка: Спецификация №4100101319 от 17.06.2021 
291553-04-ОВ.ОЛ2
ID по каталогу: 174842', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 151, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка приточная 20500м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка приточная L=2000 м3/ч; Р=300Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Установка приточная L=2000 м3/ч; Р=300Па', N'Электропомещения
Тип/марка: 292481-ОВ.ОЛ
ID по каталогу: 164352', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 152, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка приточная L=2000 м3/ч; Р=300Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Установка приточная L=2000 м3/ч; Р=300Па', [Description] = N'Электропомещения
Тип/марка: 292481-ОВ.ОЛ
ID по каталогу: 164352', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 152, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка приточная L=2000 м3/ч; Р=300Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для запрессовки металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Устройство для запрессовки металлографических шлифов', N'Устройство для запрессовки', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 153, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для запрессовки металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Устройство для запрессовки металлографических шлифов', [Description] = N'Устройство для запрессовки', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 153, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для запрессовки металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для опрессовки и продувки', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Устройство для опрессовки и продувки', N'Тип/марка: А-УОП
ID по каталогу: 254481', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 154, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для опрессовки и продувки', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Устройство для опрессовки и продувки', [Description] = N'Тип/марка: А-УОП
ID по каталогу: 254481', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 154, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для опрессовки и продувки', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для полирования металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Устройство для полирования металлографических шлифов', N'Устройство для полирования', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 155, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для полирования металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Устройство для полирования металлографических шлифов', [Description] = N'Устройство для полирования', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 155, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для полирования металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для резки металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Устройство для резки металлографических шлифов', N'Устройство для резки', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 156, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для резки металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Устройство для резки металлографических шлифов', [Description] = N'Устройство для резки', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 156, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для резки металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Фильтр песчаный D=4000 мм', CHAR(0), N'')), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Фильтр песчаный D=4000 мм', N'SF301-1...SF301-42, SF201-1...SF201-40', NULL, NULL, 1, 0, NULL, 157, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Фильтр песчаный D=4000 мм', CHAR(0), N'')), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Фильтр песчаный D=4000 мм', [Description] = N'SF301-1...SF301-42, SF201-1...SF201-40', [ClassifierId] = NULL, [UnitId] = NULL, [Coefficient] = 1, [IsAssessment] = 0, [SortOrder] = 157, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Фильтр песчаный D=4000 мм', CHAR(0), N'')), 2));
END;

IF NOT EXISTS (SELECT 1 FROM [Equipments] WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Фильтр-пресс', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)))
BEGIN
    INSERT INTO [Equipments] ([Name], [Description], [ClassifierId], [UnitId], [Coefficient], [IsAssessment], [ParentId], [SortOrder], [IsArchived], [CreatedAt], [CatalogKeyHash])
    VALUES (N'Фильтр-пресс', N'FP401', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 1.0000, 1, NULL, 158, 0, SYSUTCDATETIME(), LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Фильтр-пресс', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)));
END;
ELSE
BEGIN
    UPDATE [Equipments] SET [Name] = N'Фильтр-пресс', [Description] = N'FP401', [ClassifierId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [Coefficient] = 1.0000, [IsAssessment] = 1, [SortOrder] = 158, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [CatalogKeyHash] = LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Фильтр-пресс', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2));
END;

-- 2. Archive equipment removed from source
UPDATE [Equipments] SET [IsArchived] = 1, [ModifiedAt] = SYSUTCDATETIME()
WHERE [IsArchived] = 0
  AND [ProjectId] IS NULL
  AND NOT EXISTS (SELECT 1 FROM [ProjectEquipments] pe WHERE pe.[EquipmentId] = [Equipments].[Id])
  AND [CatalogKeyHash] NOT IN (
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочно-модульная насосная станция водоснабжения производительностью18-54 м3/ч, напор 30 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочно-модульная станция водяного/пенного пожаротушения', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочный тепловой пункт с зависимой схемой отопления', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Блочный тепловой пункт с зависимой схемой системы отопления и с закрытой схемой ГВС', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Вытяжная установка L=3500 м3/ч, Р=600 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Вытяжной канальный вентилятор L=1060 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Дымосос ДПЭ-7(1ЦМ) в комплекте с рукавом рукавом всасывающим 5м, воздуховодом специальным одинарным и рукавом напорным 10м, дополнительно: - узлом стыковочным УС-1вв EI30 (для напорного рукава) врезным размером 200х200мм - 1шт.; - узлом стыковочным УС-1в EI30 (однонаправленный, только отсос) врезным размером 200х200 -1шт.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Емкость подземная дренажная (40 м3)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор DN1000 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно с шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор DN800 мм, PN10 кгс/см2 поворотный, дисковый. Присоединение - фланцевое. Управление - электропривод. Комплектно со шкафом управления. Комплектно с ответными фланцами, прокладками и крепежом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор шиберный 1000х1000', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Затвор шиберный ножевой DN800', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Комплект приточных установок (1 раб., 1 рез.) L=13000 м3/ч с компрессорно-конденсаторными блоками и клапанами', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран Ду32 мм Ру16 кгс/см2 муф.', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой г/п 50 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А5, ГЗМ - крюк;', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой грейферный двухбалочный г/п 10+10 т Пролет 30 м; ГЗМ - грейфер + крюк; Назначение – выгрузка окалины.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой грейферный двухбалочный г/п 5 т Пролет 16 м; Назначение – выгрузка окалины.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный г/п 16 т Пролет 34 м, Н=22 м, ГЗМ - крюк; Назначение – для ремонтных работ', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 10 т', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 110/25 т Пролет 28 м, Н=13 м, ГЗМ - крюк; Назначение – транспортировка валков и валковых опор', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 120/25 т Пролет 22 м, Н=19 м, ГЗМ - двурогий крюк; Назначение – транспортировка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 120/32 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 28 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 45 т Пролет 37 м, Н=22 м, ГЗМ - двурогий крюк; Назначение – транспортировка слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 28 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 50/12,5 т Пролет 34 м, Н=11 м, ГЗМ - однорогий крюк; Назначение – транспортировка горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 60/20 т Пролет 28 м, Н=21 м, ГЗМ - однорогий крюк; Назначение – транспортировка оборудования и горячекатаных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой двухбалочный опорный г/п 60/25 т Пролет 28 м, Н=13 м, ГЗМ - однорогий крюк; Назначение – транспортировка валков и валковых опор', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой клещевой двухбалочный г/п 120 т Пролет 31 м (при ширине пролета склада 33 м), Н=12 м, Режим работы А8, ГЗМ - клещи г/п 120 т с электромеханическим приводом, крюк основной траверсы г/п 80 т; Назначение – транспортировка слябов (выгрузка/загрузка на рольганг).', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой ручной однобалочный подвесной г/п 1,0т, пролет крана- 4,2 м, общая длина крана-5,4м, расстояние от оси тягового колеса 3700мм Категория размещения У3. Исполнение взрывозащищенное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной г/п 3,2т, пролет 9м, высота подъема 8 м; полная длина 11 м. Температура окружающей среды -20...+40 град.С Исполнение общепромышленное. Режим работы - А3.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной общепромышленного исполнения. В комплекте с талью производства Болгария Климатическое исполнение У3. Режим работы А3. Температура окружающей среды -20 +40', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мостовой электрический однобалочный подвесной общепромышленного испонения. Таль с малой строительной высотой Т320-511', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран мотсовой электрический однобалочный г/п 5 т. Пролет 15 м, консоли 2х0,6 м, высота подъема 12 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 10 т Пролет 15 м, Н=14 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 10 т Пролет 6 м, Н=18 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 12 м, Н=9 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 22 м, Н=912м, ГЗМ - крюк; Назначение – ремонт и обслуживание фильтров.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п 5 т Пролет 6 м, Н=12 м, ГЗМ - крюк; Назначение – ремонт и обслуживание ПС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п3,2т Пролет 12 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран подвесной однобалочный г/п3,2т Пролет 9 м, H=9 м, ГЗМ - крюк. Исполнение - взрыво-пожаробезопасное.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран полукозловой электрический г/п 25 т Пролет 10 м (при ширине пролёта - 30 м); Н=7,3 м, ГЗМ - крюк; Назначение – обслуживание валковых опор.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран полукозловой электрический магнитный г/п на магнитах 50 т Пролет 14 м (при ширине пролета склада 33 м), Н=3,25 м, ГЗМ - грузовые электромагниты 4 шт. с общей грузо-подъемностью 50 т; Назначение – транспортировка и кантовка сляба на площадке обработки слябов.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран трехходовой для подключения манометра Ду15 Ру16 с ручкой материал - латунь рабочая среда - вода температура среды - -40°C до +40°C герметичность - класс "D" по ГОСТ 9544; климатическое исполнение – У3 по гост 15150 тип присоединения - резьба внутренняя 1/2"', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой DN80 PN16 стальной фланцевый полнопроходный со стальной рукояткой в комплекте с ответными фланцами, крепежом и прокладкам материал - сталь углеродистая рабочая среда - вода температура среды - -40...+200С герметичность - класс "А" по ГОСТ 9544 климатическое исполнение – У по гост 15150', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой стальной муфтовый Ду = 25 мм, Ру = 1,6 МПа Раб. среда - вода.', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровой фланцевый Ду 40 мм; Ру1,6 МПа. Среда - вода. Температура до150 °С. Корпус и шар - нержавеющая сталь; Уплотнение - фторопласт. Ручное управление. Класс герметичности "А". Климатическое исполнение - У2. С КОФ, прокладками и крепежом.', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка - бабочка. Присоединение резьба В-В. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 15, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 20, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый латунный муфтовый DN 25, Ру 16 кгс/см2. Ручка "бабочка". Присоединение резьба В-Н. Рабочая среда - вода. Температура рабочей среды, °С - от +1 до +120. Класс герметичности затвора - "А" по ГОСТ 9544-2015г.', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду25; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2005 Класс А -, Материал (корпуса) Латунь ЛЦ40СД -, Температура рабочей среды Вода до 150. гр.С', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду32; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый муфтовый проходной Ду50; Ру 16кгс/см2. Присоединение к трубопроводу муфтовое. Герметичность затвора по ГОСТ 9544-2015. Класс А, Материал (корпуса) - Латунь ЛЦ40СД, Температура рабочей среды - вода до 150. гр.С', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран шаровый стальной муфтовый материал корпуса- сталь рабочая среда - техническая вода температура рабочей среды +5...+40 град С герметичность - класс "А" по ГОСТ 9544-2015. климатическое исполнение У1.', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический мостовой опорный г/п 15т.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арматуры и др. в помещении комплекса водоподготовки.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран электрический однобалочный мостовой г/п 10 т Пролет 16,5 м; Назначение – монтаж/демонтаж, техническое обслужива-ние насосного оборудования, запорной арма-туры и др. в помещении насосной станции.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Кран-балка г/п 10т для обслуживания площадки балансиров', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Лебедка ручная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ленточнопильный станок с ЧПУ', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Машина для ударных испытаний, в комплекте с устройством для охлаждения образцов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Машина переносная газовой резки', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Механическая задвижка DN 820 с электроприводом', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная автоматическая насосная станция. Рабочая среда - питьевая вода.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная вытяжная установка L=19000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная компрессорная станция (МКС). Производительность станции по осушенному сжатому воздуху на выходе - 332 Нм3/мин.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная приточная установка, расход воздуха L=500 м3/ч, сторона обслуживания - снизу, со встроенной автоматикой.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульная станция осушки сжатого воздуха Устанавливается на улице. В составе: адсорбционный осушитель с горячей регенерацией Atlas Copco BD3000 - 2 шт. (1 раб.,1 рез.), набор фильтров ( PD, BD,фильтр вихревой) на каждый осушитель Класс чистоты выздуха на входе - 7:9:1 по ГОСТ Р ИСО 8573-1-2016 Класс чистоты воздуха на выходе - 1:2:1 по ГОСТ Р ИСО 8573-1-2016 Рабочее давление 6-8 бар Температура воздуха на входе до +40°С Производительность - 10000 м3/час', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульное бытовое помещение и ремонтная мастерская', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Модульное здание АБК', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительная емкость для сбора бытовых стоков V=15 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительная емкость для сбора дождевых стоков V=35 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Накопительный бак для сбора масла', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос для аварийных стоков', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос дренажный погружной моноблочный для грязной воды подача (номин.) - 10 м3/ч; напор - 10 м; мощность двигателя - 1 кВт; частота вращения - 3000 об/мин; ток- 8 А; напряжение сети- 220 В; Комплект поставки: электродвигатель, поплавковый выключатель, шнур питания L=10м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос дренажныйй погружной моноблочный для грязной воды с поплавковым выключателем Q= 6 м3/ч, Н=10 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос подачи осветленной воды к горизонтальному отстойнику', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный артезианский погружной Q=600 м3/ч, напор 28 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный горизонтальный двухстороннего входа Д4000-95 с электродвигателем АОД -1600-6У1', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный горизонтальный консольный Q=1100 м3/ч, напор 45-50 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос центробежный, двустороннего входа - производительность Q=450 м3/ч; - напор Н=45 м; - давление на входе не более 0,6 МПа; - индекс обточки рабочего колеса а (уменьшенный диаметр рабочего колеса); - номинальный диаметр всасывающего патрубка 250 мм; - номинальный диаметр напорного патрубка 150 мм; - мощность 90 кВт; - напряжение 380 В; - частота вращения 980 об/мин.; - частота 50 Hz; - род тока - переменный; - комплект поставки: ответные фланцы, крепеж, прокладки, электродвигатель, рама с крепежом к насосу, установочными винтами, регулировочными подкладками', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос циркуляционный G=207 м3/ч; H=50 м; с электродвигателем U=380В, P=45кВт в комплекте с ответными фланцами, прокладками и крепежом Рабочая среда: сетевая вода (Pp=1,6 МПа; Тр=95°С)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насос циркуляционный, DN25, PN25, длина 250 мм, присоединение — фланец', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насосная станция питьевого водоснабжения производительностью 0,1-0,8 м3/ч, напор 10 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Насосный агрегат консольный. Подача Q=100 м3/час, Н=80 м.в.ст. N=45,0 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка (1 - рабочая, 1 - резервная) L=22000 м3/ч, Рсети=900 Па, с водяным калорифером, сторона обслуживания слева. В комплекте: - приточная установка - 2 шт.; - узел обвязки - 2 шт.; - шкаф автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 1 600 м3/ч, с электрокалорифером', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 2500 м3/ч, Нсети = 700Па С электрокалорифером, с воздухоохлажением. С автоматикой.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 26 000 м3/ч, H.сети=500 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 410 м3/ч, H.сети=300 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 5 000 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 5 200 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 500 м3/ч, H.сети=300 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 600 м3/ч, с электрокалорифером', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 650 м3/ч, Нсети = 400Па с электрокалорифером.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L= 80000 м3/ч, Р=1500 Па с холодильной машиной с комплектом автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=11 200 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=15000 м3/ч, P=900 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1600м3/ч, P=900 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=1750 куб.м/ч Рсети=500Па с резервным вентилятором', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=21000 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=2200 куб.м/ч Рсети=450 Па подвесная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=28000 м3/ч, Hсети=700 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=30000 м3/ч, P=1000 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=3250 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=43 600 м3/ч с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=5570м3/ч, Р=500 Па с встроенной автоматикой, в комплекте с узлом регулирования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=7200 м3/ч, Р=600 Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=8800 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка L=90 000 м3/ч, с водяным воздухонагревателем', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П1. L=5570м3/ч, P=650Па, в комплекте с автоматикой, шкафом управления, со смесительным узлом.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П6, клапаны ВЕ3, компрессорно-конденсаторный блок. L=4 500 м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточная установка П9 (1 раб./1 резер.) L=3 000 м3/ч, компрессорно-конденсаторный блок (1 раб. /1 резер.)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Приточные установки L= 80000 м3/ч, Р=1500 Па с комплектом автоматики', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Протяжный станок для ударных испытаний образцов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пульт цифровой диспетчерской громкоговорящей связи на 24 кнопки (без WiFi модуля)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iн=320А, арт. 109881 - 1шт. Выключатели распределения: ВА57-35-340010-250А-2500-690AC-УХЛ3-КЭАЗ, Iн=250 А, арт. 108600 - 2 шт.; ВА57-35-340010-160А-1600-690AC-УХЛ3-КЭАЗ, Iн=160 А, арт. 108586 - 2 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 2 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 2 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники снизу : СКПО-40 - 8шт., СКПО-22 - 4шт.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-200А-2000-400AC-УХЛ3-КЭАЗ, Iн=200А, арт. 109314 - 1шт. Выключатели распределения: ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iн=80 А, арт. 108428 - 1 шт.; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iн=63 А, арт. 108440 - 1 шт.; ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 1 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверху : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -6шт., СКПО-22 -2 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х50)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный ~380/220В, Выключатель ввода: ВА57Ф35-340010-250А-2500-400AC-УХЛ3-КЭАЗ, Iн=250А, арт. 109319 - 1шт. Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iн=50 А, арт. 108439 - 3 шт.; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iн=40 А, арт. 108434 - 6 шт.; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iн=31,5 А, арт. 108433 - 3 шт.; с нулевой рабочей шиной N и защитной шиной PE Степень защиты IP54 Сальники сверхуу : СКПО-40 - 1шт., СКПО-32 -6 шт., СКПО-22 -2 шт. Сальники снизу : СКПО-40 - 2шт., СКПО-32 -2 шт., СКПО-22 -6 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х70)', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-100А-1000-400AC-УХЛ3-КЭАЗ, Iр=100А, арт. №109286; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 5 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 1 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 12 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-FRLS-5х50.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля сверху. Выключатель ввода: ВА57Ф35-340010-160А-1600-400AC-УХЛ3-КЭАЗ, Iр=160А, арт. №109307; Выключатели распределения: ВА57-31-340010-50А-800-690AC-УХЛ3-КЭАЗ, Iр=50А - 2 шт., арт. №108439; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 4 шт., арт. №108434; ВА57-31-340010-31,5А-400-690AC-УХЛ3-КЭАЗ, Iр=31,5А - 4 шт., арт. №108433; ВА57-31-340010-25А-400-690AC-УХЛ3-КЭАЗ, Iр=25А - 2 шт., арт. №108432. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники сверху: PG 48 (DKC, арт. 53400) - 1 шт., PG 36 (DKC, арт. 53200) - 8 шт., PG 29 (DKC, арт. 53100) - 4 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-5х95.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Пункт распределительный AC 0,4/0,23 кВ, навесного исполнения, подвод питающего кабеля снизу. Выключатель ввода: ВА57-39-340010-320А-3200-690AC-УХЛ3-КЭАЗ, Iр=320А, арт. №109881; Выключатели распределения: ВА57-35-340010-200А-2000-690AC-УХЛ3-КЭАЗ, Iр=200А - 1 шт., арт. №108594; ВА57-35-340010-100А-1000-690AC-УХЛ3-КЭАЗ, Iр=100А - 1 шт., арт. №108566; ВА57-31-340010-100А-1200-690AC-УХЛ3-КЭАЗ, Iр=100А - 3 шт., арт. №108427; ВА57-31-340010-80А-1200-690AC-УХЛ3-КЭАЗ, Iр=80А - 2 шт., арт. №108428; ВА57-31-340010-63А-800-690AC-УХЛ3-КЭАЗ, Iр=63А - 4 шт., арт. №108440; ВА57-31-340010-40А-400-690AC-УХЛ3-КЭАЗ, Iр=40А - 1 шт., арт. №108434. с нулевой рабочей шиной N и защитной шиной PE. Степень защиты IP54. Сальники снизу: PG 48 (DKC, арт. 53400) - 3 шт., PG 42 (DKC, арт. 53300) - 10 шт., PG 36 (DKC, арт. 53200) - 1 шт. Обеспечить подключение к вводному автомату кабеля ВВГнг(А)-LS-2(5х95).', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Расширительный бак объёмом 15 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Резак РПМ3 РПК-М L=1300 мм', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Резак РПМ3-РПК-М L=2000 мм', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ресивер (воздухосборник) емкостью 25 м3', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Ресивер диаметром 1,6 м, объемом 10 м3, расчетное давление 0,9 Мпа, в комплекте с арматурой и КИП', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная червячная передвижная г/п 0,5 т высота подъема 7 м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная червячная передвижная г/п 2 т, высота подъема- 7 м. Исполнение - общепромышленное', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль ручная шестеренная передвижная во взрывобезопасном исполнении', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 1 т, Н=6м, Общепромышленное исполнение. Упраление с пола. Условие работы У3, в помещении, температура эксплуатации от -20оС до +40оС.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 10т. Высота подъема 21м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 5т. Высота подъема 21м', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая г/п 5т. Профиль подкранового пути 36А Высота подъема 9м.Взрывозащищенное исполнение.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая грузоподъемностью 1 т. Климатическое исполнение-У3. Управление с пола. Температура эксплуатации от +5 до +40. Высота подъема 12 м. Общепромышленное испонение', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая грузоподъемностью 3,2 т, высота подъема 6 м.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая канатная передвижная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая передвижная', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Таль электрическая с неприводным механизмом передвижения, г/п 5т Высота подъема 5,5 м.Взрывозащищенное исполнение. Профиль пути 30А.', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка моторная шлейфовая г/п 2 т', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 10 т для печного участка Назначение: Для перевозки оборудования и материалов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 100 т для ВШМ Назначение: Перевозка оборудования', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 120 т для склада рулонов Назначение: Перевозка горячекатанных рулонов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тележка передаточная г/п 120 т для склада слябов Назначение: Перевозка стопок слябов или клещевого захвата на складе слябов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тепловая завеса L=1000 мм, N=6 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Тепловая завеса L=1500 мм, N=9 кВт', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Трансивер', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка вакуумной пылеуборки АССклин в комплекте с одним сменным фильтром', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка вакуумной пылеуборки ЦиклАСС в комплекте с одним сменным фильтром', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка приточная 20500м3/ч', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Установка приточная L=2000 м3/ч; Р=300Па', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для запрессовки металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для опрессовки и продувки', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для полирования металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Устройство для резки металлографических шлифов', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Фильтр песчаный D=4000 мм', CHAR(0), N'')), 2)),
LOWER(CONVERT(varchar(64), HASHBYTES('SHA2_256', CONCAT(N'Фильтр-пресс', CHAR(0), COALESCE(CAST((SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт') AS nvarchar(20)), N''))), 2))
);

COMMIT TRANSACTION;
GO
