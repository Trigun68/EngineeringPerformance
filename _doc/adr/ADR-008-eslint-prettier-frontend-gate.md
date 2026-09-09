# ADR-008: ESLint 9 + Prettier + бюджет бандла как gate для frontend

**Дата:** 2026-09-08  
**Статус:** Accepted  
**Авторы:** команда Engineering Performance

---

## Контекст

До сентября 2026 в `client/` скрипт `npm run lint` был псевдонимом `tsc --noEmit`: проверялись только типы. Не ловились неиспользуемые переменные, нарушения правил хуков (`react-hooks/exhaustive-deps`), a11y-проблемы в JSX, случайные не-компонентные экспорты из файлов с компонентами (ломают Fast Refresh). Форматирование не было закреплено. Размер бандла (≈4 МБ одним чанком) никак не ограничивался — регрессии замечались только вручную.

Ограничения: GitHub Actions в репозитории не оплачены — все проверки гоняются локально (`scripts/quality-gates.sh`, правила `self-verify-checks.mdc`). Type-aware правила ESLint удвоили бы время проверки на дереве ~35 маршрутов.

## Варианты решения

### Вариант A: ESLint 9 flat-config (recommended, без type-checked) + Prettier + `check-bundle-budget.mjs`

`client/eslint.config.js`: `@eslint/js` recommended, `typescript-eslint` recommended, `react` (+ `jsx-runtime`), `react-hooks`, `jsx-a11y`, `react-refresh`; игнор `e2e/`, `dist/`. `_`-префикс для намеренно неиспользуемых аргументов; `no-autofocus` и `display-name` — warning. `.prettierrc.json`: `singleQuote`, `semi`, `trailingComma: all`, `printWidth: 100`. `npm run build` = `tsc -b && vite build && node scripts/check-bundle-budget.mjs` (любой чанк > 900 КБ gzip или сумма > 2.5 МБ gzip → exit 1).

**Плюсы:** быстро (секунды), ловит реальные классы ошибок, бюджет фиксирует регрессии размера без новых зависимостей (`size-limit`/визуализатор не добавляются).  
**Минусы:** без type-aware правил (`no-floating-promises`, `no-misused-promises`) — они остаются на `tsc` strict и ревью.

### Вариант B: type-checked конфиг (`recommendedTypeChecked`) сразу

**Плюсы:** ловит незавершённые промисы.  
**Минусы:** `parserOptions.project` на всём дереве — десятки секунд на прогон, много ложных срабатываний на AG Grid колбэках; отложено до следующего шага.

### Вариант C: оставить `tsc` как единственный линт

**Плюсы:** ничего не делать.  
**Минусы:** не решает исходную проблему.

## Решение

**Выбран вариант A.**

- `npm run typecheck` — `tsc --noEmit`; `npm run lint` — `eslint src`; `lint:fix`, `format`, `format:check` — вспомогательные, **не** запускаются массово по дереву в рамках рефакторингов (чтобы не смешивать форматирование с логикой в одном коммите).
- `scripts/quality-gates.sh` вызывает `typecheck` и `lint` для клиента.
- Бюджет бандла — часть `npm run build`; лимиты меняются только осознанно правкой `check-bundle-budget.mjs` с записью в CHANGELOG.
- Playwright после UI-правок — по-прежнему обязателен (`playwright-e2e-verify.mdc`), линт его не заменяет.

## Последствия

### Положительные
- Ошибки хуков и a11y видны до сборки образа.
- Размер бандла не растёт незаметно.

### Отрицательные / Риски
- Переход на type-checked правила — отдельный шаг (ISS-016), потребует чистки предупреждений.
- Prettier без массового `format` даёт смешанное форматирование в старых файлах до их следующей правки.

### Нейтральные
- ESLint не гоняется на `e2e/` (Playwright-спеки имеют свой стиль и собственный tsconfig).

## Связанные документы

- `_doc/issues/ISS-016-tooling-and-test-coverage.md`
- `.cursor/rules/self-verify-checks.mdc`, `.cursor/rules/playwright-e2e-verify.mdc`
- `client/eslint.config.js`, `client/.prettierrc.json`, `client/scripts/check-bundle-budget.mjs`
