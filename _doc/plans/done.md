# Выполненные задачи — аудит и исправление

**Дата:** 2026-07-26

## Исправления авторизации

- **TrendsAnalyticsController**: на все endpoint-ы добавлен `[RequireProjectAccess]` (кроме `ListFormulas`). Ранее любой пользователь с глобальной Permission `GroupTrend:Read` мог читать трендовые данные любого проекта — полный обход проектной авторизации.
- **ProjectPermissionsController**: восстановлен `[RequireProjectAccess]` на всех методах. Без него любой пользователь с `Projects:Edit` глобально мог манипулировать правами для любого проекта.
- **AuthController.FindUserForLoginAsync**: заменён `ToListAsync()` на `FirstOrDefaultAsync()` — ранее при каждом логине/Negotiate/Me загружалась вся таблица пользователей (DoS-уязвимость).
- **AuthController.Login**: добавлен отдельный `MapLoginUser` без поля `IsActive`. Раньше `IsActive` возвращался в login-ответе, позволяя перечислять заблокированные учётные записи.
- **MonitoringController**: на все параметры добавлены `[Range]` (windowMinutes, take, hours) — ранее админ-эндпоинты принимали неограниченные значения, потенциально перегружая БД.
- **ProjectVersionsController**: добавлен `POST /restore` (unarchive) endpoint, метод `UnarchiveAsync` в сервисе и интерфейсе. Soft delete без восстановления был неполным.

## Исправления безопасности (backend)

- **ProjectExcelDataImportService.FormatImportExceptionMessage**: возвращает generic сообщение "Ошибка импорта данных. Обратитесь к администратору." вместо stacktrace. Full stack логируется на сервере.
- **ProjectExcelDataImportService.ParseDecimal**: выбрасывает `InvalidOperationException` с понятным сообщением вместо silent возврата 0.
- **DirectoriesController.SanitizeFileName**: добавлена null-guard и trim до 128 символов.
- **FactAssessmentPipeline**: generic error "Неизвестная формула оценки." вместо подстановки `request.FormulaId` в ответ.
- **ProjectsController.ExportCombinedPdf**: валидация что все 4 versionIds принадлежат projectId. Бросает `InvalidOperationException` при рассинхроне.
- **ProjectAnalyticsController.forecast/assessment-volumes**: PermissionType изменён с `Projects` на `Planning` — соответствует бизнес-логике.
- **Program.cs**: добавлена rate limiter policy "export" (5 req/min, fixed window), применена к 11 export-эндпоинтам (ProjectsController, ProjectAnalyticsController, TrendsAnalyticsController, DirectoriesController).

## Исправления часовых поясов (PDF-экспорт)

- **QuestPdfReportComposer**: UTC timestamp → MSK (ConvertFromUtc).
- **ProjectExportService**: аналогично.
- **ProjectsCatalogExportService**: аналогично.
- **VisualReportService**: аналогично (2 места).

## Исправления EF Core конфигураций

- **ProjectAnalysisVersionConfiguration** (создан): `HasMaxLength` на Name/VersionName, уникальный индекс на primary-версию (ProjectId + Direction + IsPrimary), индекс на Direction.
- **IicWorkDirectionNormConfiguration** (создан): уникальный индекс на пересечение периодов (VersionId + PeriodFrom + PeriodTo + ElementCode).

## Исправления SPA session management

- **AppLayout.tsx**: heartbeat интервал теперь динамический — читает `sessionExpirationMinutes` из SystemSettings и использует 80% от этого значения. Fallback: 15 минут при недоступности настроек.
- **apiClient.ts — retry-after-refresh**: если повторный запрос после refresh тоже возвращает 401 — сессия сбрасывается, `onUnauthorized` срабатывает (ранее — тихий отказ, пользователь оставался залогиненным).
- **apiClient.ts — BroadcastChannel fallback**: добавлена синхронизация через localStorage (`storage` event). Если BroadcastChannel недоступен, logout в одной вкладке синхронизируется во всех остальных.
- **useAuthStore.initialize**: упрощена цепочка инициализации. Убран дублирующий вызов `/auth/me` после negotiate. Теперь: /auth/me → /auth/negotiate (установка сессии) → показ формы логина. Раньше был лишний `/auth/me` после negotiate.

## Запуск агентов-аудиторов

- **security-auditor** (11 контроллеров): выявлено 3 критических, 4 высоких, 4 средних, 3 низких issues. Три критических (TrendsAnalytics, AuthController scan, ExportCombinedPdf) исправлены. Остальные — в бэклоге.
- **security-auditor** (import/export): 1 critical, 8 high, 4 medium, 4 low issues. Критическая утечка DB-исключений в `ProjectExcelDataImportService` — исправлена.

## Итоги аудита сессий клиента (frontend SPA)

**Было:**
- httpOnly cookies (ep_auth_token + ep_refresh_token), без JWT в localStorage — ✅ хорошо
- Promise deduplication на refresh (refreshRequestPromise) — ✅ хорошо
- BroadcastChannel для синхронизации logout между вкладками — ✅ хорошо
- Retry оригинального запроса после refresh — ✅ хорошо

**Исправлено:**
1. Retry-after-refresh failure: если повторный запрос тоже 401 — handler теперь срабатывает (redirect на login).
2. Heartbeat жёстко 15 минут, игнорирует sessionExpirationMinutes из SystemSettings — теперь динамический (80% от настроек).
3. Initialize() хрупкая цепочка: /auth/me → /auth/negotiate → /auth/me, лишний вызов — упрощена.
4. BroadcastChannel без fallback — при недоступности logout не синхронизируется — добавлен localStorage fallback.

## Исправления DataTable: сохранение видимости столбцов

- **DataTable.tsx — scheduleLayoutSave**: убран преждевременный `captureLayoutFromApi()` в начале функции. Раньше snapshot layout делался сразу при входе в `scheduleLayoutSave`, до того как `api.applyColumnState` успевала обновить состояние грида — при сохранении через диалог настроек колонок сохранялось старое состояние, и после перезагрузки все столбцы появлялись заново.

## Сборка

`dotnet` не установлен в окружении — сборка/тесты не выполнены. Рекомендуется проверить локально:
```bash
dotnet build EngineeringPerformance.slnx
dotnet test EngineeringPerformance.slnx
```
