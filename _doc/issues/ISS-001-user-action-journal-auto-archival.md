# ISS-001: Авто-архивация журнала действий (backend задача)

**Статус:** 📋 backlog
**Создано:** 2026-05-09
**Связь:** [ADR-002](../adr/ADR-002-user-action-journal.md)

## Контекст
В рамках `feat/user-action-journal` реализована **ручная** архивация: на UI кнопки «Сохранить в CSV и удалить» / «Восстановить из CSV». Авто-архивация в первую итерацию не вошла осознанно — фокус на UI и базовой инфраструктуре.

## Что нужно (на будущее)
Регулярная backend-задача, которая:

1. **Триггер**: расписание (cron) или порог количества записей — например, ежедневно в 02:00 или при достижении ≥100 000 записей.
2. **Действие**: экспортировать записи старше N дней (конфигурируемо, по умолчанию 90) в CSV-файл в `archive/UserActionLogs/YYYY-MM-DD.csv`, затем удалить экспортированные строки из БД.
3. **Конфигурация**: `appsettings.json` блок `UserActionLog.AutoArchive`:
   - `Enabled: bool`
   - `OlderThanDays: int` (например 90)
   - `ArchivePath: string` (относительно `%APPDATA%\EngineeringPerformance\archive\` или абсолютный)
   - `Schedule: cron` (например `"0 2 * * *"`)
4. **Идемпотентность**: имя файла включает дату прогона; повторный запуск в тот же день — append к существующему файлу (или skip, если файл уже создан и в БД ничего нет за этот период).
5. **Логирование самой задачи**: успех/неуспех — в Serilog + опционально в сам `UserActionLogs` с `ActionType=SystemArchive` и `UserId=<systemUserId>`.

## Реализация (предполагаемая)
- Использовать `IHostedService` / `BackgroundService` с библиотекой [Quartz.NET](https://www.quartz-scheduler.net/) или ручной таймер на `PeriodicTimer`.
- Регистрация в DI рядом с `Microsoft.Extensions.Hosting`.
- Сервис принимает `IUserActionLogService` для повторного использования `ExportToCsvAndPurgeAsync`.
- Тесты: unit-тест с моком `IUserActionLogService` + integration-тест с `InMemoryDatabase`.

## Зависимости
- Базовая инфраструктура журнала (UAJ-001 … UAJ-006) должна быть слита.
- Решение по cron-библиотеке (Quartz.NET vs самописный таймер).

## Не блокирует
Текущую работу пользователей. Ручная архивация полностью покрывает базовый сценарий.

## Acceptance criteria (когда возьмём в работу)
- [ ] Конфиг `UserActionLog.AutoArchive` валидируется при старте приложения.
- [ ] При `Enabled=false` задача не регистрируется в `IHostedService`.
- [ ] При срабатывании по расписанию архивирует только записи `Timestamp < (UtcNow - OlderThanDays)`.
- [ ] CSV формат идентичен ручному экспорту (один и тот же сервис).
- [ ] Покрытие тестами ≥80%.
- [ ] Документация: `_doc/features/user-action-journal.md` дополняется разделом «Авто-архивация».
