# ISS-013: Декомпозиция god-классов backend

**Статус:** 🔵 Backlog (2026-08-16)
**Категория:** Refactoring / SRP
**Приоритет:** P1 (High)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §3

## Контекст
Классы со смешанными ответственностями (цель CLAUDE.md — функции ≤30 строк, один класс = одна причина изменения):
- `ProjectExcelDataImportService.*` — 5 partial-файлов, ~2879 строк (PPV 1344 / LI 569 / Equipment 526 / API / транзакции / метаданные).
- `ProjectsPageGridBuilder` — 1282 строки, 25 статических методов: forecast summary + forecast grids + fact grids + compare + performance totals + именование IIC. Методы `BuildFactGrids` ~170 строк, `BuildForecastDetail` ~105.
- `ProjectExportService` — 1272 строки, 13 публичных экспортов (Excel+PDF × forecast/fact/compare); `BuildFactDetailSummaryTableWorkbook` ~150 строк, `AddDetailSummarySheet` ~125.
- Также: `VisualReportService` 830, `FactNormAnalysisService` 684, `ProjectForecastService` 636, `NormRegistryService` 611, `DirectoryFacade` 546.

## Решение (направление)
1. Import: разделить на `PhysicalVolumeImportProcessor` / `LaborIntensityImportProcessor` / `EquipmentImportProcessor` (уже частично есть) + тонкий оркестратор + отдельный `ImportIterationMetadataWriter` (см. ISS-009).
2. GridBuilder: `ForecastGridBuilder` / `FactGridBuilder` / `CompareGridBuilder` + общий `HourRollup`-модуль; именование IIC — через SoT-хелпер (ISS-015).
3. Export: `ExcelExportWriter`/`PdfExportWriter` по доменам (forecast/fact/compare), общие хелперы стилей/чисел.
4. Методы >80 строк с несколькими ответственностями — Extract Method.

## Acceptance Criteria
- [ ] Ни один файл не превышает ~600 строк; методы ≤80 строк (кроме декларативных маппингов).
- [ ] Поведение не меняется: существующие тесты зелёные, e2e экспорта зелёные.
- [ ] Нет новых нарушений SSOT движка (ISS-010 учтён).

## Файлы
- `backend/EngineeringPerformance.Infrastructure/Import/ProjectExcelDataImportService.*.cs`
- `backend/EngineeringPerformance.Infrastructure/Analytics/ProjectsPageGridBuilder.cs`
- `backend/EngineeringPerformance.Infrastructure/Export/ProjectExportService.cs`, `VisualReportService.cs`

## Связанные
- ISS-009, ISS-010, ISS-015; `performance-calculation-engine-ssot.mdc`
