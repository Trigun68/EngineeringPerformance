# Агрегация физических объёмов: правило Qty × Coefficient × IsAssessment

_Последнее обновление: 2026-05-06_

## Суть правила

Хранимое поле `ProjectPhysicalVolume.Quantity` содержит количество в единицах измерения **номенклатурной позиции проекта**, которые могут отличаться от единиц группы ИИС.

Поле `PhysicalVolume.Coefficient` служит переводным коэффициентом:

```
ОбъёмВЕдиницахИИС = Quantity × Coefficient
```

**Во всех аналитических расчётах и отображениях агрегированного объёма используется именно произведение `Quantity × Coefficient`, а не сырое `Quantity`.**

### Фильтр «Оценка» (IsAssessment)

Помимо умножения на коэффициент, **в агрегацию включаются только строки физических объёмов, у которых `PhysicalVolume.IsAssessment == true`** (поле «Оценка» в справочнике физических объёмов установлено в «Да»).

Строки со значением `false` или `null` — **исключаются из всех аналитических агрегатов**.

```
// Включается в агрегат
WHERE PhysicalVolume.IsAssessment = true

// Исключается
WHERE PhysicalVolume.IsAssessment = false  -- «Нет»
WHERE PhysicalVolume.IsAssessment IS NULL  -- «Не выбрано»
```

Исключение: raw-таблица первоисточника (`ProjectPhysicalVolumesWindow`) отображает все строки без фильтрации — там пользователь видит и редактирует полный справочник.

## Точки вывода — карта

Колонка «Фильтр IsAssessment» показывает, применяется ли ограничение `PhysicalVolume.IsAssessment == true`.

| Точка | Компонент | Поле/колонка | Значение | Фильтр IsAssessment | Статус |
|---|---|---|---|---|---|
| Расчёт Z-score | `PerformanceService.GetPerformanceDataAsync` | `volumeByDirection` / `FactPhysicalVolume` | `SUM(Qty × Coeff)` через `QuantityInIicGroupUnits()` | ✅ применяется | ✅ корректно |
| Прогноз трудоёмкости | `ProjectForecastService.GenerateForecastAsync` | `volumeByDirection` / `PlannedVolume` | `SUM(Qty × Coeff)` через `QuantityInIicGroupUnits()` | ✅ применяется | ✅ корректно |
| Отчёт «Анализ версии» — метрики направления | `ProjectVersionAnalysisService` | `FactPhysicalVolume` | берётся из `PerformanceService` | ✅ через PerformanceService | ✅ корректно |
| Отчёт «Анализ версии» — таблица ФО карточки | `ProjectVersionAnalysisService.BuildPhysicalVolumes` + `ProjectVersionAnalysisWindow`, колонка «Итого» | `Total = Quantity × Coefficient` | построчно | ✅ применяется | ✅ корректно |
| Отчёт «Анализ версии» — строка ИТОГО | `ProjectVersionAnalysisViewModel.TotalFactPhysicalVolume` | сумма `FactPhysicalVolume` по направлениям | агрегат из `PerformanceService` | ✅ через PerformanceService | ✅ корректно |
| Окно проекта — вкладка «Производительность» | `ProjectEditWindow`, колонка «Факт ФО» | `FactPhysicalVolume` из `PerformanceService` | `SUM(Qty × Coeff)` | ✅ применяется | ✅ корректно |
| Планирование — ввод плановых объёмов | `PlanningForm`, колонка «Плановый объём» | `PlannedVolume` из `ForecastService` | базис из `SUM(Qty × Coeff)` | ✅ применяется | ✅ корректно |
| Строка «(пусто)» — нераспределённые ФО | `PerformanceService.GetPerformanceDataAsync` | `unallocatedPhysicalVolume` | `SUM(Qty × Coeff)` без ClassifierId | ✅ применяется | ✅ корректно |
| Страница данных — raw-грид ФО | `ProjectPhysicalVolumesWindow`, колонка «Кол-во» | `Quantity` (сырое) | первоисточник в БД, не аналитика | ➖ не применяется (намеренно) | ℹ️ намеренно сырые данные |

## Реализация

Метод-расширение `QuantityInIicGroupUnits` в `ProjectPhysicalVolumeIicUnits.cs`:

```csharp
public static decimal QuantityInIicGroupUnits(this ProjectPhysicalVolume pv)
{
    var k = pv.PhysicalVolume?.Coefficient ?? 1m;
    return pv.Quantity * k;
}
```

Обратное преобразование (из единиц ИИС в сырое `Quantity`) — `ProjectQuantityFromIicGroupUnits`.

## Правило для новых фич

> **При добавлении любого нового вывода агрегированного физического объёма:**
> 1. Использовать `QuantityInIicGroupUnits()` (или `Quantity × Coefficient`), а не сырое `Quantity`.
> 2. Применять фильтр `PhysicalVolume.IsAssessment == true` — строки с `false` и `null` в аналитику не включаются.

Исключение: raw-таблицы первоисточника (`ProjectPhysicalVolumesWindow`) — там фильтрация и пересчёт намеренно отсутствуют, т.к. отображается полный первоисточник импорта.

### Типовой паттерн фильтрации (C#)

```csharp
var volumeByDirection = physicalVolumes
    .Where(pv => pv.PhysicalVolume?.ClassifierId.HasValue == true
              && pv.PhysicalVolume.IsAssessment == true)
    .GroupBy(pv => pv.PhysicalVolume!.ClassifierId!.Value)
    .ToDictionary(g => g.Key, g => g.Sum(pv => pv.QuantityInIicGroupUnits()));
```

## Связанные файлы

- `EngineeringPerformance.Infrastructure/Services/ProjectPhysicalVolumeIicUnits.cs` — метод-расширение
- `EngineeringPerformance.Infrastructure/Services/PerformanceService.cs` — Z-score расчёт
- `EngineeringPerformance.Infrastructure/Services/ProjectForecastService.cs` — прогноз
- `EngineeringPerformance.Infrastructure/Services/ProjectVersionAnalysisService.cs` — отчёт версии
- `EngineeringPerformance.Core/DTOs/PerformanceRowDto.cs` — `FactPhysicalVolume`
- `EngineeringPerformance.Core/DTOs/ForecastIicVolumeRowDto.cs` — `PlannedVolume`
- `docs/database/entities/PhysicalVolume.md` — описание поля `Coefficient`
- `docs/features/performance-analytics.md` — Z-score pipeline
