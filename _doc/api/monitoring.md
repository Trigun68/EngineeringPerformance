# Мониторинг и ресурсы сервера

Дата обновления: 2026-09-01.  
Право: `Administration` Read. UI: **Администрирование**.

Продуктовое описание вкладки «Ресурсы»: [_doc/features/server-resource-monitoring.md](../features/server-resource-monitoring.md). Архитектура: [_doc/architecture/three-tier-docker.md](../architecture/three-tier-docker.md).

Базовый URL: `/api/monitoring`. JSON — camelCase.

## Эндпоинты

| Метод | Путь | Назначение |
|-------|------|------------|
| GET | `/system-metrics` | RED, тренд latency, зависимости |
| GET | `/health` | Сводка сервисов (админская; публичный ping — `GET /api/health`) |
| GET | `/resource-metrics` | CPU/RAM/коннекты бэкенда, Redis, SQL |
| GET | `/logs` | Последние логи процесса |
| GET | `/audit-config` | Журнал смены настроек |
| GET | `/permission-drift` | Расхождение матрицы прав с каталогом объектов |

Без JWT — **401**. Без права Administration — **403**.

## `GET /resource-metrics`

Снимок «сейчас» (KPI, вердикт) плюс ряд точек для графиков за окно.

| Query | Тип | По умолчанию | Границы |
|-------|-----|--------------|---------|
| `windowMinutes` | int | `60` | 1 … 129600 (90 суток) |

Вне диапазона — **400** (валидация `[Range]`).

Ответ (`ResourceMetricsDto`):

| Поле | Смысл |
|------|--------|
| `snapshot` | Последний сэмпл: пользователи, коннекты, CPU/RAM трёх процессов, runtime |
| `series` | Даунсэмпл рядов за окно (`backendCpuCores`, RAM, коннекты, saturation) |
| `verdicts` | Авто-вердикт по оперативному часу в памяти, не по длинному окну |
| `collectedAtUtc` | Время последнего сэмпла |
| `windowMinutes` | Фактически использованное окно (после clamp) |
| `bucketSeconds` | Шаг корзины на сервере |
| `historyAvailableFromUtc` | Первый сэмпл в SQL (или в ответе, если таблицы ещё нет) |

Шаг корзины (`ResourceSeriesBucket`):

| Окно | `bucketSeconds` |
|------|-----------------|
| ≤ 1 ч | 10 |
| ≤ 6 ч | 30 |
| ≤ 12 ч | 60 |
| ≤ 24 ч | 120 |
| ≤ 3 дн | 300 |
| ≤ 7 дн | 600 |
| ≤ 30 дн | 3600 |
| ≤ 60 дн | 7200 |
| ≤ 90 дн | 10800 |

Агрегация в корзине: AVG для CPU/RAM/ratio/alloc; MAX для пользователей, коннектов, очереди, накопительного Gen2.

Час после рестарта API читается из SQL, если кольцо в памяти ещё не покрывает окно. Запись сэмплов — `dbo.ResourceMetricSamples`, миграция [`120_ResourceMetricSamples.sql`](../../migrations/schema/120_ResourceMetricSamples.sql), lane `EP-ResourceMonitor` (не `AnalyticsSqlGate`).

Пример:

```http
GET /api/monitoring/resource-metrics?windowMinutes=1440
Authorization: Bearer …
```

Код: `MonitoringController`, `IResourceMetricsService`, `ResourceMetricsSampler`.
