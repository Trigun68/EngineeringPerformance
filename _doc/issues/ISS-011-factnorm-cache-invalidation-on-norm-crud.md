# ISS-011: Кэш FactNorm-аналитики не инвалидируется при CRUD норм

**Статус:** 🔵 Backlog (2026-08-16)
**Категория:** Analytics / Cache
**Приоритет:** P0 (Medium)
**Источник:** `_work/20260816_221500_code_architecture_review.md` §4 (C-02)

## Контекст
`FactNormAnalysisService` кэширует ответы `AnalyzeAsync` в Redis с собственным generation-ключом; инвалидация происходит только внутри самого сервиса (создание нормы из рекомендации, стр. 501).

`IicWorkDirectionNormService` при Create/Update/Delete нормы (стр. 114-115, 157-158, 178-179) инвалидирует кэш справочников (`ICacheInvalidator`) и norm-registry (`_normRegistry.InvalidateCacheAsync`) и запускает пересчёт, но **не трогает кэш FactNorm** → после правки нормы аналитика «факт vs нормы» отдаёт протухшие данные до TTL.

## Решение (направление)
- Вызвать `IFactNormAnalysisService.InvalidateCacheAsync` из `IicWorkDirectionNormService` на всех путях записи (или ввести составной invalidator / общий generation-ключ норм).

## Acceptance Criteria
- [ ] Create/Update/Delete нормы инвалидирует FactNorm-кэш.
- [ ] Тест/ручная проверка: правка нормы → следующий `AnalyzeAsync` возвращает пересчитанные данные (FromCache=false).
- [ ] `dotnet test` зелёный.

## Файлы
- `backend/EngineeringPerformance.Infrastructure/Registers/IicWorkDirectionNormService.cs`
- `backend/EngineeringPerformance.Infrastructure/Analytics/FactNormAnalysisService.cs`

## Связанные
- Отчёт аудита; ISS-010
