# Тестирование

## Структура тестов

Все тесты находятся в `EngineeringPerformance.Tests/`.

```
EngineeringPerformance.Tests/
├── PerformanceServiceTests.cs
├── ProjectVersionAnalysisServiceTests.cs
├── ProjectForecastServiceTests.cs
├── ProjectAnalysisServiceTests.cs
└── AnalysisVersionSnapshotTests.cs
```

## Запуск тестов

```bash
# Все тесты
dotnet test

# С подробным выводом
dotnet test --verbosity normal

# Конкретный файл
dotnet test --filter "FullyQualifiedName~ProjectForecastServiceTests"
```

## Стек

| Компонент | Назначение |
|-----------|-----------|
| xunit 2.9.2 | Тест-фреймворк |
| FluentAssertions 8.9.0 | Читаемые ассерты |
| EF Core InMemory | In-memory БД для изоляции |

## Паттерн тестов — SeedAsync

Каждый тестовый класс содержит вспомогательный метод `SeedAsync`, который заполняет in-memory базу тестовыми данными:

```csharp
private async Task SeedAsync(AppDbContext db)
{
    db.PhysicalVolumes.Add(new PhysicalVolume
    {
        Id = 50, Code = "PV-A", Name = "Объём А",
        IsAssessment = true,   // ОБЯЗАТЕЛЬНО — иначе аналитика игнорирует запись
        ClassifierId = 1, UnitGroupId = 1
    });
    await db.SaveChangesAsync();
}
```

### Критическое требование: IsAssessment = true

Фильтр `IsAssessment == true` применяется во всех аналитических сервисах.
**Все тестовые `PhysicalVolume` объекты должны иметь `IsAssessment = true`**, иначе тест пройдёт, но данные будут проигнорированы, что даёт ложный результат.

## Структура теста (AAA)

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedResult()
{
    // Arrange
    var options = new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;
    await using var db = new AppDbContext(options);
    await SeedAsync(db);
    var service = new MyService(new DbContextFactoryStub(options));

    // Act
    var result = await service.DoSomethingAsync(...);

    // Assert
    result.Should().NotBeNull();
    result.Value.Should().Be(expectedValue);
}
```

## Что тестируется

| Сервис | Файл тестов | Покрытие |
|--------|------------|---------|
| `PerformanceService` | `PerformanceServiceTests.cs` | Z-score, PEPM, агрегация по направлениям |
| `ProjectVersionAnalysisService` | `ProjectVersionAnalysisServiceTests.cs` | Сравнение версий, снапшоты |
| `ProjectForecastService` | `ProjectForecastServiceTests.cs` | Прогноз Base/Opt/Pes |
| `ProjectAnalysisService` | `ProjectAnalysisServiceTests.cs` | Анализ факта |
| Снапшоты | `AnalysisVersionSnapshotTests.cs` | Изоляция версий |

## Что не тестируется (намеренно)

- WPF ViewModels (UI-логика)
- DevExpress-компоненты
- `DataImportService` — интеграционный, требует реального файла Excel
- `NavigationService`

## Добавление нового теста

1. Создать метод `SeedAsync` с минимально необходимыми данными.
2. Использовать `UseInMemoryDatabase(Guid.NewGuid().ToString())` — уникальное имя на каждый тест.
3. Установить `IsAssessment = true` на всех `PhysicalVolume`.
4. Именование: `MethodName_Scenario_ExpectedResult`.
