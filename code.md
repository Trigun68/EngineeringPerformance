# Стандарты кода

## Язык и именование

| Элемент | Правило |
|---------|---------|
| Классы, методы, свойства, переменные | English |
| Комментарии в коде | Русский (объясняют «почему», не «что») |
| Документация (`_doc/`, `_doc/`) | Русский |
| Коммиты (заголовок) | English, Conventional Commits |

## MVVM-паттерн (App)

### ViewModel
```csharp
// Свойства через CommunityToolkit.Mvvm
[ObservableProperty]
private string _projectName = string.Empty;

// Команды
[RelayCommand]
private async Task LoadDataAsync()
{
    // ...
}
```

### ILoadableViewModel
Реализуется всеми VM, которые загружаются лениво при первом показе вкладки:
```csharp
public interface ILoadableViewModel
{
    Task LoadAsync();
}
```

### Lazy-loading вкладок
Используется в `ReferencesPageView.xaml.cs` и `SettingsPageView.xaml.cs`:
- `DXTabControl` (DevExpress) — верхний уровень страниц Справочники и Настройки
- WPF `TabControl` со стилем `PageTabControlStyle` — внутренние вкладки (ProjectsPageView)
- Контент вкладки создаётся только при первом выборе (`dxTab.Content == null`)

## Dependency Injection

```csharp
// Регистрация в App.xaml.cs
services.AddSingleton<MainViewModel>();
services.AddTransient<ProjectsPageView>();
services.AddTransient<ProjectsPageViewModel>();

// Внедрение через конструктор — ВСЕГДА
public MyService(IDbContextFactory<AppDbContext> dbFactory, ILogger<MyService> logger)
{
    _dbFactory = dbFactory;
    _logger = logger;
}
```

**Запрещено**: `new MyService()` внутри классов, service locator.

## Доступ к базе данных

Использовать `IDbContextFactory<AppDbContext>` для сервисов с несколькими областями видимости:

```csharp
await using var db = await _dbFactory.CreateDbContextAsync();
var result = await db.Projects
    .Where(p => p.IsActive)
    .ToListAsync();
```

## Фильтр IsAssessment

**Критическое правило**: в аналитических запросах физические объёмы фильтруются по `IsAssessment == true`:

```csharp
var volumeByDirection = physicalVolumes
    .Where(pv => pv.PhysicalVolume?.ClassifierId.HasValue == true
              && pv.PhysicalVolume.IsAssessment == true)
    .GroupBy(pv => pv.PhysicalVolume!.ClassifierId!.Value)
    .ToDictionary(g => g.Key, g => g.Sum(pv => pv.QuantityInIicGroupUnits()));
```

## Расширяемость импорта

Шаблоны импорта определяются через `PhysicalVolumeColumnMap`:
```csharp
private static readonly PhysicalVolumeColumnMap LegacyTemplate = new() { WorkId = 0, ... };
private static readonly PhysicalVolumeColumnMap SpisokRabot    = new() { WorkId = 8, ... };
```

`WorkId = 0` означает «колонка отсутствует в шаблоне».

## Структура сущностей EF Core

- Конфигурации в `EngineeringPerformance.Infrastructure/Data/Configurations/`
- Одна конфигурация = один класс `IEntityTypeConfiguration<T>`
- `HasMaxLength` обязателен для строковых полей (nvarchar с ограничением)
- Миграции только через `dotnet ef migrations add <Name>`; ручное редактирование запрещено

## Денормализация

`ProjectPhysicalVolume` хранит денормализованные поля `CatalogCode` и `CatalogName`:
- Заполняются при импорте из `PhysicalVolume.Code` / `PhysicalVolume.Name`
- Цель: исключить JOIN при отображении таблицы импортированных объёмов

## Обработка ошибок

```csharp
// Специфичные исключения, не Exception
throw new InvalidOperationException($"PhysicalVolume {id} not found");

// В фоновых Task — логировать, не глотать
.ContinueWith(t =>
{
    if (t.IsFaulted)
        _logger.LogError(t.Exception, "Background load failed");
}, TaskScheduler.FromCurrentSynchronizationContext());
```

## Конвенции именования файлов

| Тип | Шаблон | Пример |
|-----|--------|--------|
| ViewModel | `*ViewModel.cs` | `ProjectsPageViewModel.cs` |
| View (XAML) | `*View.xaml` | `ProjectsPageView.xaml` |
| Конфигурация EF | `*Configuration.cs` | `ProjectPhysicalVolumeConfiguration.cs` |
| Миграция | `YYYYMMDDHHMMSS_*.cs` | `20260505212529_AddWorkFields.cs` |
| Конвертер | `*Converter.cs` или группа `*Converters.cs` | `NullConverters.cs` |

## Запреты

- ❌ `new` для сервисов внутри классов
- ❌ `async void` (кроме обработчиков событий WPF)
- ❌ прямой `DbContext` без `using` / `await using`
- ❌ магические числа и строки — использовать константы
- ❌ комментарии на английском в коде (только в CLAUDE.md / tech-файлах)
