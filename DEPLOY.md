# Развёртывание

## Требования к окружению

| Компонент | Версия |
|-----------|--------|
| Windows | 10 / 11 |
| .NET Runtime | 10.0 (Desktop Runtime для WPF) |
| SQL Server Express | 2019 / 2022 |
| Visual Studio / Rider | опционально для разработки |

## Первоначальная установка

### 1. Клонировать репозиторий

```bash
git clone <repo-url>
cd EngineeringPerformance
```

### 2. Настроить строку подключения

Строка подключения задаётся в `EngineeringPerformance.App/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Server=.\\SQLEXPRESS;Database=EngineeringPerformance;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

Для другого экземпляра SQL Server замените `.\SQLEXPRESS` на имя своего экземпляра.

### 3. Создать базу данных и применить миграции

```bash
cd EngineeringPerformance.Infrastructure
dotnet ef database update --startup-project ../EngineeringPerformance.App
```

Применяются все 28 миграций из папки `Migrations/`.

### 4. Запустить приложение

```bash
cd EngineeringPerformance.App
dotnet run
```

Или открыть `EngineeringPerformance.sln` в Visual Studio и запустить проект `EngineeringPerformance.App`.

## Обновление (новая версия)

1. Получить изменения:
   ```bash
   git pull
   ```

2. Применить новые миграции:
   ```bash
   cd EngineeringPerformance.Infrastructure
   dotnet ef database update --startup-project ../EngineeringPerformance.App
   ```

3. Пересобрать и запустить приложение.

## Создание новой миграции (для разработчиков)

```bash
cd EngineeringPerformance.Infrastructure
dotnet ef migrations add <MigrationName> --startup-project ../EngineeringPerformance.App
```

Затем применить:
```bash
dotnet ef database update --startup-project ../EngineeringPerformance.App
```

## Откат миграции

```bash
dotnet ef database update <PreviousMigrationName> --startup-project ../EngineeringPerformance.App
```

Список миграций:
```bash
dotnet ef migrations list --startup-project ../EngineeringPerformance.App
```

## Сборка Release

```bash
dotnet publish EngineeringPerformance.App/EngineeringPerformance.App.csproj \
  -c Release -r win-x64 --self-contained true \
  -o ./publish
```

## Логи

Логи пишутся через Serilog. По умолчанию — в консоль и файл `logs/log-.txt` (ротация по дням).

## CI/CD

На данный момент CI/CD не настроен. Все сборки и деплои выполняются вручную по инструкции выше.

При необходимости настройки GitHub Actions — создать workflow в `.github/workflows/` и добавить шаги:
1. `dotnet restore`
2. `dotnet build`
3. `dotnet test`
4. `dotnet publish`
