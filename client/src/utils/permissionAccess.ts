import type { AccessLevel } from '../types/auth';

/** SuperAdmin: текущее имя, legacy Administrator, или RoleId === 1. */
export function isSuperAdmin(
  roleName: string | undefined,
  roleId?: number | null,
): boolean {
  if (roleId === 1) return true;
  return roleName === 'SuperAdmin' || roleName === 'Administrator';
}

/** Уровень доступа к объекту (0 = None, 1 = Read, 2 = Edit). */
export function getPermissionLevel(
  permissions: Record<string, AccessLevel> | undefined,
  objectName: string,
  roleName: string | undefined,
  roleId?: number | null,
): AccessLevel {
  if (isSuperAdmin(roleName, roleId)) return 2;
  return permissions?.[objectName] ?? 0;
}

/** Проверка чтения — зеркало WPF IAccessControlService.CanRead. */
export function canReadPermission(
  permissions: Record<string, AccessLevel> | undefined,
  objectName: string,
  roleName: string | undefined,
  roleId?: number | null,
): boolean {
  return getPermissionLevel(permissions, objectName, roleName, roleId) >= 1;
}

/** Проверка редактирования — зеркало WPF IAccessControlService.CanEdit. */
export function canEditPermission(
  permissions: Record<string, AccessLevel> | undefined,
  objectName: string,
  roleName: string | undefined,
  roleId?: number | null,
): boolean {
  return getPermissionLevel(permissions, objectName, roleName, roleId) >= 2;
}

export interface MainNavItemConfig {
  path: string;
  label: string;
  objectName: string;
  end?: boolean;
}

/** Порядок и объекты прав главного меню — как WPF MainViewModel.BuildNavigation. */
export const MAIN_NAV_ITEMS: readonly MainNavItemConfig[] = [
  { path: '/', label: 'Главная', objectName: 'Dashboard', end: true },
  { path: '/projects', label: 'Проекты', objectName: 'Projects' },
  { path: '/analytics', label: 'Аналитика', objectName: 'GroupTrend' },
  { path: '/references', label: 'Справочники', objectName: 'References' },
  { path: '/settings/users', label: 'Настройки', objectName: 'SettingsPage' },
  { path: '/help', label: 'Справка', objectName: 'HelpPage' },
] as const;

/** Видимость пункта «Настройки»: SettingsPage или Users (как в WPF для подразделов). */
export function canAccessSettings(
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined,
  roleId?: number | null,
): boolean {
  if (isSuperAdmin(roleName, roleId)) return true;
  return (
    canReadPermission(permissions, 'SettingsPage', roleName, roleId)
    || canReadPermission(permissions, 'Users', roleName, roleId)
  );
}

export function isMainNavItemVisible(
  item: MainNavItemConfig,
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined,
  roleId?: number | null,
): boolean {
  if (item.objectName === 'SettingsPage') {
    return canAccessSettings(permissions, roleName, roleId);
  }
  return canReadPermission(permissions, item.objectName, roleName, roleId);
}

/** Первый доступный маршрут для редиректа при отсутствии прав. */
export function getFirstAccessiblePath(
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined,
  roleId?: number | null,
): string | null {
  for (const item of MAIN_NAV_ITEMS) {
    if (isMainNavItemVisible(item, permissions, roleName, roleId)) {
      return item.path;
    }
  }
  return null;
}
