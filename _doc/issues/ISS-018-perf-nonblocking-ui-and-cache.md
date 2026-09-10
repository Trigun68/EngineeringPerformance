# ISS-018: Долгая загрузка данных и блокирующий интерфейс

**Статус:** ✅ Выполнено (2026-08-17)
**Категория:** Performance / UX
**Приоритет:** P0 (High)
**Источник:** запрос пользователя «загрузка данных очень долгая, нужно не блокировать весь
интерфейс, а только части с подгружаемыми данными»

## Контекст
1. **Блокирующие оверлеи.** `WorkingOverlay` — полноэкранный `Backdrop` поверх модалок — стоял
   не только на мутациях/экспорте, но и на **чтении** данных: загрузка справочников в `AppLayout`,
   `page-data` в `FullProjectsPage`, плюс собственный оверлей в каждой панели (прогноз, факт,
   сравнение, отчёт). Оверлеи накладывались друг на друга: пока грузится один датасет,
   недоступна вся страница, включая шапку и вкладки.
2. **Стартовый бандл.** Ни `React.lazy`, ни `manualChunks`: Highcharts, AG Grid и MUI попадали в
   единый `index.js`, который скачивался целиком до первого экрана.
3. **Нет кэша тяжёлых чтений.** `page-data` (9 гридов) и `performance-report` (план + факт +
   история всех версий) пересчитывались на каждое переключение вкладки и возврат на страницу.
4. **Perf-smoke без порогов.** `scripts/perf-smoke.sh` печатал время, но не падал на регрессии.
5. **Нет сжатия.** В `client/nginx.conf` не было `gzip`: `page-data` уходил клиенту как 50.6 МБ
   несжатого JSON, стартовый скрипт — 2.21 МБ.

## Реализация
**Неблокирующий UI:**
- Новый `PanelBusyBar` — тонкая `LinearProgress` в потоке панели, с задержкой и зарезервированной
  высотой (нет скачка вёрстки).
- `WorkingOverlay` остался **только для мутаций и экспорта**; все загрузки данных
  (`AppLayout` — справочники, `FullProjectsPage` — page-data, панели прогноза/факта/сравнения/
  отчёта) переведены на `PanelBusyBar`. Шапка, вкладки и уже загруженные гриды остаются рабочими.
- `ComparePanel`: таблица и диаграмма грузятся через `Promise.all`, а не каскадом.

**Размер бандла:**
- `React.lazy` для аналитики, справки, админки, настроек и аудита; графики (`CompareChartView`,
  `NormBandChart`) — по требованию.
- `manualChunks`: `highcharts`, `aggrid`, `mui` вынесены в отдельные чанки.
- Итог: Highcharts (291 kB / 108 kB gzip) и график норм (104 kB) больше **не** предзагружаются на
  старте; стартовый payload меньше примерно на 170 kB gzip.

**Кэш тяжёлых endpoint** (`IProjectAnalyticsCache`):
- Ключ версионируется поколением проекта и глобальным поколением: инвалидация — инкремент
  счётчика, перебор ключей в Redis не нужен. TTL записи 10 минут.
- Кэшируется упакованный `page-data` (`page-data-encoded-v2`) и `performance-report`.
  Неупакованный DTO и полный ответ сравнения в Redis не пишутся.
- Инвалидация проекта: импорт Excel, CRUD ПОВ/трудоёмкости/оборудования, сохранение планового
  объёма прогноза, пересчёт версии, любые мутации версии (создание, правка, архив, разархив,
  primary, копирование). Глобальная инвалидация: CRUD норм ИИС.
- Недоступность Redis не ломает чтение: промах логируется, данные считаются заново.

**Сжатие (nginx):**
- `gzip on` уровня 5 от 1 КБ, обязательный `gzip_proxied any` — иначе ответы, пришедшие от backend
  через proxy, остаются несжатыми.
- `page-data` 50.6 МБ → **2.24 МБ**, `index-*.js` 2.21 МБ → **618 КБ**.

**Perf-smoke:**
- Бюджеты latency на каждый endpoint (переопределяются через env), измерение **cold + warm** для
  кэшируемых endpoint, ненулевой код возврата при превышении; `PERF_STRICT=0` — режим warn.
- `curl --compressed`: меряем тот же путь, что и браузер.

## Acceptance Criteria
- [x] Загрузка данных не блокирует остальной интерфейс.
- [x] Тяжёлые библиотеки не в стартовом чанке.
- [x] Повторный запрос `page-data` / `performance-report` обслуживается из кэша.
- [x] Мутация данных проекта делает кэш недостижимым (тесты `ProjectAnalyticsCacheTests`).
- [x] `perf-smoke.sh` падает при превышении бюджета.
- [x] Ответы API и статика отдаются сжатыми.

## Файлы
- `client/src/components/WorkingOverlay/PanelBusyBar.tsx`, `client/src/App.tsx`,
  `client/vite.config.ts`, `client/src/layout/AppLayout.tsx`,
  `client/src/pages/projects/FullProjectsPage.tsx`, панели прогноза/факта/сравнения/отчёта
- `backend/EngineeringPerformance.Application/Analytics/IProjectAnalyticsCache.cs`
- `backend/EngineeringPerformance.Infrastructure/Analytics/ProjectAnalyticsCache.cs`
- `backend/EngineeringPerformance.Infrastructure/Analytics/ProjectsPageDataFetcher.cs`,
  `PerformanceReportService.cs`, `AnalysisVersionRecalculationOrchestrator.cs`,
  `ForecastPlannedVolumePersistenceService.cs`
- `backend/EngineeringPerformance.Infrastructure/Projects/ProjectDataService.cs`,
  `ProjectVersionService.cs`
- `scripts/perf-smoke.sh`, `client/nginx.conf.template`

## Связанные
- ISS-011 (поколение кэша FactNorm — тот же приём инвалидации)
- ISS-019 (объём `page-data` — сжатие сняло транспорт, но не сериализацию)
