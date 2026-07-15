import { useCallback, useMemo } from 'react';
import { useAuthStore } from '../store/useAuthStore';
import type { AccessLevel } from '../types/auth';
import {
  canEditPermission,
  canReadPermission,
  getPermissionLevel,
  isAdministrator,
} from '../utils/permissionAccess';

export interface PermissionResult {
  level: AccessLevel;
  canRead: boolean;
  canEdit: boolean;
  /** Edit-доступ (level >= 2); для кнопок импорта/копирования. */
  hasAccess: boolean;
  hasLevel: (required: AccessLevel) => boolean;
  isAdmin: boolean;
}

/**
 * Проверка прав на объект (как в WPF PermissionService).
 *
 * @example
 * const { hasAccess: canImport } = usePermission('ProjectDataUpload');
 * <Button disabled={!canImport}>Загрузить</Button>
 */
const EMPTY_PERMISSIONS: Record<string, AccessLevel> = {};

export function usePermission(objectName: string): PermissionResult {
  // Стабильная ссылка: `?? {}` в селекторе даёт новый объект на каждый snapshot → риск #185.
  const permissions = useAuthStore((state) => state.user?.permissions) ?? EMPTY_PERMISSIONS;
  const roleName = useAuthStore((state) => state.user?.roleName);
  const admin = isAdministrator(roleName);

  const level: AccessLevel = useMemo(
    () => getPermissionLevel(permissions, objectName, roleName),
    [objectName, permissions, roleName],
  );

  const canRead = level >= 1;
  const canEdit = level >= 2;

  const hasLevel = useCallback(
    (required: AccessLevel) => admin || level >= required,
    [admin, level],
  );

  return {
    level,
    canRead,
    canEdit,
    hasAccess: canEdit,
    hasLevel,
    isAdmin: admin,
  };
}

/**
 * Краткая форма: boolean для заданного уровня (по умолчанию Edit).
 *
 * @example const canImport = usePermissionAccess('ProjectDataUpload');
 */
export function usePermissionAccess(
  requiredPermission: string,
  requiredLevel: AccessLevel = 2,
): boolean {
  const { hasLevel } = usePermission(requiredPermission);
  return hasLevel(requiredLevel);
}

/** Хук для проверки чтения без привязки к компоненту с полным результатом. */
export function useCanRead(objectName: string): boolean {
  const permissions = useAuthStore((state) => state.user?.permissions);
  const roleName = useAuthStore((state) => state.user?.roleName);
  return useMemo(
    () => canReadPermission(permissions, objectName, roleName),
    [objectName, permissions, roleName],
  );
}

/** Хук для проверки редактирования. */
export function useCanEdit(objectName: string): boolean {
  const permissions = useAuthStore((state) => state.user?.permissions);
  const roleName = useAuthStore((state) => state.user?.roleName);
  return useMemo(
    () => canEditPermission(permissions, objectName, roleName),
    [objectName, permissions, roleName],
  );
}
