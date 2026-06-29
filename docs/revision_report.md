# Отчёт ревизии кодовой базы — Engineering Performance (Web)

**Дата:** 2026-06-09  
**Роль:** Principal Architect / Lead Fullstack  
**Стек:** ASP.NET Core 8 + React/TS + MS SQL + Docker/Nginx  
**Эталон:** WPF `EngineeringPerformance.App` + `docs/migration_map.md`

---

## Сводка

| Область | Оценка | Комментарий |
|---------|--------|-------------|
| Справочники API/UI | ✅ Реализовано | 8 вкладок + нормы ИИС, деревья, архив, Excel export |
| Расчёт производительности (ИИС) | ⚠️ Частично | Ядро перенесено; групповой анализ — заглушки |
| Excel-импорт | ⚠️ Частично | Работает; нет посимвольного сравнения; мало тестов |
| Версионирование | ⚠️ Частично | CRUD есть; копия версии без данных |
| Права доступа | ⚠️ Частично | API в основном защищён; UI и `/dictionaries/all` — пробелы |
| Тесты | ⚠️ Частично | ~15 API integration; алгоритмы тестируются в legacy-проекте |
| Docker/Infra | ⚠️ Частично | Multi-stage OK; SPNEGO закомментирован; туннель — техдолг DNS |

---

## Детальная таблица GAP Analysis

| Модуль / Функция | Статус | Найденные проблемы / Риски |
|------------------|--------|----------------------------|
| **Справочники — API** (`DirectoriesController`, `DirectoryFacade`) | Реализовано | 7 ресурсов + деревья; permissions через `EnsureAccessAsync` (не `[HasPermission]`) |
| **Справочники — UI** (`DirectoryPage`, `IicNormsPage`, `referenceConfig`) | Реализовано | Нет drag-and-drop sort; нет контекстного меню на справочниках |
| **Нормы ИИС — API** (`RegistersController`, `IicWorkDirectionNormService`) | Реализовано | Регистр с датой действия; Excel export |
| **Нормы ИИС — UI** (`IicNormsPage`) | Реализовано | Фильтр «на дату»; CRUD через модалку |
| **Словарь (Dictionary)** (`DictionariesController`, `useDictionaryStore`) | Частично | `GET /dictionaries/all` **без проверки прав** — утечка справочных данных |
| **Физические объёмы (проект)** (`ProjectDataController`, `ProjectDetailPage`) | Реализовано | Пагинация AG Grid; нет полного CRUD контекстного меню как в WPF Fact/Plan |
| **Оборудование (проект)** | Реализовано | Аналогично PPV |
| **Трудоёмкости (проект)** | Реализовано | Отображение; редактирование ограничено vs WPF |
| **Расчёт ИИС / Z-score** (`PerformanceService`, `ProjectVersionAnalysisService`) | Реализовано | Дублирование с legacy `EngineeringPerformance.Infrastructure.Services.PerformanceService` |
| **Прогноз / планирование** (`ProjectForecastService`, `PlanningTab`) | Реализовано | PDF/Excel export через QuestPDF/ClosedXML |
| **Сравнение версий** (`ProjectCompareService`, `CompareTab`) | Реализовано | Фильтр «только различия» |
| **Групповой анализ / тренд** (`GroupAnalyticsService`, `GroupPlaceholderTab`) | Отсутствует | API возвращает placeholder DTO |
| **Excel-импорт PPV/труд/оборудование** (`ProjectExcelDataImportService`, `ImportWizard`) | Частично | Нормализация строк, не посимвольное сравнение; монолит ~2400 строк; **нет API-тестов** |
| **Excel-импорт списка проектов** (`ProjectsController.ImportExcel`) | Отсутствует | Stub — считает строки, не пишет в БД |
| **Валидация загрузки Excel** (`ExcelUploadValidator`) | Реализовано | MIME, magic bytes, 50 MB |
| **Иерархия справочников** (create parent/child, tree API) | Реализовано | `parentId` в CRUD; фильтры на PV/equipment |
| **Пустые шаблоны / копирование строк** (WPF create from copy) | Частично | Копия версии проекта **без** PPV/PLI/equipment |
| **Версии анализа — CRUD** (`ProjectVersionsController`) | Реализовано | Create, copy, archive (soft delete) |
| **Версии — UI** (`ProjectDetailPage`) | Частично | Нет edit/archive в UI; create упрощён vs WPF |
| **Снимок норм** (`AnalysisVersionSnapshotService`) | Реализовано | При создании версии |
| **Матрица прав — API** (`HasPermission`, `PermissionAuthorizationHandler`) | Частично | `DirectoriesController` — другой путь; `DictionariesController` — открыт |
| **Матрица прав — UI** (`usePermission`, `AppLayout`) | Частично | Скрыты Dashboard/Audit/Settings; **Проекты/Справочники/Справка всегда видны** |
| **Права на вкладки проекта** (`ProjectDetailPage`) | Частично | Все 11 вкладок видны; Import без `ProjectDataUpload` |
| **Пользователи / роли** (`UsersController`, `UsersPage`) | Реализовано | CRUD admin |
| **Матрица прав UI** (`AccessRightsPage`) | Реализовано | Administrator only |
| **Аудит** (`UserActionLogger`, `UserActionLogPage`) | Реализовано | Login, projects; расширять покрытие |
| **Auth JWT + cookie** (`AuthController`) | Реализовано | HttpOnly cookie; dev Windows simulate |
| **Auth SPNEGO** (Nginx + API) | Частично | `auth_gss` закомментирован в `nginx.conf`; JWT fallback работает |
| **QuestPDF / ClosedXML** | Реализовано | `using` declarations в export/import (проверено) |
| **FluentValidation** | Отсутствует | Валидация inline в контроллерах |
| **Backend тесты** (`EngineeringPerformance.Api.Tests`) | Частично | 8 файлов, skip при недоступной БД |
| **Legacy тесты** (`EngineeringPerformance.Tests`) | Реализовано | **Не в CI backend** — риск расхождения |
| **Playwright E2E** (`migration-smoke.spec.ts`) | Частично | 10 smoke; нет import/permissions |
| **Vitest** | Отсутствует | Нет unit-тестов фронта |
| **Backend Dockerfile** | Реализовано | Multi-stage, non-root `appuser` |
| **Frontend Dockerfile** | Реализовано | Node build → nginx:alpine |
| **nginx.conf / template** | Частично | Security headers ✅; `auth_gss` ❌ (комментарий); `X-Remote-User` ✅ |
| **docker-compose.yml** | Реализовано | Секреты из `.env`; `*.env` в `.gitignore` |
| **Cloudflare / tunnel** | Частично | Коллизия DNS `web` (исправлено rename HR Portal); Dashboard → `ep-web` |

---

## Блок 5: Аудит Docker и безопасности

### ✅ Соответствует

| Проверка | Файл | Статус |
|----------|------|--------|
| Multi-stage backend build | `backend/Dockerfile` | SDK → aspnet:8.0 runtime |
| Non-root API user | `backend/Dockerfile` | `USER appuser` |
| Node build + nginx static | `frontend/Dockerfile` | ✅ |
| API proxy, 50m body | `nginx.conf.template` | ✅ |
| Security headers | `nginx.conf`, `nginx.conf.template` | DENY, nosniff, CSP |
| Secrets not in git | `.gitignore` | `*.env` |
| JWT/DB from env | `docker-compose.yml`, `.env.example` | ✅ |

### ⚠️ Требует доработки (не блокирует dev)

| Проверка | Проблема | Рекомендация |
|----------|----------|--------------|
| `auth_gss on` | Закомментирован | Включить в prod-профиле nginx + keytab volume |
| `X-Trusted-Proxy-Token` | Пустой по умолчанию | Задать `PROXY_TRUSTED_SECRET` при SPNEGO |
| API `network: host` при build | `docker-compose.yml` | Убрать для переносимости CI |
| CORS production | `AllowAnyOrigin` в dev | `Cors__AllowedOrigins` в prod |
| `DictionariesController` | Без `[HasPermission]` | Добавить `References` Read |

---

## Критические риски (топ-7)

1. **Два кодовых базиса** — backend vs WPF Infrastructure; тесты алгоритмов на legacy.
2. **Копия версии без данных** — расхождение с WPF, потеря доверия пользователей.
3. **`/dictionaries/all` без ACL** — избыточная выдача данных Observer.
4. **Импорт Excel без backend fixture-тестов** — регрессии при рефакторинге монолита.
5. **Групповой анализ — заглушка** — UI обещает функцию, API пустой.
6. **UI permissions** — навигация и вкладки не соответствуют WPF.
7. **FluentValidation отсутствует** — слабая валидация входных DTO на границе API.

---

## Связанные документы

- `docs/migration_map.md` — карта WPF → Web API
- `docs/development_roadmap.md` — план доработок
- `_doc/security/web-security-audit.md` — предыдущий security audit
- `_install/DEPLOY.md` — деплой и туннель
