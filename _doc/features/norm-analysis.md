# Анализ норм — реестр и health-check

**Дата обновления:** 2026-08-28  
**Код:** `backend/.../Analytics/NormRegistry/*`, `client/src/features/analytics/norm-analysis/*`

**Справка для пользователя (CV, достоверность, дрейф, KPI):** [_doc/ui/norm-analysis-help.md](../ui/norm-analysis-help.md). Тот же текст открывается кнопкой «Справка» (иконка i) на вкладке.

## Назначение

Вкладка **«Анализ норм»** на `/analytics` — реестр периодов `IicWorkDirectionNorm` с health-check (10 правил), таймлайном, карточкой нормы, закрытием периода и экспортом. Без новой lifecycle-модели Draft/GUID: статус производный (`Active` / `Historical` / `Future`).

## Права

| Действие | Permission |
|----------|------------|
| Страница `/analytics` | `GroupTrend` (как раньше) |
| Чтение реестра / health / timeline | `IicWorkDirectionNorms` Read |
| Archive, экспорт | `IicWorkDirectionNorms` Edit |

## API (`/api/analytics/norm-registry`)

| Метод | Путь | Описание |
|-------|------|----------|
| POST | `/list` | Постраничный реестр + фильтры |
| GET | `/{id}` | Карточка (usage, audit, issues, prev/next) |
| POST | `/health-check` | Сводка и список issues |
| GET | `/timeline` | Полосы периодов по направлениям |
| GET | `/filters` | Метаданные фильтров |
| GET | `/{id}/usage` | Статистика snapshot-ссылок |
| POST | `/{id}/archive` | Закрыть период (`EffectiveTo = today`) |
| GET | `/export` | CSV/XLSX реестра |
| GET | `/health-report` | CSV/XLSX health |

## Health-правила

1. OVERLAP_CONFLICT  
2. COVERAGE_GAP  
3. NO_ACTIVE_NORM  
4. STALE_ACTIVE  
5. DUPLICATE_NORM  
6. ZERO_SIGMA  
7. INVALID_PARAMS  
8. UNUSED_NORM  
9. STALE_FUTURE (замена STALE_DRAFT)  
10. BROKEN_CHAIN  

Конфиг: `Analytics:NormHealth` в `appsettings.json`.

## Кэш

Redis (`IDistributedCache`): ключи с маркером инвалидации `norm-registry:invalidate-marker`, TTL ~5 мин. Инвалидация при create/update/delete нормы и archive.

## UI

- Фильтры (debounce 400 ms), бейджи Error/Warning/Info, таймлайн Ганта (одна строка на группу IIC: слева `код — наименование (ед.)`, справа периоды с зазором; тёмная обводка только у блока периода), MUI-таблица / heatmap, карточка с превью кривой (`NormBandChart`).
- Ось таймлайна = выбранный период «Отчёт за»; сегменты обрезаются по его границам.
- Маркер «Сегодня» рисуется только на колонке дорожек и только если дата внутри диапазона оси (не на краю). Подписи оси — начало / конец / «сегодня».
- `GET /timeline` отдаёт `groupName` уже в каноническом формате (`IicWorkDirectionDisplayUnit.FormatCodeNameWithResolvedUnit`).
- «Открыть в справочнике» → `/references/iic-norms?workDirectionId=&normId=`.
- «Закрыть период» → confirm → POST archive.

## Вне scope

- Вкладка «Сравнение факта и норм» / `FactNormAnalysisEngine`.
- Таблицы Norm GUID / NormChangeAudit / Draft status.
