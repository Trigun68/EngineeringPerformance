# Стоимость работ

Дата обновления: 2026-08-29 (титулы PDF).  
SoT: [`IicWorkDirectionWorkCostService`](../../backend/EngineeringPerformance.Infrastructure/Registers/IicWorkDirectionWorkCostService.cs), страница [`WorkCostsPage`](../../client/src/pages/references/WorkCostsPage.tsx).

Справочник-регистр ставки инженерных работ **₽/ч** по дереву групп ИИС. Версионность — только датами периода (`EffectiveFrom` / `EffectiveTo`). Слева — список дат, в которые заданы расценки; справа — дерево групп ИИС выбранного периода (или срез as-of, если период не выбран).

## Право

`EngineeringWorkCosts` («Стоимость работ»). В матрице — сразу после норм ИИС. SuperAdmin — bypass; Admin — Edit; Engineer/Gip — Read.

## Данные

Таблица `IicWorkDirectionWorkCosts` (миграция `115_WorkDirectionWorkCosts.sql`): одна ставка на направление и дату начала, уникальность `(WorkDirectionId, EffectiveFrom)`. Новый период с открытым «по» закрывает предыдущий открытый (`EffectiveTo = новая дата − 1 день`), как нормы ИИС.

Уровень ввода (организация / 1 уровень / 2 уровень / каждый элемент) **в БД не хранится**. Сервер раскладывает ставки на узлы ИИС в [`WorkCostCascade`](../../backend/EngineeringPerformance.Application/Registers/WorkCostCascade.cs) — те же глубины, что параметры планирования Z.

## API

| Метод | Назначение |
|-------|------------|
| `GET /api/registers/work-costs?asOf=` | Все **неархивные** группы ИИС; ставка, действующая на дату |
| `GET /api/registers/work-costs?effectiveFrom=` | Ставки **только** с этой датой «Действует с» (без наследования предыдущего периода) |
| `GET /api/registers/work-costs/periods` | Список дат установки: с / по, число групп, единая ставка если все равны |
| `POST /api/registers/work-costs/batch` | `effectiveFrom`, `level` 1–4, `items[] { workDirectionId, ratePerHour }`. `workDirectionId = 0` — ставка по организации. Та же дата — обновление блока. Дата внутри уже закрытого интервала — новый блок до дня перед следующим периодом |
| `DELETE /api/registers/work-costs/periods?effectiveFrom=` | Удаляет весь блок на дату и продлевает предыдущий период |
| `POST /api/registers/work-costs/export` | Excel видимого грида |
| `GET /api/registers/work-costs/export.xlsx` | Excel as-of среза |

Имя файла: `справочник_стоимость_работ.xlsx`.

Имена групп — `код — наименование (ед.)` (`IicWorkDirectionDisplayUnit` / `formatIisNorm`).

## UI

Вкладка **«Стоимость работ»** сразу после «Нормы физобъёмов (IIC)». Грид групп только для чтения.

- **Периоды** — таблица дат «Действует с / по» и числа групп. Клик открывает блок этой даты в дереве; двойной клик — правка.
- **Задать стоимость** — новый период (дата «Действует с», уровень каскада).
- **Изменить** — тот же диалог по всему блоку групп ИИС выбранной даты (дату менять нельзя).
- **Удалить** — все ставки этой даты; предыдущий период продлевается.

Поле **«Дата»** справа в панели — срез as-of (что действовало в календарный день), без выбора строки периода.

Выгрузка — `ExportActionButton` «Выгрузить Excel».

## PDF-титулы

На обложке прогноза («Итого» и «По объектам») под каждым сценарием (оптимистичный → базовый → пессимистичный) — стоимость **₽**: ставка as-of `CreatedAt` версии × собственные часы направления. Расчёт — `PerformanceCalculationEngine.ComputeScenarioWorkCosts`. На контентных страницах стоимость не выводится. Без ставки для направления — «—».
