import type { AccessLevel } from '../types/auth';

export function isAdministrator(roleName: string | undefined): boolean {
  return roleName === 'Administrator';
}

/** Уровень доступа к объекту (0 = None, 1 = Read, 2 = Edit). */
export function getPermissionLevel(
  permissions: Record<string, AccessLevel> | undefined,
  objectName: string,
  roleName: string | undefined,
): AccessLevel {
  if (isAdministrator(roleName)) return 2;
  return permissions?.[objectName] ?? 0;
}

/** Проверка чтения — зеркало WPF IAccessControlService.CanRead. */
export function canReadPermission(
  permissions: Record<string, AccessLevel> | undefined,
  objectName: string,
  roleName: string | undefined,
): boolean {
  return getPermissionLevel(permissions, objectName, roleName) >= 1;
}

/** Проверка редактирования — зеркало WPF IAccessControlService.CanEdit. */
export function canEditPermission(
  permissions: Record<string, AccessLevel> | undefined,
  objectName: string,
  roleName: string | undefined,
): boolean {
  return getPermissionLevel(permissions, objectName, roleName) >= 2;
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
  { path: '/analytics/trends', label: 'Аналитика', objectName: 'GroupTrend' },
  { path: '/references', label: 'Справочники', objectName: 'References' },
  { path: '/settings/users', label: 'Настройки', objectName: 'SettingsPage' },
  { path: '/help', label: 'Справка', objectName: 'HelpPage' },
] as const;

/** Видимость пункта «Настройки»: SettingsPage или Users (как в WPF для подразделов). */
export function canAccessSettings(
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined,
): boolean {
  if (isAdministrator(roleName)) return true;
  return (
    canReadPermission(permissions, 'SettingsPage', roleName)
    || canReadPermission(permissions, 'Users', roleName)
  );
}

export function isMainNavItemVisible(
  item: MainNavItemConfig,
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined,
): boolean {
  if (item.objectName === 'SettingsPage') {
    return canAccessSettings(permissions, roleName);
  }
  return canReadPermission(permissions, item.objectName, roleName);
}

/** Первый доступный маршрут для редиректа при отсутствии прав. */
export function getFirstAccessiblePath(
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined,
): string | null {
  for (const item of MAIN_NAV_ITEMS) {
    if (isMainNavItemVisible(item, permissions, roleName)) {
      return item.path;
    }
  }
  return null;
}
