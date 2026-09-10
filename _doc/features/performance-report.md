# Отчёт производительности проекта (вкладка «Отчет»)

**Дата обновления:** 2026-09-10  
**Код:** `PerformanceReportService`, `ProjectReportSummaryService`, `PlanningScenarioReportMetrics`, `client/src/features/projects/report/`, `client/src/pages/projects/reports/`

## Назначение

Визуальный отчёт по инженерной производительности проекта: план/факт по основным версиям, шкала Z, динамика, направления IIC, PDF.

Часы и Z на гридах прогноза/факта/сравнения: [hours-and-z-ssot.md](../architecture/hours-and-z-ssot.md).  
Визуальные PDF: [pdf-visual-reports.md](./pdf-visual-reports.md).

## Резолв версий

1. **План-метрики:** версия факта с `IsPrimary`; если нет — версия плана с `IsPrimary`; иначе latest Fact → latest Plan.
2. **Факт-метрики:** только Fact-direction (`IsPrimary` → latest). Часы — **Σ PlannedHours только по строкам с `WorkDirectionId`**. `ActualHours` в KPI и Z не входит.
3. Объём в дереве: факт, иначе план.
4. **Трудоёмкость плана** = GrandTotals прогноза (BaseHours).
5. **Z плана на вкладке «Отчёт»** — заложенный параметр планирования (`ResolvePlanningZ` → `ZBase`), тот же rollup, что в диалоге «Параметры планирования» (`PlanningScenarioReportMetrics`). Это **не** live Z грида прогноза (`BuildPlanPerformance` → `CalculatePerformance`); грид не менялся.
6. **Z факта** — `ComputeWeightedZFromPerformanceRows` по allocated PlannedHours.

UI не передаёт `planVersionId` / `factVersionId`. Кэш отчёта: `report:v7`.

## API

| Метод | Путь | Право |
|-------|------|--------|
| GET | `/api/projects/{projectId}/report-summary` | `Reports` Read |
| GET | `/api/projects/{projectId}/performance-report` | `Reports` Read |
| GET | `/api/projects/{projectId}/export/report/pdf` | `Reports` Read |
| GET | `/api/projects/{projectId}/performance-report/export.pdf` | `Reports` Read |

`report-summary` — сводный вер 1 (KPI план/факт).  
`performance-report` — сводный вер 2 (дерево направлений, `PlanZScore` = zBase).  
PDF: визуальный движок, правила в [pdf-visual-reports.md](./pdf-visual-reports.md).

## UI

Подвкладки: **Сводный вер 1** (по умолчанию), **Тренд**, **Сводный вер 2**. Стили выбранного таба — `projectsPageSubTabsSx` (тёмный текст на `#D6EAF8`).

- Имена групп IIC только SoT `код — наименование (ед.)`; отдельную колонку «Код» и подпись «ИИЦ» не показывать.
- Числа ru-RU, вправо: объём/часы — 1 знак, производительность и Z — 2 знака.
- «Сводный вер 1»: на всю ширину панели; «Дополнительно» раскрыто; Top-5 лучших и худших — по группам ИИС с собственными факт-часами (не только корни). Дерево направлений: **Физобъём** (own) + **Сгруппированный физобъём**, одни факт-часы, интерпретация «—». Таблица прогноза — дерево IIC, own + сгруппированный, без подписи ±0.5σ. «Все группы физобъёмов IIC» — колонка сгруппированного объёма (`AssessmentPhysicalVolume`).
- Диаграмма Z только по корневым направлениям (вторая «по дисциплинам» убрана как дубль после rollup).
- Дерево направлений иерархическое. Без claimed-set факт-часы на L1; при непустом регистре — на заявленном узле, объём соседа не входит в родителя.
- «Сводный вер 2»: **Физобъём** = own (`PhysicalVolume`, не subtree на корнях) + **Сгруппированный физобъём**; план-часы на листьях не поднимаются (другой SoT). Точность и тренд не менялись.
- Визуальная шкала Z (сводный вер 1, вер 2 и PDF): слева хуже (`+3` / Критично), справа лучше (`−3` / Отлично). Пороги и формула Z не меняются.
- PDF на «Детально» (прогноз, факт, сравнение) нет — только Excel.
- Вне скоупа: Excel/PDF **Факт → Итого** (`FactTableDocumentBuilder`) всё ещё печатает own-часы на листьях — см. [hours-and-z-ssot.md](../architecture/hours-and-z-ssot.md) §5.
