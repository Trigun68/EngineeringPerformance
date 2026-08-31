# Engineering Performance — Установка и первый запуск

> **⚠️ УСТАРЕЛО (2026-08-26):** гайд описывает **замороженный** WPF-стек (`EngineeringPerformance.App`, `DbInitializer` — перенесён в `archive/`).
> Актуальное развёртывание web-версии: [DEPLOY.md](./DEPLOY.md) (Docker Compose). WPF вне scope разработки — см. [_doc/architecture/wpf-reference-archive.md](../architecture/wpf-reference-archive.md).

## Системные требования

| Компонент | Версия |
|-----------|--------|
| .NET SDK | 10.0+ |
| MS SQL Server | Express 2019+ (или выше) |
| ОС | Windows 10/11 |

## 1. Установка MS SQL Server Express

1. Скачайте [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads)
2. Установите с параметрами по умолчанию
3. Имя экземпляра: `SQLEXPRESS`
4. Режим аутентификации: **Смешанный режим** (Windows + SQL Server)
5. Убедитесь, что служба запущена: `services.msc` → `SQL Server (SQLEXPRESS)`

## 2. Создание базы данных

Откройте SQL Server Management Studio (SSMS) или выполните SQL-скрипт:

```sql
CREATE DATABASE EngineeringPerformance;
```

## 3. Настройка строки подключения

Строка подключения хранится в `%APPDATA%\EngineeringPerformance\settings.json`.

При первом запуске используется значение по умолчанию:

```
Server=.\SQLEXPRESS;Database=EngineeringPerformance;Trusted_Connection=True;
```

> ⚠️ Параметр `TrustServerCertificate` по умолчанию **выключен**. Включайте его (через окно
> настроек подключения, чек-бокс «Доверять серверному сертификату») **только** для
> локального LocalDB / SQL Express. Для удалённых SQL-серверов это небезопасно (MitM по TLS).

> ⚠️ Авто-вход по Windows-пользователю по умолчанию **выключен**. Включается чек-боксом
> «Авто-вход по Windows-пользователю» в окне настроек. На общих ПК / RDP включать
> не рекомендуется — учётка будет автоматически авторизована без пароля.

Если ваш SQL Server отличается:
- Запустите приложение
- Нажмите кнопку **шестерёнки** в окне входа
- Укажите свою строку подключения
- Нажмите **Сохранить**

Или задайте переменную окружения:

```powershell
$env:EP_CONNECTION_STRING = "Server=.\SQLEXPRESS;Database=EngineeringPerformance;Trusted_Connection=True;"
```

Чтобы при запуске приложения **не применять EF миграции автоматически** (например, на проде):

```powershell
$env:EP_SKIP_MIGRATIONS = "true"
```

## 4. Инициализация базы данных

### Способ A — через DbInitializer (рекомендуется)

```powershell
cd EngineeringPerformance.DbInitializer
dotnet run
```

Утилита:
- Проверит подключение к БД
- Применит миграции
- Заполнит тестовыми данными
- Выведет сводку

### Способ B — автоматически при запуске приложения

При первом запуске приложения база данных инициализируется автоматически через `DatabaseSeeder.SeedAsync()`.

## 5. Сборка приложения

```powershell
dotnet build
```

## 6. Запуск приложения

```powershell
cd EngineeringPerformance.App
dotnet run
```

Или из Visual Studio: установите `EngineeringPerformance.App` как startup-проект и нажмите F5.

## 7. Тестовые учётные записи

| Логин | Пароль | Роль | Описание |
|-------|--------|------|----------|
| `admin` | `admin` | Администратор | Полный доступ ко всем функциям |
| `manager` | `manager` | Менеджер проекта | Чтение и редактирование согласно правам |
| `observer` | `observer` | Наблюдатель | Только чтение согласно правам |

## 8. Тестовые данные

После инициализации в базе данных будут:

### Пользователи (3)
- Администратор, Менеджер проекта, Наблюдатель

### Виды физических объёмов (2)
- БЕР, КФО

### Направления работ IIC (4)
- IIC — Направления работ IIC (корневой)
  - IIC-01 — Проектирование
  - IIC-02 — Строительство
  - IIC-03 — Пусконаладка

### Нормы направлений (3)
- По 1.0 чел/час для каждого поднаправления

### Проекты (3)
| Код | Название | Видимость |
|-----|----------|-----------|
| PRJ-001 | Строительство объекта Альфа | Все пользователи |
| PRJ-002 | Модернизация объекта Бета | Определённые пользователи |
| PRJ-003 | Реконструкция объекта Гамма | Все пользователи |

### Версии анализа проектов (4)
- PRJ-001: Версия 1.0, Версия 1.1
- PRJ-002: Версия 1.0
- PRJ-003: Версия 1.0

### Права доступа (30)
- Менеджер проекта: **Edit** на все объекты
- Наблюдатель: **Read** на все объекты
- Администратор: полный доступ (не зависит от таблицы прав)

### Доступ к проектам
- `manager` и `observer` имеют доступ к проекту PRJ-002 (SpecificUsers)

## 9. Структура проекта

```
EngineeringPerformance/
├── EngineeringPerformance.Core/          # Доменная модель, интерфейсы, DTO, исключения
│   ├── DTOs/                             # Объекты передачи данных
│   ├── Enums/                            # Перечисления (UserRole, AccessLevel, ...)
│   ├── Exceptions/                       # Пользовательские исключения
│   ├── Models/                           # Базовые модели (DirectoryItem, RegisterEntry, DataTableRecord)
│   └── Services/                         # Интерфейсы сервисов
│
├── EngineeringPerformance.Infrastructure/ # Реализация сервисов, EF Core, валидаторы
│   ├── Data/                             # DbContext, сущности, конфигурации
│   │   ├── Entities/                     # Классы сущностей БД
│   │   └── Configurations/               # EF Core конфигурации
│   ├── Services/                         # Реализации сервисов
│   │   ├── AuthenticationService.cs      # Авторизация (Windows + логин/пароль)
│   │   ├── DbConnectionService.cs        # Проверка подключения к БД
│   │   ├── AccessControlService.cs       # Проверка прав доступа
│   │   ├── ProjectVisibilityService.cs   # Видимость проектов
│   │   ├── DatabaseSeeder.cs             # Инициализация БД
│   │   └── DirectoryServiceBase.cs       # Базовый CRUD для справочников
│   └── Validators/                       # FluentValidation валидаторы
│
├── EngineeringPerformance.App/           # WPF приложение (UI)
│   ├── App.xaml / App.xaml.cs            # Точка входа, DI, Serilog
│   ├── Services/                         # NavigationService
│   ├── ViewModels/                       # ViewModel'ы (MVVM)
│   │   ├── MainViewModel.cs              # Главное окно, навигация, dashboard
│   │   ├── LoginViewModel.cs             # Авторизация
│   │   ├── DbSettingsViewModel.cs        # Настройки БД
│   │   ├── DirectoryViewModelBase.cs     # Базовый VM для справочников
│   │   └── ProjectDashboardViewModel.cs  # Dashboard проектов
│   └── Views/                            # XAML окна и контролы
│       ├── MainWindow.xaml               # Главное окно (sidebar + content)
│       ├── LoginWindow.xaml              # Окно входа
│       ├── DbSettingsWindow.xaml         # Настройки БД
│       ├── DirectoryWindowBase.xaml      # Базовое окно справочника
│       ├── ProjectDashboardView.xaml     # Dashboard проектов
│       └── Forms/                        # Заглушки форм
│
├── EngineeringPerformance.DbInitializer/ # Утилита инициализации БД
│   └── Program.cs                        # Консольный инициализатор
│
└── EngineeringPerformance.Tests/         # Unit-тесты
```

## 10. Устранение проблем

### Ошибка подключения к БД
- Убедитесь, что SQL Server Express запущен
- Проверьте строку подключения через окно настроек (шестерёнка)
- Попробуйте подключиться через SSMS с теми же параметрами

### Ошибка миграций
```powershell
cd EngineeringPerformance.Infrastructure
dotnet ef database update --startup-project ../EngineeringPerformance.App
```

### Ошибка сборки
```powershell
dotnet restore
dotnet build
```

### Ошибка DevExpress
Убедитесь, что DevExpress WPF установлен. Предупреждение DX1001 не влияет на работу — это лицензионное уведомление.
