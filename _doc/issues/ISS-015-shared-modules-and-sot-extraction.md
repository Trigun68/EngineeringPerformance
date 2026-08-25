# ISS-015: Экстракция общих модулей и выравнивание SoT (форматтеры, chrome, confirm, валидаторы, IIC)

**Статус:** 🔵 Backlog (2026-08-16)
**Категория:** Refactoring / DRY / SoT
**Приоритет:** P2 (Medium)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §2 (A-05, A-07), §3 (M-04, M-07, M-08), §4 (C-03)

## Контекст
1. **IIC-имена:** канонический `IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit` лежит в Infrastructure → Application-код не может его использовать → 5 ручных копий склейки `код — наименование (ед.)`: `PerformanceCalculationEngine.FormatClassifierName`, `DetailFactSummaryTableBuilder`, `DetailForecastSummaryTableBuilder`, `NormHealthCheckEngine` (без ед.), `AnalyticsExportService` (без ед.).
2. **Валидаторы загрузки:** `ExcelUploadValidator` (Api, magic bytes) vs `ImportUploadValidator` (Application, без magic bytes) — дубль правил; оба 10 MB при задокументированных 50 MB.
3. **Форматтеры чисел:** `utils/formatGridNumber` ≡ `reportFormat.fmt`; локальные `.toFixed` в аналитике; `interpretationColor` дублирует backend-пороги.
4. **Chrome-близнецы:** `WorkspaceChromeToggleRow`/`workspaceChromeContext` ↔ `AnalyticsChromeToggleRow`/`analyticsChromeContext` (diff — подписи/testid).
5. **Три confirm:** `ConfirmDialog`, `useConfirmation`, `ConfirmArchiveDialog`.
6. **Прочее:** `cn()` живёт в `reportFormat.ts`; `useDictionaryStore` ходит в `apiClient` напрямую; `gridColumns.tsx` — 3 facility- и 3 detail-фабрики с пересечениями.

## Решение (направление)
1. Перенести IIC-форматтер в Application (или Domain) и удалить ручные копии; клиент продолжает использовать `formatIisNorm`.
2. Единый `UploadValidator` (Application, с magic bytes), используется и контроллерами, и сервисом импорта; лимит синхронизировать с `.cursorrules` (10 или 50 MB — решить и зафиксировать).
3. `utils/format.ts` — единый форматтер чисел; пороги интерпретации — из backend DTO или общего модуля.
4. Общий `ChromeToggleRow` + параметризованный контекст; один confirm-механизм; `cn()` → `utils/cn.ts`; dictionaries — через `api/dictionariesApi.ts`.
5. `gridColumns.tsx`: общие базовые колонки + композиция вариантов.

## Acceptance Criteria
- [ ] Нет ручной склейки имён IIC вне SoT-хелперов (grep чистый).
- [ ] Один валидатор загрузки; один форматтер чисел; один chrome; один confirm.
- [ ] `npm run build`, `npm test`, Playwright зелёные; `dotnet test` зелёный.

## Связанные
- `iic-group-naming.mdc`; ISS-010, ISS-013, ISS-014
