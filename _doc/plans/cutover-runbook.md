# Runbook: Cutover WPF → Web

> **Статус (2026-07-14):** cutover с точки зрения **разработки завершён** — активен только web. Runbook сохранён для ops/rollback. Политика: [web-only-development-plan.md](./web-only-development-plan.md).

**Обновлено:** 2026-07-14  
**Связано:** `_install/DEPLOY.md`, `uat-phase4-checklist.md`

---

## Предусловия

- [ ] UAT пройден (чеклист + sign-off)
- [ ] `_doc/security/web-security-audit.md` — prod checklist выполнен
- [ ] Backup БД создан
- [ ] WPF-дистрибутив доступен для rollback

---

## Неделя −1: подготовка

| День | Действие | Ответственный |
|------|----------|---------------|
| Пн | Рассылка пользователям: дата cutover, URL web, логин/SPNEGO | Admin |
| Вт | Smoke: `./scripts/uat-smoke.sh` + `./scripts/perf-smoke.sh` на staging | DevOps |
| Ср | Обучение PM: проекты, импорт, аналитика (1 ч) | PM lead |
| Чт | Обучение Observer + Admin | Admin |
| Пт | Freeze WPF: только hotfix по согласованию | Dev lead |

---

## Недели 1–2: параллельная эксплуатация

- Web и WPF на **одной БД**
- Новые данные вносить **только в web** (рекомендация)
- Ежедневно: журнал `/audit`, обращения пользователей
- Критерий успеха: 0 блокирующих инцидентов за 5 рабочих дней

---

## Cutover day (D0)

1. 08:00 — финальный backup БД
2. 09:00 — deploy web prod (`_install/DEPLOY.md`)
3. 09:30 — `uat-smoke.sh` на prod URL
4. 10:00 — объявление: WPF read-only / не рекомендуется
5. 18:00 — сводка инцидентов

---

## D+1 … D+7: стабилизация

- Мониторинг логов API/web
- Hotfix через web-only pipeline
- WPF не обновлять (кроме критического rollback)

---

## Rollback

См. `_install/DEPLOY.md` § Rollback. Триггеры:

- Потеря данных
- Невозможность импорта / расчёта для >50% PM
- Security incident

---

## D+14: decommission WPF

- [ ] Отзыв WPF installer из раздачи
- [ ] Архивация пакетов (1 год)
- [ ] Обновление `migration_map.md`: Phase 4 ✅
- [ ] `EngineeringPerformance.App` — reference only (уже помечен)
