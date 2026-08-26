# Технологический стек

## Платформа

| Компонент | Версия |
|-----------|--------|
| .NET | 10.0 |
| Целевые фреймворки | `net10.0` (Infrastructure, Core, Tests), `net10.0-windows` (App) |
| База данных | SQL Server Express |
| Строка подключения | `Server=.\SQLEXPRESS;Database=EngineeringPerformance;Trusted_Connection=True;TrustServerCertificate=True;` |

## Структура решения

```
EngineeringPerformance/
├── EngineeringPerformance.App/          # WPF-приложение (MVVM)
├── EngineeringPerformance.Core/         # Бизнес-логика, интерфейсы сервисов
├── EngineeringPerformance.Infrastructure/ # EF Core, репозитории, сервисы
└── EngineeringPerformance.Tests/        # xUnit-тесты
```

## Ключевые пакеты

### App (WPF)
| Пакет | Версия |
|-------|--------|
| CommunityToolkit.Mvvm | 8.4.2 |
| DevExpress (WPF) | 25.2.6 |
| Microsoft.Extensions.DependencyInjection | 10.x |
| Serilog | 4.3.1 |

### Infrastructure
| Пакет | Версия |
|-------|--------|
| Microsoft.EntityFrameworkCore.SqlServer | 10.0.5 |
| ClosedXML | 0.104.2 |
| PDFsharp | 6.2.0 |

### Tests
| Пакет | Версия |
|-------|--------|
| xunit | 2.9.2 |
| FluentAssertions | 8.9.0 |
| Microsoft.EntityFrameworkCore.InMemory | 10.0.x |

## Архитектурные слои

```
App (WPF, MVVM)
    └── Core (интерфейсы, доменные модели)
        └── Infrastructure (EF Core, реализации сервисов)
```

### App — паттерны
- **MVVM** через `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`)
- **DI-контейнер** — `Microsoft.Extensions.DependencyInjection`, конфигурируется в `App.xaml.cs`
- **NavigationService** — кэширует View/ViewModel (`_viewCache`, `_loaded` HashSet)
- **ILoadableViewModel** — ленивая загрузка данных при первом показе вкладки

### Infrastructure — паттерны
- **Repository** через EF Core `AppDbContext`
- **IDbContextFactory<AppDbContext>** для сервисов с несколькими областями видимости
- **DataImportService** — парсинг Excel через ClosedXML, два шаблона: `LegacyTemplate` и `SpisokRabot`

## UI-фреймворк

- **DevExpress 25.2.6**: `DXTabControl`/`DXTabItem`, `GridControl`, `TreeListView`, диалоги
- Стили вкладок страниц верхнего уровня: `PageTabControlStyle` / `PageTabItemStyle` (в `App.xaml`)
- Конвертеры значений: `NullToCollapsedConverter`, `NullToBoolConverter`, `BoolToVisibilityConverter`, `InverseBooleanToVisibilityConverter`

## База данных

- **ORM**: Entity Framework Core 10, Code-First
- **Миграции**: 28 штук, хранятся в `EngineeringPerformance.Infrastructure/Migrations/`
- **Последняя миграция**: `20260505212529_AddProjectPhysicalVolumeWorkFields`
- Применение: `dotnet ef database update` в проекте Infrastructure

## Документация проекта

```
_doc/
├── adr/         # Архитектурные решения (ADR-NNN-*.md)
├── changelog/   # CHANGELOG.md (раздел [Unreleased])
├── database/    # Схемы, описания таблиц
├── features/    # Описание функциональности
└── ui/          # Макеты, описание экранов
```
