# Мониторинг ресурсов сервера

Дата обновления: 2026-09-01.  
Реализация: `GET /api/monitoring/resource-metrics`, вкладка **Администрирование → Ресурсы**.

Снимок потребления **бэкенда (ASP.NET)**, **Redis** и **Microsoft SQL Server** на одной шкале. Nginx и браузер не считаются: фронт работает у клиента.

## Где смотреть

Маршрут `/admin/resources`. Доступ как у остальных вкладок админки (UI — супер-админ, API — право `Administration` Read). Соседние вкладки не заменяются: «Состояние сервисов» — ping, «Мониторинг» — HTTP RED.

Автообновление UI — 10 с на окне «1 ч», 60 с на более длинных периодах. На сервере `BackgroundService` пишет точки каждые 10 с в кольцо на 60 минут (оперативные KPI) **и** в таблицу `ResourceMetricSamples` (до 90 суток). Графики читаются с даунсэмплом; после рестарта API час можно восстановить из SQL.

## Единицы

| Метрика | Смысл |
|---|---|
| CPU | Доли логического ядра: `ΔCPU-time / Δwall-time`. 1.00 = одно ядро занято целиком. Не «% контейнера». |
| RAM | RSS-подобный размер в МиБ: Working Set API, `used_memory_rss` Redis, `physical_memory_in_use` SQL. |
| Активные пользователи | Различные `userId` с HTTP-запросами за 5 минут (не JWT presence). |
| Сессии | Неотзванные непросроченные `RefreshTokens`. |
| HTTP-коннекты | ESTABLISHED TCP на порту Kestrel (8080 в Docker) плюс EventCounter `current-connections`, если он живой. За Nginx это keep-alive до API, не вкладки браузера. |
| SQL Interactive / Analytics | Сессии с `Application Name` `EP-Interactive` / `EP-Analytics`. |
| Redis clients | `connected_clients` из `INFO` (клиент кэша, не люди). |

Самоопрос вкладки (`/api/monitoring/resource-metrics`, `/api/health`, `/metrics`) не входит в счётчик пользователей.

## Источники

- Backend: `Process`, GC, ThreadPool, EventCounters Kestrel/Hosting.
- Redis: `INFO` через общий `IConnectionMultiplexer` (`allowAdmin=true` только ради INFO; тот же mux, что кэш и Data Protection).
- SQL: DMV одним пакетом с `Application Name=EP-ResourceMonitor`, timeout 2 с. Нужен `VIEW SERVER STATE` (у `sa` в Docker есть). Ошибка DMV не роняет весь endpoint.

Ошибка одного источника помечает карточку `available: false`.

## Как читать авто-вердикт

- Доли CPU/RAM по трём сервисам: SQL обычно доминирует в RAM из-за buffer pool (лимит `Monitoring:SqlMemoryLimitMib`, по умолчанию 2048).
- «Аналитика в очереди» — заняты оба слота `AnalyticsSqlGate`.
- «Утечка SQL-коннектов» — сессии растут, число пользователей почти не меняется.
- «Давление GC» — alloc-rate или прирост Gen2 выше медианы окна.

KPI **ядра / пользователь** и **МиБ / пользователь** — оценка стоимости текущего онлайна (при 0 пользователей — «—»).

## Конфигурация

Секция `Monitoring` в `appsettings.json`: интервал сэмпла, окно кольца в памяти, окно активности, лимит памяти SQL для линии на графике, `HistoryRetentionDays` (по умолчанию 90).

В шапке вкладки — **Отчёт за**: 1 ч (по умолчанию), 3 ч, 6 ч, 12 ч, 24 ч, 3 дн, 7 дн, 30 дн, 60 дн, 90 дн. Карточки и авто-вердикт всегда «сейчас»; графики — выбранное окно. Пока история короче окна, показывается дата первого сэмпла.

Контракт HTTP: [_doc/api/monitoring.md](../api/monitoring.md).

## Хранение и даунсэмпл

| Место | Окно | Кто пишет |
|-------|------|-----------|
| Кольцо в памяти | 60 мин | `ResourceMetricsSampler` каждые 10 с |
| `dbo.ResourceMetricSamples` | 90 суток | тот же сэмплер, `Application Name=EP-ResourceMonitor` |

Кластерный ключ — `TimestampUtc`. Ошибка INSERT не роняет кольцо (warning). Раз в час `DELETE` старше `HistoryRetentionDays` (clamp 1…90). Объём порядка ~800 тыс. строк за 90 дней.

Чтение графиков: если запрошен 1 ч и память покрывает окно — из кольца; иначе SQL `GROUP BY` по корзине. Код: `ResourceSeriesBucket`, `ResourceSampleDownsampler`, `ResourceMetricHistoryStore`.

## Что не входит

cAdvisor / docker.sock / Grafana, топ wait stats и топ endpoint — следующий заход.
