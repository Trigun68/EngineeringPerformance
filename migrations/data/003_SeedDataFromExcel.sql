-- ============================================================================
-- Seed Data from Excel (003): IIC из листа «Техас» (Копия ++++-1.xlsx)
-- ----------------------------------------------------------------------------
-- Источник: .src/Копия ++++-1.xlsx, лист «Техас» (заголовки в 3-й строке).
-- Иерархия групп строится по номеру «№» (например, 1.2.3 → родитель 1.2 → 1).
-- Единицы — знаменатель из «Ед. изм. (метр)».
-- Нормы: Performance, Estimate (Z-оценка), StandardDeviation (Cт. откл.).
-- Идемпотентно: безопасно запускать повторно.
-- ============================================================================

-- ----------------------------------------------------------------------------
-- 1. IIC Work Directions (иерархия по № из «Техаса»)
-- ----------------------------------------------------------------------------
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
    VALUES (N'IIC-1-2', N'Общие фундаменты (ярд³) (без свай)', N'Объем фундаментов — только кубические ярды бетона. Трудозатраты исключают сваи.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м³'), 4, 0, SYSUTCDATETIME());
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
    VALUES (N'IIC-4', N'Общие трубопроводы (только погонные футы)', N'Общий объем — только погонные футы. Трудозатраты включают малый и большой диаметр, инженерные опоры/подвески и фитинги. Примечание: «Горячие» трубопроводы — температура проектирования >250°F; «Холодные» — температура проектирования <–20°F. Количество трубопроводов — это трубопроводы, указанные в окончательных чертежах «Выпущено для строительства» (IFC). Это количество должно измеряться «центром к центру» через клапаны и фитинги, как и количество для строительной метрики.', NULL, (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 27, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-4-1', N'Малый диаметр (≤2½") — погонные футы', N'Трубы диаметром ≤2½ дюйма.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 28, 0, SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2')
BEGIN
    INSERT INTO [IicWorkDirections] ([Code], [Name], [Description], [ParentId], [UnitId], [SortOrder], [IsArchived], [CreatedAt])
    VALUES (N'IIC-4-2', N'Большой диаметр (≥3") — погонные футы', N'Трубы диаметром ≥3 дюйма.', (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'), (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'м'), 29, 0, SYSUTCDATETIME());
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

-- ----------------------------------------------------------------------------
-- 2. IIC Work Direction Norms (Performance / Estimate=Z / StandardDeviation)
-- ----------------------------------------------------------------------------
DECLARE @ChelHourIdNorm INT = (SELECT [Id] FROM [UnitsOfMeasurement] WHERE [Name] = N'час');

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1'), 4.3160, 1.4623, 4.3427, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1'), 1.3079, 0.2684, 4.6572, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-1'), 3.9236, 1.3670, 3.2697, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-1-2'), 0.7847, -0.2424, 1.1251, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2'), 2.6157, 0.9615, 2.7325, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-1'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-2'), 6.4086, 1.8576, 7.6998, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-3'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-2-3'), 1.8310, 0.6049, 1.2442, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-1-3'), 4.1852, 1.4316, 2.0198, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2'), 12.0000, 2.4849, 7.9451, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-1'), 13.0000, 2.5649, 18.9110, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2'), 10.0000, 2.3026, 5.0700, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-1'), 10.0000, 2.3026, 5.1039, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-2-2-2'), 12.5000, 2.5257, 7.5664, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1'), 29.0000, 3.3673, 13.2098, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-1'), 21.5000, 3.0681, 11.1555, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-1-2'), 28.0000, 3.3322, 28.8517, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-1'), 0.6562, -0.4213, 0.4932, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-2-2'), 7.0000, 1.9459, 14.4750, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-3'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-3'), 1.6404, 0.4950, 1.4159, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-1'), 0.8202, -0.1982, 0.7225, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-4-2'), 0.6000, -0.5108, 0.7616, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-3-5-1'), 4.5000, 1.5041, 1.5811, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4'), 2.2966, 0.8314, 1.1600, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-1'), 1.9685, 0.6773, 1.0694, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-2'), 2.2966, 0.8314, 0.9843, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-3'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-4-3'), 2.1000, 0.7419, 0.6519, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-1'), 23.0000, 3.1355, 21.1896, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-2'), 11.0000, 2.3979, 7.9451, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-3'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-5-3'), 10.3000, 2.3321, 10.0245, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6'), 127.0000, 4.8442, 81.1819, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-1'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-1'), 87.0000, 4.4659, 56.2994, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-2'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-2'), 62.2000, 4.1304, 34.5129, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-3'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-3'), 67.4000, 4.2106, 42.2191, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-4'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-4'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-6'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-6'), 304.0000, 5.7170, 290.4522, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-7'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-7'), 78.0000, 4.3567, 111.5532, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-8'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-8'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-9'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-9'), NULL, NULL, NULL, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-10'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-10'), 174.0000, 5.1591, 129.1898, @ChelHourIdNorm, SYSUTCDATETIME(), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-11'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-11'), NULL, NULL, NULL, @ChelHourIdNorm, CONVERT(datetime2, '2026-04-24T00:00:00', 126), SYSUTCDATETIME());
END;

IF NOT EXISTS (SELECT 1 FROM [IicWorkDirectionNorms] WHERE [WorkDirectionId] = (SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-12'))
BEGIN
    INSERT INTO [IicWorkDirectionNorms] ([WorkDirectionId], [Performance], [Estimate], [StandardDeviation], [UnitId], [EffectiveFrom], [CreatedAt])
    VALUES ((SELECT [Id] FROM [IicWorkDirections] WHERE [Code] = N'IIC-6-12'), NULL, NULL, NULL, @ChelHourIdNorm, CONVERT(datetime2, '2026-04-24T00:00:00', 126), SYSUTCDATETIME());
END;
