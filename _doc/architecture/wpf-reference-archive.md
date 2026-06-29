# WPF reference archive (политика legacy)

**Обновлено:** 2026-06-08  
**Статус:** `EngineeringPerformance.App` — reference implementation до cutover

---

## Назначение

Папка `EngineeringPerformance.App/` и связанные проекты (`EngineeringPerformance.Core`, `EngineeringPerformance.Infrastructure` в корне решения) — **эталонная desktop-реализация**. После Phase 4 они не используются в production, но **не удаляются** из репозитория.

---

## Правила

| Правило | Описание |
|---------|----------|
| Freeze features | После Phase 2 — новый функционал только в `backend/` + `client/` |
| Bugfix only | Критические баги WPF — по согласованию, дублировать fix в web |
| Reference | При портировании сверять бизнес-логику с WPF ViewModels/Services |
| Build | Solution `EngineeringPerformance.slnx` должен собираться для сравнения |

---

## Cutover (MIG4-006)

1. Параллельная эксплуатация 2 недели (web + WPF на одной БД).
2. UAT по `_doc/plans/uat-phase4-checklist.md`.
3. Объявление отключения WPF-distributive.
4. Архивация установочных пакетов WPF (хранить 1 год).
5. README в `EngineeringPerformance.App/` — пометка «Archived / Reference only».

---

## Связь с web-стеком

| WPF | Web |
|-----|-----|
| `EngineeringPerformance.Infrastructure/Services/*` | `backend/EngineeringPerformance.Infrastructure/*` |
| `*ViewModel` | `client/src/pages/*` + API |
| PdfSharp exports | QuestPDF в `AnalyticsExportService` |

См. `docs/migration_map.md` для полной карты миграции.
