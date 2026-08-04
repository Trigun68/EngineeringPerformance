# SQL profiling и оптимизация доступа к данным

**Дата:** 2026-07-23 (обновлено 2026-07-27)  
**Статус:** метрики собираются; hot-path **110** + wave 2 **111** внедрены

## 1. Что внедрено

| Компонент | Назначение |
|-----------|------------|
| `SqlProfilingMiddleware` | End-to-end время HTTP, flush метрик SQL |
| `SqlQueryCommandInterceptor` | Длительность SELECT/INSERT/UPDATE/DELETE внутри EF |
| Таблица `SqlQueryMetrics` | Накопление: endpoint, query type, tables, duration, rows, hash, parameters |
| API `/api/monitoring/sql-metrics/*` | Summary / slow / frequent / weak-tables (Admin Read) |
| `110_HotPathIndexes.sql` | Covering PPV + composite PE (см. §6) |
| `111_SqlMetricsWave2Indexes.sql` | Covering LI, covering UX норм, import iterations (см. §7) |

Конфиг (`SqlProfiling` в appsettings):

- `Enabled` (default true)
- `MinDurationMsToPersist` — порог записи
- `RetentionDays` (default 14)

## 2. Как читать отчёты

```http
GET /api/monitoring/sql-metrics/summary?hours=24&take=20
GET /api/monitoring/sql-metrics/slow?hours=24&take=20
GET /api/monitoring/sql-metrics/frequent?hours=24&take=20
GET /api/monitoring/sql-metrics/weak-tables?hours=24&take=20
```

- **Top slow** — кандидаты на индексы / упрощение JOIN / проекции.
- **Top frequent** — кандидаты на `IDistributedCache` / dictionary cache.
- **Weak tables** — таблицы с максимальным суммарным SQL-временем.
- **Size correlation** — бакеты по числу строк vs avg duration.

## 3. Рекомендуемые индексы (по схеме и типичным путям)

Обоснование: частые фильтры в API проектов/версий/аналитики и импорте. После накопления метрик приоритеты уточнять по `weak-tables` + `slow`.

| # | Индекс | Обоснование | Ожид. ускорение | Затраты | Риски | Приоритет |
|---|--------|-------------|-----------------|---------|-------|-----------|
| 1 | `IX_ProjectEquipments_VersionId_EquipmentId` | `/performance`, import replace | Высокое | Средние (write) | Низкие | **done** (110) |
| 2 | `IX_ProjectPhysicalVolumes_VersionId_ProjectId_Covering` | page-data / compare / planning (~11k rows) | Высокое (меньше key lookup) | Средние | Write на Replace | **done** (110) |
| 2b | Старые `(VersionId, ProjectId, Kfo/BerId)` | Фильтр по каталогу | — | — | Дублирование с covering | **keep** (оценка через неделю) |
| 3 | `ProjectLaborIntensities` covering VersionId | export/analysis (hash LI P95) | Среднее | Средние | Низкие | **done** (111) |
| 4 | `Equipments (ProjectId, IsArchived, Name)` | Проектный справочник + lookup импорта | Среднее | Низкие | Низкие | medium (частично есть ParentId/SortOrder) |
| 5 | `DataImportIterations (ProjectId, ProjectAnalysisVersionId, DataType, ImportedAt)` | Список итераций | Среднее | Низкие | Низкие | **done** (111) |
| 6 | `IicWorkDirectionNorms` covering UX `(WorkDirectionId, EffectiveFrom)` | Snapshot / нормы на дату | Среднее | Низкие | Низкие | **done** (111) |
| 7 | Filtered `Equipments (CatalogKeyHash) WHERE ProjectId IS NULL` | Уже есть после 106 | — | — | — | done |
| 8 | Partial на архивных справочниках `WHERE IsArchived = 0` | Ускорение UI directories | Низкое–среднее | Низкие | Низкие | low |
| — | `UserSettings (UserId, Key)` | preferences (10k+ exec, ~0.1 ms) | — | — | — | **already present** |

**Write overhead:** индексы на `Project*` (часто rewrite при импорте Replace) — измерять p95 INSERT после внедрения; при деградации импорта урезать INCLUDE на PPV / LI.

## 4. Архитектурные предложения

| Решение | Когда | Ожид. эффект | Затраты | Риски | Приоритет |
|---------|-------|--------------|---------|-------|-----------|
| Кеш `GET /dictionaries/all` (уже Redis) + invalidate | Частота высокая | Снижение SELECT по справочникам | Низкие | Stale при miss invalidate | **high** (поддерживать) |
| Кеш page-data / performance по `(projectId, versionId)` TTL 30–60s | Hot open проекта | Сильное снижение повторных агрегаций | Средние | Устаревание после импорта — invalidate | **high** |
| Projection DTO в raw loader (не полный PPV+KFO+BER) | #1 TotalMs после 110/111 | Главный выигрыш vs индексы | Средние | Пропуск полей | **high** |
| Материализованная/денормализованная сводка IIC hours на версию | Тяжёлый Forecast/Fact | Стабильный p95 экспорта | Высокие | Рассинхрон | medium |
| Batch `AddRange` + меньше `SaveChanges` в импорте | Уже частично | Снижение round-trips | Низкие | Транзакции | medium |
| Explicit `AsNoTracking` + projection DTO (без full graph) | Грид/аналитика | Меньше memory + SQL | Средние | Пропуск полей | **high** |
| Eager `Include` только нужных FK | Избегать N+1 | Меньше команд | Низкие | Over-fetch | medium |
| Партицирование `SqlQueryMetrics` / крупных факт-таблиц по дате | Рост >10M строк | Стабильный cleanup | Высокие | Сложность ops | low |
| Отключить SqlProfiling в Production или `MinDurationMsToPersist=50` | Нагрузка на запись метрик | Меньше write amplification | Нулевые | Меньше данных | **high** для prod |
| Кеш Users/Roles (auth) | Высокий TotalMs при низкой Avg | Не индексы | Низкие | Stale | medium |

## 5. Процесс приоритизации

1. Неделя сбора → `summary`.
2. Top 10 slow + weak tables → кандидаты индексов.
3. Top frequent read-only → cache keys.
4. После каждого изменения — сравнить p95 endpoint из RED (`/monitoring/system-metrics`) и SQL summary.

## 6. Baseline и внедрение hot-path (2026-07-27)

Источник: живая `dbo.SqlQueryMetrics` на `ep-sqlserver`, окно **48h** до применения `110`.

### Baseline SELECT (по QueryHash, TotalMs DESC)

| Ранг | Паттерн | Exec | Avg / Max / Total ms | Avg rows |
|------|---------|------|----------------------|----------|
| 1 | PPV + KFO + BER (полный SELECT) | 1012 | 54.2 / 999 / **54360** | ~11432 |
| 2 | ProjectEquipments + Equipments | 966 | 10.5 / 252 / **9638** | ~3184 |
| 3 | PPV + catalogs + Units | 13 | 45.5 / 119 / 585 | ~11431 |
| 4 | PPV без каталогов (import) | 3 | 170.8 / 470 / 511 | ~7621 |
| 5 | LI + IicWorkDirections | 13 | 27.4 / 337 / 352 | ~621 |

**Weak tables (TotalMs):** `ProjectPhysicalVolumes` (55472) → `PhysicalVolumesBer/Kfo` → `Equipments` / `ProjectEquipments`.

### Применено (110)

| Индекс | Ключ / INCLUDE | Статус |
|--------|----------------|--------|
| `IX_ProjectEquipments_VersionId_EquipmentId` | `(ProjectAnalysisVersionId, EquipmentId)` | создан на стенде |
| `IX_ProjectPhysicalVolumes_VersionId_ProjectId_Covering` | `(VersionId, ProjectId)` + INCLUDE LI/catalog/facility/… | создан на стенде |

Скрипт: [`migrations/schema/110_HotPathIndexes.sql`](../../migrations/schema/110_HotPathIndexes.sql) (в `docker-db-init` SCHEMA_FILES + drift path).  
EF parity: `backend/.../Persistence/Configurations/ProjectEquipmentConfiguration.cs`, `ProjectPhysicalVolumeConfiguration.cs` (+ legacy Data/Configurations).

### Сверка после внедрения (110)

| Паттерн | Baseline 48h Avg/Max | Post-apply (n≈4) Avg/Max | Avg rows |
|---------|----------------------|--------------------------|----------|
| PPV + KFO + BER | 54.2 / **999** | **15.8 / 19** | ~11431 |
| PE + Equipments | 10.5 / 252 | **9.2 / 20** | ~3191 |

## 7. Wave 2 по SqlQueryMetrics (2026-07-27, 111)

Источник: ~75k метрик, окно **168h** TotalMs + **48h** P95 (exec≥5).

### Наблюдения до 111

| Паттерн | Метрика | Вывод |
|---------|---------|--------|
| PPV+KFO+BER | TotalMs ~93s / P95 ~194 | #1; covering 110 есть — узкое место объём графа (~11k rows), не seek |
| PE+Equipments | TotalMs ~17s | 110 уже закрыл VersionId+EquipmentId |
| LI+IIC | P95 ~142 (мало exec) | Covering по VersionId |
| Users+Roles | TotalMs высокий, Avg низкий | Кеш auth, не индекс |

### Применено (111)

| Индекс | Ключ / INCLUDE | Статус |
|--------|----------------|--------|
| `IX_ProjectLaborIntensities_VersionId_Covering` | `(ProjectAnalysisVersionId)` + INCLUDE колонок LI из SELECT hash `f1309808` | создан на стенде |
| `UX_IicWorkDirectionNorms_WorkDirection_EffectiveFrom` | UNIQUE `(WorkDirectionId, EffectiveFrom DESC)` + INCLUDE EffectiveTo/Unit/Performance/формулы | создан (covering) |
| `IX_DataImportIterations_Project_Version_DataType_ImportedAt` | `(ProjectId, VersionId, DataType, ImportedAt DESC)` | создан |

Скрипт: [`migrations/schema/111_SqlMetricsWave2Indexes.sql`](../../migrations/schema/111_SqlMetricsWave2Indexes.sql).  
EF: `ProjectLaborIntensityConfiguration`, `IicWorkDirectionNormConfiguration`, `DataImportIterationConfiguration` (+ регистрация в `AppDbContext`).

Smoke после apply: `GET .../page-data` 200, `.../performance` 200, `GET /api/iic-norms` 200 (проект 1 / версия 2).

### Дальше

1. Projection DTO для PPV+каталогов — главный рычаг против #1 TotalMs.  
2. Кеш page-data / performance + invalidate после импорта.  
3. Через ~неделю: 3-key Kfo/Ber vs covering PPV; p95 INSERT при Replace (INCLUDE LI/PPV).  
4. Сравнить LI hash Avg/P95: `GET /api/monitoring/sql-metrics/slow?hours=24` после нагрузки.

## Связанные файлы

- `backend/.../Diagnostics/SqlQueryCommandInterceptor.cs`
- `backend/.../Middleware/SqlProfilingMiddleware.cs`
- `migrations/schema/107_SqlQueryMetrics.sql`
- `migrations/schema/110_HotPathIndexes.sql`
- `migrations/schema/111_SqlMetricsWave2Indexes.sql`
- `docs/changelog/CHANGELOG.md`
