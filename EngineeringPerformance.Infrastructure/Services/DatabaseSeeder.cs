using EngineeringPerformance.Core;
using EngineeringPerformance.Core.Enums;
using EngineeringPerformance.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EngineeringPerformance.Infrastructure.Services;

/// <summary>
/// Идемпотентное заполнение БД ролями, демо-пользователями, справочниками и тестовыми проектами.
/// </summary>
/// <remarks>
/// Безопасность: при первом создании пользователей задаются пароли по умолчанию (см. метод <c>SeedUsersAsync</c>).
/// В эксплуатации их необходимо сменить сразу после первого входа; учётные данные не предназначены для продакшена «как есть».
/// </remarks>
public static class DatabaseSeeder
{
    /// <summary>
    /// Применяет EF Core миграции. Должен вызываться явно — не из обычного App-startup в проде.
    /// </summary>
    public static async Task MigrateAsync(AppDbContext context)
    {
        if (context.Database.IsRelational())
            await context.Database.MigrateAsync();
    }

    /// <summary>
    /// Заполняет БД начальными данными. Идемпотентно — пропускает уже существующие наборы.
    /// Не вызывает <see cref="MigrateAsync"/> — миграции должны быть применены отдельно.
    /// </summary>
    public static async Task SeedAsync(AppDbContext context)
    {
        if (!await context.Roles.AnyAsync())
            await SeedRolesAsync(context);

        if (!await context.Users.AnyAsync())
            await SeedUsersAsync(context);

        if (!await context.AccessRights.AnyAsync())
            await SeedAccessRightsAsync(context);
        else
            await BackfillAccessRightsAsync(context);

        if (!await context.UnitsOfMeasurement.AnyAsync())
            await SeedUnitsOfMeasurementAsync(context);

        if (!await context.PhysicalVolumeTypes.AnyAsync())
            await SeedPhysicalVolumeTypesAsync(context);

        if (!await context.IicWorkDirections.AnyAsync())
        {
            await SeedIicWorkDirectionsAsync(context);
            await SeedIicWorkDirectionNormsAsync(context);
        }

        if (!await context.Projects.AnyAsync())
            await SeedProjectsAsync(context);

        if (!await context.ProjectPermissions.AnyAsync())
            await SeedProjectPermissionsAsync(context);
    }

    private static async Task SeedRolesAsync(AppDbContext context)
    {
        var adminRole = new Role { Name = nameof(UserRole.Administrator), Description = "Полный доступ ко всем функциям системы" };
        var managerRole = new Role { Name = nameof(UserRole.ProjectManager), Description = "Управление проектами и данными" };
        var observerRole = new Role { Name = nameof(UserRole.Observer), Description = "Только чтение" };

        context.Roles.AddRange(adminRole, managerRole, observerRole);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUsersAsync(AppDbContext context)
    {
        var adminRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == nameof(UserRole.Administrator));
        var managerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == nameof(UserRole.ProjectManager));
        var observerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == nameof(UserRole.Observer));

        if (adminRole is null || managerRole is null || observerRole is null)
            return;

        var adminUser = new User
        {
            Username = "admin",
            DisplayName = "Администратор",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin"),
            RoleId = adminRole.Id,
            IsActive = true
        };

        var managerUser = new User
        {
            Username = "manager",
            DisplayName = "Менеджер проекта",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("manager"),
            RoleId = managerRole.Id,
            IsActive = true
        };

        var observerUser = new User
        {
            Username = "observer",
            DisplayName = "Наблюдатель",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("observer"),
            RoleId = observerRole.Id,
            IsActive = true
        };

        context.Users.AddRange(adminUser, managerUser, observerUser);
        await context.SaveChangesAsync();
    }

    private static async Task SeedUnitsOfMeasurementAsync(AppDbContext context)
    {
        var units = new[]
        {
            new UnitOfMeasurement { Name = "м³", Description = "Кубический метр", SortOrder = 0 },
            new UnitOfMeasurement { Name = "м²", Description = "Квадратный метр", SortOrder = 1 },
            new UnitOfMeasurement { Name = "шт", Description = "Штука", SortOrder = 2 },
            new UnitOfMeasurement { Name = "т", Description = "Тонна", SortOrder = 3 },
            new UnitOfMeasurement { Name = "кг", Description = "Килограмм", SortOrder = 4 },
            new UnitOfMeasurement { Name = "час", Description = "Час", SortOrder = 5 },
            new UnitOfMeasurement { Name = "ярд³", Description = "Кубический ярд", SortOrder = 6 },
            new UnitOfMeasurement { Name = "компл.", Description = "Комплект", SortOrder = 7 },
            new UnitOfMeasurement { Name = "км", Description = "Километр", SortOrder = 8 },
            new UnitOfMeasurement { Name = "м", Description = "Метр", SortOrder = 9 },
            new UnitOfMeasurement { Name = "га", Description = "Гектар", SortOrder = 10 },
            new UnitOfMeasurement { Name = "п.м.", Description = "Погонный метр", SortOrder = 11 }
        };

        context.UnitsOfMeasurement.AddRange(units);
        await context.SaveChangesAsync();
    }

    private static async Task SeedPhysicalVolumeTypesAsync(AppDbContext context)
    {
        var berType = new Data.PhysicalVolumeType { Name = "БЕР", Description = "Базовая единица ресурса", SortOrder = 0 };
        var kfoType = new Data.PhysicalVolumeType { Name = "КФО", Description = "Классификатор физических объёмов", SortOrder = 1 };
        context.PhysicalVolumeTypes.AddRange(berType, kfoType);
        await context.SaveChangesAsync();
    }

    private static async Task SeedIicWorkDirectionsAsync(AppDbContext context)
    {
        var rootDirection = new IicWorkDirection { Code = "IIC", Name = "Направления работ IIC", Description = "Классификатор направлений работ", SortOrder = 0 };
        context.IicWorkDirections.Add(rootDirection);
        await context.SaveChangesAsync();

        var subDirections = new[]
        {
            new IicWorkDirection { ParentId = rootDirection.Id, Code = "IIC-01", Name = "Проектирование", Description = "Работы по проектированию", SortOrder = 0 },
            new IicWorkDirection { ParentId = rootDirection.Id, Code = "IIC-02", Name = "Строительство", Description = "Строительные работы", SortOrder = 1 },
            new IicWorkDirection { ParentId = rootDirection.Id, Code = "IIC-03", Name = "Пусконаладка", Description = "Пусконаладочные работы", SortOrder = 2 }
        };
        context.IicWorkDirections.AddRange(subDirections);
        await context.SaveChangesAsync();
    }

    private static async Task SeedIicWorkDirectionNormsAsync(AppDbContext context)
    {
        var fallbackUnit = await context.UnitsOfMeasurement.FirstOrDefaultAsync(u => u.Name == "час");
        if (fallbackUnit is null)
            return;

        var root = await context.IicWorkDirections.FirstOrDefaultAsync(d => d.Code == "IIC" && d.ParentId == null);
        if (root is not null && root.UnitId is null)
        {
            root.UnitId = fallbackUnit.Id;
            await context.SaveChangesAsync();
        }

        var directions = await context.IicWorkDirections
            .Include(d => d.Unit)
            .ToListAsync();

        var byId = directions.ToDictionary(d => d.Id);
        var subDirections = directions.Where(d => d.ParentId.HasValue).ToList();

        var norms = subDirections.Select(d => new IicWorkDirectionNorm
        {
            WorkDirectionId = d.Id,
            UnitId = IicWorkDirectionDisplayUnit.ResolveUnitId(d, byId) ?? fallbackUnit.Id,
            EffectiveFrom = DateTime.UtcNow
        }).ToList();

        context.IicWorkDirectionNorms.AddRange(norms);
        await context.SaveChangesAsync();
    }

    private static async Task SeedProjectsAsync(AppDbContext context)
    {
        var projects = new[]
        {
            new Project
            {
                Code = "PRJ-001",
                Name = "Строительство объекта Альфа",
                WorkNumber = "РН-001",
                ContractNumber = "Д-2024-001",
                CalendarPlanNumber = "КП-001",
                Type = "Строительство",
                Customer = "Заказчик А",
                Contractor = "Подрядчик А",
                Gip = "Иванов И.И.",
                WorkStatus = "В работе",
                WorkStart = new DateOnly(2024, 1, 15),
                Program = "Программа 1",
            },
            new Project
            {
                Code = "PRJ-002",
                Name = "Модернизация объекта Бета",
                WorkNumber = "РН-002",
                ContractNumber = "Д-2024-002",
                CalendarPlanNumber = "КП-002",
                Type = "Модернизация",
                Customer = "Заказчик Б",
                Contractor = "Подрядчик Б",
                Gip = "Петров П.П.",
                WorkStatus = "Планирование",
                WorkStart = new DateOnly(2024, 6, 1),
                Program = "Программа 2",
            },
            new Project
            {
                Code = "PRJ-003",
                Name = "Реконструкция объекта Гамма",
                WorkNumber = "РН-003",
                ContractNumber = "Д-2024-003",
                CalendarPlanNumber = "КП-003",
                Type = "Реконструкция",
                Customer = "Заказчик В",
                Contractor = "Подрядчик В",
                Gip = "Сидоров С.С.",
                WorkStatus = "Завершён",
                WorkStart = new DateOnly(2023, 3, 1),
                Program = "Программа 3",
            }
        };

        context.Projects.AddRange(projects);
        await context.SaveChangesAsync();

        var versions = new[]
        {
            new ProjectAnalysisVersion { ProjectId = projects[0].Id, VersionName = "План" },
            new ProjectAnalysisVersion { ProjectId = projects[0].Id, VersionName = "Факт" },
            new ProjectAnalysisVersion { ProjectId = projects[0].Id, VersionName = "Прогноз версия 1" },
            new ProjectAnalysisVersion { ProjectId = projects[1].Id, VersionName = "План" },
            new ProjectAnalysisVersion { ProjectId = projects[2].Id, VersionName = "План" }
        };

        context.ProjectAnalysisVersions.AddRange(versions);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// V2-C: вместо <c>ProjectUserAccess</c>+<c>Visibility</c> сидим явные разрешения.
    /// Раньше PRJ-002 был SpecificUsers и manager/observer попадали в ProjectUserAccess;
    /// сейчас то же поведение — но через ProjectPermission.Read.
    /// </summary>
    private static async Task SeedProjectPermissionsAsync(AppDbContext context)
    {
        var manager = await context.Users.FirstOrDefaultAsync(u => u.Username == "manager");
        var observer = await context.Users.FirstOrDefaultAsync(u => u.Username == "observer");
        var restrictedProject = await context.Projects.FirstOrDefaultAsync(p => p.Code == "PRJ-002");

        if (manager is not null && restrictedProject is not null)
        {
            context.ProjectPermissions.Add(new ProjectPermission
            {
                ProjectId = restrictedProject.Id,
                UserId = manager.Id,
                Level = AccessLevel.Edit
            });
        }

        if (observer is not null && restrictedProject is not null)
        {
            context.ProjectPermissions.Add(new ProjectPermission
            {
                ProjectId = restrictedProject.Id,
                UserId = observer.Id,
                Level = AccessLevel.Read
            });
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedAccessRightsAsync(AppDbContext context)
    {
        var pmRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == nameof(UserRole.ProjectManager));
        var observerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == nameof(UserRole.Observer));

        if (pmRole is null || observerRole is null)
            return;

        var accessRights = new List<AccessRight>();
        foreach (var obj in AccessControlConstants.AllObjectNames)
        {
            accessRights.Add(new AccessRight { RoleId = pmRole.Id, ObjectName = obj, AccessLevel = AccessLevel.Edit });
            accessRights.Add(new AccessRight { RoleId = observerRole.Id, ObjectName = obj, AccessLevel = AccessLevel.Read });
        }

        context.AccessRights.AddRange(accessRights);
        await context.SaveChangesAsync();
    }

    // Идемпотентный backfill: добавляет права для тех комбинаций (role, objectName), которых ещё нет в БД.
    // Нужен потому, что AllObjectNames со временем расширяется (например, добавился UserActionLog при фиче
    // «Журнал действий пользователей»), а старый seed работает только при пустой таблице — существующие БД
    // оставались без прав на новые объекты, и в UI «Права доступа» эти строки нельзя было отредактировать.
    private static async Task BackfillAccessRightsAsync(AppDbContext context)
    {
        var pmRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == nameof(UserRole.ProjectManager));
        var observerRole = await context.Roles.FirstOrDefaultAsync(r => r.Name == nameof(UserRole.Observer));

        if (pmRole is null || observerRole is null)
            return;

        var existing = await context.AccessRights
            .Select(r => new { r.RoleId, r.ObjectName })
            .ToListAsync();
        var existingSet = existing.Select(x => (x.RoleId, x.ObjectName)).ToHashSet();

        var toAdd = new List<AccessRight>();
        foreach (var obj in AccessControlConstants.AllObjectNames)
        {
            if (!existingSet.Contains((pmRole.Id, obj)))
                toAdd.Add(new AccessRight { RoleId = pmRole.Id, ObjectName = obj, AccessLevel = AccessLevel.Edit });
            if (!existingSet.Contains((observerRole.Id, obj)))
                toAdd.Add(new AccessRight { RoleId = observerRole.Id, ObjectName = obj, AccessLevel = AccessLevel.Read });
        }

        if (toAdd.Count == 0) return;

        context.AccessRights.AddRange(toAdd);
        await context.SaveChangesAsync();
    }
}
