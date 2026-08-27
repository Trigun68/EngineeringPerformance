# WPF — архив (вне scope разработки)

**Обновлено:** 2026-07-14  
**Статус:** **заморожен** — не развиваем, не цель паритета

---

## Политика (с 2026-07-14)

| Правило | Описание |
|---------|----------|
| **Вне scope** | Новый функционал **только** в `backend/` + `client/` |
| **Не эталон** | WPF **не используется** для сверки бизнес-логики и приёмки |
| **Не блокер** | Сборка `EngineeringPerformance.App` не входит в критерии релиза web |
| **Архив в репо** | Код остаётся до явного удаления; документация — историческая |

Актуальный план: [_doc/plans/web-only-development-plan.md](../plans/web-only-development-plan.md).

---

## Что было (историческая справка)

Папка `EngineeringPerformance.App/` и корневые `EngineeringPerformance.Core` / `EngineeringPerformance.Infrastructure` — desktop-реализация этапа миграции WPF → Web.

При миграции использовались:
- `*ViewModel` → `client/src/pages/*` + API
- `EngineeringPerformance.Infrastructure/Services/*` → `backend/EngineeringPerformance.Infrastructure/*`

Инвентаризация WPF-логики (устарела как operational doc): [_doc/inventory/business_logic_inventory.md](../inventory/business_logic_inventory.md).

---

## Cutover

Параллельная эксплуатация web + WPF **завершена** с точки зрения разработки: production-развитие — **только web**. UAT и sign-off — по web-клиенту (`_doc/plans/uat-phase4-checklist.md`).
