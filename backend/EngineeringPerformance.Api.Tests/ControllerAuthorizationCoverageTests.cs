using System.Reflection;
using EngineeringPerformance.Api.Authorization;
using EngineeringPerformance.Api.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Xunit;

namespace EngineeringPerformance.Api.Tests;

/// <summary>
/// Страж единого паттерна авторизации (ADR-006): каждое HTTP-действие контроллера
/// защищено <see cref="HasPermissionAttribute"/> / <see cref="DirectoryPermissionAttribute"/>,
/// либо явно помечено <see cref="AllowAnonymousAttribute"/>, либо это self-эндпоинт
/// (данные текущего пользователя) из allowlist ниже. <c>[Authorize(Roles = ...)]</c> запрещён:
/// роли проверяются через матрицу прав, а не через имена ролей в JWT.
/// Тест чисто рефлексивный — БД не нужна.
/// </summary>
public sealed class ControllerAuthorizationCoverageTests
{
    /// <summary>Действия без аутентификации — только вход/выход и Negotiate.</summary>
    private static readonly HashSet<string> AnonymousAllowlist = new(StringComparer.Ordinal)
    {
        "AuthController.Login",
        "AuthController.WindowsIdentity",
        "AuthController.Negotiate",
        "AuthController.Logout",
        "AuthController.Refresh",
        "MfaController.Verify",
    };

    /// <summary>
    /// Self-эндпоинты: авторизованный пользователь работает со своими данными,
    /// объектного права нет по определению. Любое новое имя здесь — осознанное решение.
    /// </summary>
    private static readonly HashSet<string> SelfAllowlist = new(StringComparer.Ordinal)
    {
        "AuthController.Me",
        "AuthController.LogoutAll",
        "AuthController.GetSessions",
        "UsersController.GetCurrentUser",
        "UsersController.ChangeOwnPassword",
        "UserPreferencesController.*",
        "UserSettingsController.*",
        "MfaController.Setup",
        "MfaController.VerifySetup",
        "MfaController.Disable",
        "MfaController.Status",
        "SystemSettingsController.Get",
    };

    private static IEnumerable<(Type Controller, MethodInfo Action)> Actions() =>
        typeof(AuthController).Assembly
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(ControllerBase).IsAssignableFrom(t))
            .SelectMany(t => t
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => m.GetCustomAttributes<HttpMethodAttribute>(inherit: true).Any())
                .Select(m => (t, m)));

    private static bool InAllowlist(HashSet<string> list, Type controller, MethodInfo action) =>
        list.Contains($"{controller.Name}.{action.Name}") || list.Contains($"{controller.Name}.*");

    private static bool HasAny<TAttr>(Type controller, MethodInfo action) where TAttr : Attribute =>
        action.GetCustomAttributes<TAttr>(inherit: true).Any()
        || controller.GetCustomAttributes<TAttr>(inherit: true).Any();

    [Fact]
    public void EveryAction_IsProtectedByPermissionOrExplicitlyAllowlisted()
    {
        var violations = new List<string>();

        foreach (var (controller, action) in Actions())
        {
            var key = $"{controller.Name}.{action.Name}";

            if (HasAny<AllowAnonymousAttribute>(controller, action))
            {
                if (!InAllowlist(AnonymousAllowlist, controller, action))
                    violations.Add($"{key}: [AllowAnonymous] not in allowlist");
                continue;
            }

            if (HasAny<HasPermissionAttribute>(controller, action)
                || HasAny<DirectoryPermissionAttribute>(controller, action))
                continue;

            if (InAllowlist(SelfAllowlist, controller, action))
            {
                if (!HasAny<AuthorizeAttribute>(controller, action))
                    violations.Add($"{key}: self endpoint without [Authorize]");
                continue;
            }

            violations.Add($"{key}: no [HasPermission]/[DirectoryPermission] and not in self allowlist");
        }

        Assert.True(violations.Count == 0, "Unprotected controller actions:\n" + string.Join('\n', violations));
    }

    [Fact]
    public void NoAction_UsesRoleBasedAuthorize()
    {
        var offenders = Actions()
            .Where(x => x.Controller.GetCustomAttributes<AuthorizeAttribute>(true)
                            .Concat(x.Action.GetCustomAttributes<AuthorizeAttribute>(true))
                            .Any(a => !string.IsNullOrEmpty(a.Roles)))
            .Select(x => $"{x.Controller.Name}.{x.Action.Name}")
            .Distinct()
            .ToList();

        Assert.True(offenders.Count == 0, "[Authorize(Roles=...)] is forbidden (use [HasPermission]):\n" + string.Join('\n', offenders));
    }

    [Fact]
    public void Allowlists_ContainOnlyExistingActions()
    {
        var known = Actions().Select(x => $"{x.Controller.Name}.{x.Action.Name}").ToHashSet(StringComparer.Ordinal);
        var controllers = Actions().Select(x => x.Controller.Name).ToHashSet(StringComparer.Ordinal);

        var stale = AnonymousAllowlist.Concat(SelfAllowlist)
            .Where(e => e.EndsWith(".*", StringComparison.Ordinal)
                ? !controllers.Contains(e[..^2])
                : !known.Contains(e))
            .ToList();

        Assert.True(stale.Count == 0, "Allowlist entries without a matching action:\n" + string.Join('\n', stale));
    }
}
