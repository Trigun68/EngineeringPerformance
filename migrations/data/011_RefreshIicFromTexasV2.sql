-- ============================================================================
-- Refresh IIC groups from «Спр_Техас» (replace existing rows by Code)
-- Источник: _Расчет_трудоемкости_2_версии.xl.xlsx
-- ============================================================================
SET NOCOUNT ON;
BEGIN TRANSACTION;

-- 1. Insert missing directions
IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-1', N'Общий бетон', N'Общий объем бетона — только кубические ярды, без свай. Трудозатраты включают плиты, фундаменты (без свай), бетонные конструкции и все закладные детали.', NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 0, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-1-1', N'Общие плиты', N'Общий объем плит и трудозатраты только если данные невозможно разделить по вышеприведенным категориям.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 1, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-1-1-1', N'Плиты на грунте и опертые плиты', N'Включает плиты на грунте и опертые плиты по чертежам «Выпущено для строительства» (IFC). Трудозатраты на проектирование включают также часы на проектирование всех закладных деталей.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 2, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-1-1-2', N'Площадочное покрытие', N'Покрытия в пределах проекта, за исключением автодорог.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 3, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-1-2', N'Общие фундаменты (без свай)', N'Объем фундаментов — только кубические ярды бетона. Трудозатраты исключают сваи.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 4, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-1-2-1', N'Сваи (каждая)', N'Общее количество свай по чертежам IFC. Трудозатраты включают проектирование всех закладных деталей.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 5, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-1-2-2', N'Фундаменты (<3,8 м³)', N'Общий объем бетона фундаментов (<5 ярд³). Трудозатраты включают закладные детали. Сваи не включаются.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 6, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-3')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-1-2-3', N'Фундаменты (≥3,8 м³)', N'Общий объем бетона фундаментов (≥5 ярд³). Трудозатраты включают закладные детали. Сваи не включаются.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 7, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-1-3', N'Бетонные конструкции', N'Включают колонны, балки, резервуары градирен, траншеи, формованные возвышенные плиты/конструкции, подпорные стены.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 8, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-2', N'Стальные конструкции', N'Сумма стальных конструкций, эстакад/мостов и вспомогательных конструкций (или комбинированных показателей, если разделение невозможно).', NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 9, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-2-1', N'Вспомогательные стальные конструкции', N'Поручни, кромочные планки, решетчатые настилы, рифленые листы, лестницы, трапы, ограждения, вспомогательные площадки, предварительно смонтированные лестницы и площадки, опорные конструкции (включая накладки), Т- и Н-образные опоры, крышки траншей, настилы.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 10, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-2-2', N'Комбинированные стальные конструкции/эстакады и мосты', N'Комбинированный показатель, если данные невозможно разделить.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 11, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-2-2-1', N'Стальные конструкции', N'Фермы, колонны, главные балки, балки, раскосы, прогоны, связи вертикальные и горизонтальные, болты и гайки.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 12, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-2-2-2', N'Эстакады и коммуникационные мосты', N'Стальные конструкции за пределами основного здания, предназначенные для поддержки трубопроводов, кабелей и кабельных лотков.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), 13, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3', N'Электрооборудование', N'Включает трансформаторы, распределительные устройства, ИБП, центры управления двигателями (ЦУД), выпрямители, двигатели, генераторы и др. Также включает трудозатраты на однолинейные схемы, принципиальные схемы и расчеты.', NULL, NULL, 14, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-1', N'Общее электрооборудование', N'Включает трансформаторы, распределительные устройства, ИБП, центры управления двигателями (ЦУД), выпрямители, двигатели, генераторы и др. Также включает трудозатраты на однолинейные схемы, принципиальные схемы и расчеты.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 15, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-1-1', N'Электрооборудование ≤600 В', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 16, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-1-2', N'Электрооборудование >600 В', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 17, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-2', N'Кабельные каналы и трубы', N'Лотки для электрических и приборных кабелей, каналы, опоры, крышки и др.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 18, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-2-1', N'Кабельные каналы (трубы) — погонные футы', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 19, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-2-2', N'Кабельные каналы (трубы) — количество трасс', N'Планы электроснабжения, кабельные и трубные схемы, соединения. Наземные/надземные и подземные.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 20, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-3')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-3', N'Лотки для кабелей', N'Лотки для электрических и приборных кабелей, каналы, опоры, крышки и др.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 21, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-4', N'Провода и кабели', N'Силовые, контрольные и заземляющие кабели.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 22, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-4-1', N'Провода и кабели (без каналов/лотков) — погонные футы', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 23, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-4-2', N'Провода и кабели — количество заделок', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4'), NULL, 24, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-5', N'Прочие электромонтажные работы', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3'), NULL, 25, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-3-5-1', N'Освещение', N'Светильники, трубы, проводка, щиты и устройства управления. Количество — число светильников.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5'), NULL, 26, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-4')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-4', N'Общие трубопроводы (только погонные)', N'Общий объем — только погонные футы. Трудозатраты включают малый и большой диаметр, инженерные опоры/подвески и фитинги. Примечание: «Горячие» трубопроводы — температура проектирования >250°F; «Холодные» — температура проектирования <–20°F. Количество трубопроводов — это трубопроводы, указанные в окончательных чертежах «Выпущено для строительства» (IFC). Это количество должно измеряться «центром к центру» через клапаны и фитинги, как и количество для строительной метрики.', NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 27, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-4-1', N'Малый диаметр (≤2½")', N'Трубы диаметром ≤2½ дюйма.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 28, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-4-2', N'Большой диаметр (≥3")', N'Трубы диаметром ≥3 дюйма.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 29, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-3')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-4-3', N'Инженерные опоры и подвески (каждая)', N'Только нестандартные опоры и подвески по чертежам IFC, включая трудозатраты на анализ напряжений и переделки.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 30, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-5')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-5', N'КИПиА', N'Примечание: Трудозатраты на КИПиА включают всё проектирование приборов и систем управления, кроме конфигурирования и программирования DCS/PLC.', NULL, NULL, 31, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-5-1', N'Контур (петля)', N'Контур включает все датчики, проводку, алгоритмы управления и исполнительные устройства для отображения или регулирования одной технологической переменной (регулирующий или индикационный контур).', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5'), NULL, 32, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-5-2', N'Маркированные приборы', N'Количество физических приборов с тегами на схемах трубопроводов и КИП (P&ID).', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 33, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-3')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-5-3', N'Вводы/выводы (I/O)', N'Включает все локальные датчики (передатчики, выключатели, приборы, термопары, RTD, анализаторы и др.) и исполнительные устройства (клапаны, сигнализация, стартеры двигателей и др.).', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5'), NULL, 34, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6', N'Общее оборудование', N'Общее количество оборудования может включать позиции, не указанные выше. Отражает прямые трудозатраты механического отдела.', NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), 35, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-1', N'Сосуды под давлением', N'Тарельчатые/насадочные колонны, колонны, реакторы/регенераторы и прочие сосуды под давлением. Полевые сборные колонны, реакторы и регенераторы должны быть включены.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 36, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-2', N'Атмосферные резервуары', N'Резервуары хранения, понтонные резервуары, бункеры/воронки/силосы/циклоны, криогенные и низкотемпературные резервуары и прочие атмосферные резервуары.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 37, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-3')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-3', N'Теплообменное оборудование', N'Теплообменники, воздушные охладители, испарители, градирни и прочее теплообменное оборудование.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 38, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-4')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-4', N'Оборудование для транспортировки материалов (с приводом)', N'Конвейеры (ленточные, цепные, решетчатые, роторные и т.д.), краны и подъемники, весы, подъемники, штабелеры, реклаймеры, погрузчики судов, пресс-компакторы, дозаторы и упаковочные машины, а также прочее оборудование для транспортировки материалов.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 39, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-6')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-6', N'Котлы и топочные нагреватели', N'Блочные и полевые котлы, топочные нагреватели, утилизационные котлы, отдельно стоящие дымовые трубы и прочее оборудование. Общий (БТЕ/ч)', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 40, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-7')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-7', N'Вращающееся оборудование (с приводом)', N'Компрессоры (центробежные/поршневые), воздуходувки, винтовые компрессоры, дозирующие/линейные насосы, насосы (центробежные/поршневые), объемные насосы, мешалки, смесители, блендеры и прочее вспомогательное оборудование. Общий (л.с.)', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 41, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-8')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-8', N'Оборудование для выработки энергии', N'Газовые и паровые турбины, дизель-генераторы и прочее оборудование для выработки энергии. Общий (кВт)', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 42, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-9')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-9', N'Прочее технологическое оборудование', N'Специализированное газовое оборудование, оборудование для хранения химических веществ, технологическое оборудование, системы удаления частиц (пылеуловители, скрубберы и т.д.), системы обработки (водоочистка и т.д.), инсинераторы и факельные системы.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 43, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-10')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-10', N'Модули и скомплектованные блоки заводского изготовления', N'Модули (частичные установки) и полные блочные установки. Блоки с несколькими единицами оборудования учитываются как одна позиция.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 44, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-11')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-11', N'Полигоны, отвалы, дамбы', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 45, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-12')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-6-12', N'Склады сырья и готовой продукции', N'', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), NULL, 46, 0, SYSUTCDATETIME());
END;

-- 2. Update existing directions (Name, Description, UnitId, SortOrder)
UPDATE [IicWorkDirections] SET [Name] = N'Общий бетон', [Description] = N'Общий объем бетона — только кубические ярды, без свай. Трудозатраты включают плиты, фундаменты (без свай), бетонные конструкции и все закладные детали.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), [SortOrder] = 0, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-1';
UPDATE [IicWorkDirections] SET [Name] = N'Общие плиты', [Description] = N'Общий объем плит и трудозатраты только если данные невозможно разделить по вышеприведенным категориям.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), [SortOrder] = 1, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-1-1';
UPDATE [IicWorkDirections] SET [Name] = N'Плиты на грунте и опертые плиты', [Description] = N'Включает плиты на грунте и опертые плиты по чертежам «Выпущено для строительства» (IFC). Трудозатраты на проектирование включают также часы на проектирование всех закладных деталей.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), [SortOrder] = 2, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-1-1-1';
UPDATE [IicWorkDirections] SET [Name] = N'Площадочное покрытие', [Description] = N'Покрытия в пределах проекта, за исключением автодорог.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), [SortOrder] = 3, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-1-1-2';
UPDATE [IicWorkDirections] SET [Name] = N'Общие фундаменты (без свай)', [Description] = N'Объем фундаментов — только кубические ярды бетона. Трудозатраты исключают сваи.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), [SortOrder] = 4, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-1-2';
UPDATE [IicWorkDirections] SET [Name] = N'Сваи (каждая)', [Description] = N'Общее количество свай по чертежам IFC. Трудозатраты включают проектирование всех закладных деталей.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [SortOrder] = 5, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-1-2-1';
UPDATE [IicWorkDirections] SET [Name] = N'Фундаменты (<3,8 м³)', [Description] = N'Общий объем бетона фундаментов (<5 ярд³). Трудозатраты включают закладные детали. Сваи не включаются.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), [SortOrder] = 6, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-1-2-2';
UPDATE [IicWorkDirections] SET [Name] = N'Фундаменты (≥3,8 м³)', [Description] = N'Общий объем бетона фундаментов (≥5 ярд³). Трудозатраты включают закладные детали. Сваи не включаются.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), [SortOrder] = 7, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-1-2-3';
UPDATE [IicWorkDirections] SET [Name] = N'Бетонные конструкции', [Description] = N'Включают колонны, балки, резервуары градирен, траншеи, формованные возвышенные плиты/конструкции, подпорные стены.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), [SortOrder] = 8, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-1-3';
UPDATE [IicWorkDirections] SET [Name] = N'Стальные конструкции', [Description] = N'Сумма стальных конструкций, эстакад/мостов и вспомогательных конструкций (или комбинированных показателей, если разделение невозможно).', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), [SortOrder] = 9, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-2';
UPDATE [IicWorkDirections] SET [Name] = N'Вспомогательные стальные конструкции', [Description] = N'Поручни, кромочные планки, решетчатые настилы, рифленые листы, лестницы, трапы, ограждения, вспомогательные площадки, предварительно смонтированные лестницы и площадки, опорные конструкции (включая накладки), Т- и Н-образные опоры, крышки траншей, настилы.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), [SortOrder] = 10, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-2-1';
UPDATE [IicWorkDirections] SET [Name] = N'Комбинированные стальные конструкции/эстакады и мосты', [Description] = N'Комбинированный показатель, если данные невозможно разделить.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), [SortOrder] = 11, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-2-2';
UPDATE [IicWorkDirections] SET [Name] = N'Стальные конструкции', [Description] = N'Фермы, колонны, главные балки, балки, раскосы, прогоны, связи вертикальные и горизонтальные, болты и гайки.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), [SortOrder] = 12, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-2-2-1';
UPDATE [IicWorkDirections] SET [Name] = N'Эстакады и коммуникационные мосты', [Description] = N'Стальные конструкции за пределами основного здания, предназначенные для поддержки трубопроводов, кабелей и кабельных лотков.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'т'), [SortOrder] = 13, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-2-2-2';
UPDATE [IicWorkDirections] SET [Name] = N'Электрооборудование', [Description] = N'Включает трансформаторы, распределительные устройства, ИБП, центры управления двигателями (ЦУД), выпрямители, двигатели, генераторы и др. Также включает трудозатраты на однолинейные схемы, принципиальные схемы и расчеты.', [UnitId] = NULL, [SortOrder] = 14, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3';
UPDATE [IicWorkDirections] SET [Name] = N'Общее электрооборудование', [Description] = N'Включает трансформаторы, распределительные устройства, ИБП, центры управления двигателями (ЦУД), выпрямители, двигатели, генераторы и др. Также включает трудозатраты на однолинейные схемы, принципиальные схемы и расчеты.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [SortOrder] = 15, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-1';
UPDATE [IicWorkDirections] SET [Name] = N'Электрооборудование ≤600 В', [Description] = N'', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [SortOrder] = 16, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-1-1';
UPDATE [IicWorkDirections] SET [Name] = N'Электрооборудование >600 В', [Description] = N'', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [SortOrder] = 17, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-1-2';
UPDATE [IicWorkDirections] SET [Name] = N'Кабельные каналы и трубы', [Description] = N'Лотки для электрических и приборных кабелей, каналы, опоры, крышки и др.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), [SortOrder] = 18, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-2';
UPDATE [IicWorkDirections] SET [Name] = N'Кабельные каналы (трубы) — погонные футы', [Description] = N'', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), [SortOrder] = 19, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-2-1';
UPDATE [IicWorkDirections] SET [Name] = N'Кабельные каналы (трубы) — количество трасс', [Description] = N'Планы электроснабжения, кабельные и трубные схемы, соединения. Наземные/надземные и подземные.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [SortOrder] = 20, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-2-2';
UPDATE [IicWorkDirections] SET [Name] = N'Лотки для кабелей', [Description] = N'Лотки для электрических и приборных кабелей, каналы, опоры, крышки и др.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), [SortOrder] = 21, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-3';
UPDATE [IicWorkDirections] SET [Name] = N'Провода и кабели', [Description] = N'Силовые, контрольные и заземляющие кабели.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), [SortOrder] = 22, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-4';
UPDATE [IicWorkDirections] SET [Name] = N'Провода и кабели (без каналов/лотков) — погонные футы', [Description] = N'', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), [SortOrder] = 23, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-4-1';
UPDATE [IicWorkDirections] SET [Name] = N'Провода и кабели — количество заделок', [Description] = N'', [UnitId] = NULL, [SortOrder] = 24, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-4-2';
UPDATE [IicWorkDirections] SET [Name] = N'Прочие электромонтажные работы', [Description] = N'', [UnitId] = NULL, [SortOrder] = 25, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-5';
UPDATE [IicWorkDirections] SET [Name] = N'Освещение', [Description] = N'Светильники, трубы, проводка, щиты и устройства управления. Количество — число светильников.', [UnitId] = NULL, [SortOrder] = 26, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-3-5-1';
UPDATE [IicWorkDirections] SET [Name] = N'Общие трубопроводы (только погонные)', [Description] = N'Общий объем — только погонные футы. Трудозатраты включают малый и большой диаметр, инженерные опоры/подвески и фитинги. Примечание: «Горячие» трубопроводы — температура проектирования >250°F; «Холодные» — температура проектирования <–20°F. Количество трубопроводов — это трубопроводы, указанные в окончательных чертежах «Выпущено для строительства» (IFC). Это количество должно измеряться «центром к центру» через клапаны и фитинги, как и количество для строительной метрики.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), [SortOrder] = 27, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-4';
UPDATE [IicWorkDirections] SET [Name] = N'Малый диаметр (≤2½")', [Description] = N'Трубы диаметром ≤2½ дюйма.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), [SortOrder] = 28, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-4-1';
UPDATE [IicWorkDirections] SET [Name] = N'Большой диаметр (≥3")', [Description] = N'Трубы диаметром ≥3 дюйма.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), [SortOrder] = 29, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-4-2';
UPDATE [IicWorkDirections] SET [Name] = N'Инженерные опоры и подвески (каждая)', [Description] = N'Только нестандартные опоры и подвески по чертежам IFC, включая трудозатраты на анализ напряжений и переделки.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [SortOrder] = 30, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-4-3';
UPDATE [IicWorkDirections] SET [Name] = N'КИПиА', [Description] = N'Примечание: Трудозатраты на КИПиА включают всё проектирование приборов и систем управления, кроме конфигурирования и программирования DCS/PLC.', [UnitId] = NULL, [SortOrder] = 31, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-5';
UPDATE [IicWorkDirections] SET [Name] = N'Контур (петля)', [Description] = N'Контур включает все датчики, проводку, алгоритмы управления и исполнительные устройства для отображения или регулирования одной технологической переменной (регулирующий или индикационный контур).', [UnitId] = NULL, [SortOrder] = 32, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-5-1';
UPDATE [IicWorkDirections] SET [Name] = N'Маркированные приборы', [Description] = N'Количество физических приборов с тегами на схемах трубопроводов и КИП (P&ID).', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [SortOrder] = 33, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-5-2';
UPDATE [IicWorkDirections] SET [Name] = N'Вводы/выводы (I/O)', [Description] = N'Включает все локальные датчики (передатчики, выключатели, приборы, термопары, RTD, анализаторы и др.) и исполнительные устройства (клапаны, сигнализация, стартеры двигателей и др.).', [UnitId] = NULL, [SortOrder] = 34, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-5-3';
UPDATE [IicWorkDirections] SET [Name] = N'Общее оборудование', [Description] = N'Общее количество оборудования может включать позиции, не указанные выше. Отражает прямые трудозатраты механического отдела.', [UnitId] = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'шт'), [SortOrder] = 35, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6';
UPDATE [IicWorkDirections] SET [Name] = N'Сосуды под давлением', [Description] = N'Тарельчатые/насадочные колонны, колонны, реакторы/регенераторы и прочие сосуды под давлением. Полевые сборные колонны, реакторы и регенераторы должны быть включены.', [UnitId] = NULL, [SortOrder] = 36, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-1';
UPDATE [IicWorkDirections] SET [Name] = N'Атмосферные резервуары', [Description] = N'Резервуары хранения, понтонные резервуары, бункеры/воронки/силосы/циклоны, криогенные и низкотемпературные резервуары и прочие атмосферные резервуары.', [UnitId] = NULL, [SortOrder] = 37, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-2';
UPDATE [IicWorkDirections] SET [Name] = N'Теплообменное оборудование', [Description] = N'Теплообменники, воздушные охладители, испарители, градирни и прочее теплообменное оборудование.', [UnitId] = NULL, [SortOrder] = 38, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-3';
UPDATE [IicWorkDirections] SET [Name] = N'Оборудование для транспортировки материалов (с приводом)', [Description] = N'Конвейеры (ленточные, цепные, решетчатые, роторные и т.д.), краны и подъемники, весы, подъемники, штабелеры, реклаймеры, погрузчики судов, пресс-компакторы, дозаторы и упаковочные машины, а также прочее оборудование для транспортировки материалов.', [UnitId] = NULL, [SortOrder] = 39, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-4';
UPDATE [IicWorkDirections] SET [Name] = N'Котлы и топочные нагреватели', [Description] = N'Блочные и полевые котлы, топочные нагреватели, утилизационные котлы, отдельно стоящие дымовые трубы и прочее оборудование. Общий (БТЕ/ч)', [UnitId] = NULL, [SortOrder] = 40, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-6';
UPDATE [IicWorkDirections] SET [Name] = N'Вращающееся оборудование (с приводом)', [Description] = N'Компрессоры (центробежные/поршневые), воздуходувки, винтовые компрессоры, дозирующие/линейные насосы, насосы (центробежные/поршневые), объемные насосы, мешалки, смесители, блендеры и прочее вспомогательное оборудование. Общий (л.с.)', [UnitId] = NULL, [SortOrder] = 41, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-7';
UPDATE [IicWorkDirections] SET [Name] = N'Оборудование для выработки энергии', [Description] = N'Газовые и паровые турбины, дизель-генераторы и прочее оборудование для выработки энергии. Общий (кВт)', [UnitId] = NULL, [SortOrder] = 42, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-8';
UPDATE [IicWorkDirections] SET [Name] = N'Прочее технологическое оборудование', [Description] = N'Специализированное газовое оборудование, оборудование для хранения химических веществ, технологическое оборудование, системы удаления частиц (пылеуловители, скрубберы и т.д.), системы обработки (водоочистка и т.д.), инсинераторы и факельные системы.', [UnitId] = NULL, [SortOrder] = 43, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-9';
UPDATE [IicWorkDirections] SET [Name] = N'Модули и скомплектованные блоки заводского изготовления', [Description] = N'Модули (частичные установки) и полные блочные установки. Блоки с несколькими единицами оборудования учитываются как одна позиция.', [UnitId] = NULL, [SortOrder] = 44, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-10';
UPDATE [IicWorkDirections] SET [Name] = N'Полигоны, отвалы, дамбы', [Description] = N'', [UnitId] = NULL, [SortOrder] = 45, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-11';
UPDATE [IicWorkDirections] SET [Name] = N'Склады сырья и готовой продукции', [Description] = N'', [UnitId] = NULL, [SortOrder] = 46, [IsArchived] = 0, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] = N'IIC-6-12';

-- 3. Fix ParentId after all rows exist
UPDATE [IicWorkDirections] SET [ParentId] = NULL WHERE [Code] = N'IIC-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1') WHERE [Code] = N'IIC-1-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1') WHERE [Code] = N'IIC-1-1-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1') WHERE [Code] = N'IIC-1-1-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1') WHERE [Code] = N'IIC-1-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2') WHERE [Code] = N'IIC-1-2-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2') WHERE [Code] = N'IIC-1-2-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2') WHERE [Code] = N'IIC-1-2-3';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1') WHERE [Code] = N'IIC-1-3';
UPDATE [IicWorkDirections] SET [ParentId] = NULL WHERE [Code] = N'IIC-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2') WHERE [Code] = N'IIC-2-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2') WHERE [Code] = N'IIC-2-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2') WHERE [Code] = N'IIC-2-2-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2') WHERE [Code] = N'IIC-2-2-2';
UPDATE [IicWorkDirections] SET [ParentId] = NULL WHERE [Code] = N'IIC-3';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3') WHERE [Code] = N'IIC-3-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1') WHERE [Code] = N'IIC-3-1-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1') WHERE [Code] = N'IIC-3-1-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3') WHERE [Code] = N'IIC-3-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2') WHERE [Code] = N'IIC-3-2-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2') WHERE [Code] = N'IIC-3-2-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3') WHERE [Code] = N'IIC-3-3';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3') WHERE [Code] = N'IIC-3-4';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4') WHERE [Code] = N'IIC-3-4-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4') WHERE [Code] = N'IIC-3-4-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3') WHERE [Code] = N'IIC-3-5';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5') WHERE [Code] = N'IIC-3-5-1';
UPDATE [IicWorkDirections] SET [ParentId] = NULL WHERE [Code] = N'IIC-4';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4') WHERE [Code] = N'IIC-4-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4') WHERE [Code] = N'IIC-4-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4') WHERE [Code] = N'IIC-4-3';
UPDATE [IicWorkDirections] SET [ParentId] = NULL WHERE [Code] = N'IIC-5';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5') WHERE [Code] = N'IIC-5-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5') WHERE [Code] = N'IIC-5-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5') WHERE [Code] = N'IIC-5-3';
UPDATE [IicWorkDirections] SET [ParentId] = NULL WHERE [Code] = N'IIC-6';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-1';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-2';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-3';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-4';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-6';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-7';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-8';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-9';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-10';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-11';
UPDATE [IicWorkDirections] SET [ParentId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6') WHERE [Code] = N'IIC-6-12';

-- 4. Archive directions removed from source
UPDATE [IicWorkDirections] SET [IsArchived] = 1, [ModifiedAt] = SYSUTCDATETIME() WHERE [Code] LIKE N'IIC-%' AND [Code] NOT IN (N'IIC-1', N'IIC-1-1', N'IIC-1-1-1', N'IIC-1-1-2', N'IIC-1-2', N'IIC-1-2-1', N'IIC-1-2-2', N'IIC-1-2-3', N'IIC-1-3', N'IIC-2', N'IIC-2-1', N'IIC-2-2', N'IIC-2-2-1', N'IIC-2-2-2', N'IIC-3', N'IIC-3-1', N'IIC-3-1-1', N'IIC-3-1-2', N'IIC-3-2', N'IIC-3-2-1', N'IIC-3-2-2', N'IIC-3-3', N'IIC-3-4', N'IIC-3-4-1', N'IIC-3-4-2', N'IIC-3-5', N'IIC-3-5-1', N'IIC-4', N'IIC-4-1', N'IIC-4-2', N'IIC-4-3', N'IIC-5', N'IIC-5-1', N'IIC-5-2', N'IIC-5-3', N'IIC-6', N'IIC-6-1', N'IIC-6-2', N'IIC-6-3', N'IIC-6-4', N'IIC-6-6', N'IIC-6-7', N'IIC-6-8', N'IIC-6-9', N'IIC-6-10', N'IIC-6-11', N'IIC-6-12');

DECLARE @ChelHourIdNorm INT = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'час');

-- 5. Upsert norms (latest row per direction)
IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 4.3160, [Estimate] = 1.4623, [StandardDeviation] = 0.7522, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1'), 4.3160, 1.4623, 0.7522, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 1.3079, [Estimate] = 0.2684, [StandardDeviation] = 1.1619, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1'), 1.3079, 0.2684, 1.1619, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 3.9236, [Estimate] = 1.3670, [StandardDeviation] = 1.0921, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1'), 3.9236, 1.3670, 1.0921, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 0.7847, [Estimate] = -0.2424, [StandardDeviation] = 0.6761, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-2'), 0.7847, -0.2424, 0.6761, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 2.6157, [Estimate] = 0.9615, [StandardDeviation] = 0.7233, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'), 2.6157, 0.9615, 0.7233, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-1'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 6.4086, [Estimate] = 1.8576, [StandardDeviation] = 0.8715, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-2'), 6.4086, 1.8576, 0.8715, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-3'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 1.8310, [Estimate] = 0.6049, [StandardDeviation] = 0.6710, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-3');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-3'), 1.8310, 0.6049, 0.6710, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 4.1852, [Estimate] = 1.4316, [StandardDeviation] = 0.6245, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3'), 4.1852, 1.4316, 0.6245, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 12.0000, [Estimate] = 2.4849, [StandardDeviation] = 0.5230, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2'), 12.0000, 2.4849, 0.5230, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 13.0000, [Estimate] = 2.5649, [StandardDeviation] = 0.9785, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), 13.0000, 2.5649, 0.9785, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 10.0000, [Estimate] = 2.3026, [StandardDeviation] = 0.4366, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2'), 10.0000, 2.3026, 0.4366, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 10.0000, [Estimate] = 2.3026, [StandardDeviation] = 0.4930, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1'), 10.0000, 2.3026, 0.4930, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 12.5000, [Estimate] = 2.5257, [StandardDeviation] = 0.5189, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2'), 12.5000, 2.5257, 0.5189, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 29.0000, [Estimate] = 3.3673, [StandardDeviation] = 0.5672, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), 29.0000, 3.3673, 0.5672, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 21.5000, [Estimate] = 3.0681, [StandardDeviation] = 0.4728, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-1'), 21.5000, 3.0681, 0.4728, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 28.0000, [Estimate] = 3.3322, [StandardDeviation] = 0.7341, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-2'), 28.0000, 3.3322, 0.7341, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 0.6562, [Estimate] = -0.4213, [StandardDeviation] = 0.7735, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-1'), 0.6562, -0.4213, 0.7735, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 7.0000, [Estimate] = 1.9459, [StandardDeviation] = 1.0250, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-2'), 7.0000, 1.9459, 1.0250, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-3'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 1.6404, [Estimate] = 0.4950, [StandardDeviation] = 0.8021, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-3');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-3'), 1.6404, 0.4950, 0.8021, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 0.8202, [Estimate] = -0.1982, [StandardDeviation] = 1.8016, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'), 0.8202, -0.1982, 1.8016, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 0.6000, [Estimate] = -0.5108, [StandardDeviation] = 0.8503, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-2'), 0.6000, -0.5108, 0.8503, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 4.5000, [Estimate] = 1.5041, [StandardDeviation] = 0.3586, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5-1'), 4.5000, 1.5041, 0.3586, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 2.2966, [Estimate] = 0.8314, [StandardDeviation] = 0.4153, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'), 2.2966, 0.8314, 0.4153, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 1.9685, [Estimate] = 0.6773, [StandardDeviation] = 0.4801, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), 1.9685, 0.6773, 0.4801, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 2.2966, [Estimate] = 0.8314, [StandardDeviation] = 0.3831, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), 2.2966, 0.8314, 0.3831, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-3'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 2.1000, [Estimate] = 0.7419, [StandardDeviation] = 0.2139, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-3');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-3'), 2.1000, 0.7419, 0.2139, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 23.0000, [Estimate] = 3.1355, [StandardDeviation] = 0.6575, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-1'), 23.0000, 3.1355, 0.6575, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 11.0000, [Estimate] = 2.3979, [StandardDeviation] = 0.5800, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-2'), 11.0000, 2.3979, 0.5800, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-3'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 10.3000, [Estimate] = 2.3321, [StandardDeviation] = 0.7165, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-3');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-3'), 10.3000, 2.3321, 0.7165, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 127.0000, [Estimate] = 4.8442, [StandardDeviation] = 0.5579, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), 127.0000, 4.8442, 0.5579, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-1'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 87.0000, [Estimate] = 4.4659, [StandardDeviation] = 0.4687, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-1');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-1'), 87.0000, 4.4659, 0.4687, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-2'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 62.2000, [Estimate] = 4.1304, [StandardDeviation] = 0.4219, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-2');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-2'), 62.2000, 4.1304, 0.4219, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-3'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 67.4000, [Estimate] = 4.2106, [StandardDeviation] = 0.4499, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-3');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-3'), 67.4000, 4.2106, 0.4499, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-4'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-4');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-4'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-6'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 304.0000, [Estimate] = 5.7170, [StandardDeviation] = 0.8983, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-6');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-6'), 304.0000, 5.7170, 0.8983, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-7'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 78.0000, [Estimate] = 4.3567, [StandardDeviation] = 0.6672, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-7');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-7'), 78.0000, 4.3567, 0.6672, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-8'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-8');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-8'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-9'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-9');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-9'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-10'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = 174.0000, [Estimate] = 5.1591, [StandardDeviation] = 0.7729, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-10');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-10'), 174.0000, 5.1591, 0.7729, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-11'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-11');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-11'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-12'))
BEGIN
    UPDATE [IicWorkDirectionNorms] SET [Performance] = NULL, [Estimate] = NULL, [StandardDeviation] = NULL, [ModifiedAt] = SYSUTCDATETIME() WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-12');
END
ELSE
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt]) VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-12'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
GO