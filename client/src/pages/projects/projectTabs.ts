import { canReadPermission } from '../../utils/permissionAccess';
import type { PermissionObjectName } from '../../constants/accessControlConstants';
import type { AccessLevel } from '../../types/auth';

export type ProjectTabId =
  | 'versions'
  | 'data'
  | 'forecast'
  | 'analysis'
  | 'compare';

export interface ProjectTabConfig {
  id: ProjectTabId;
  label: string;
  permission: PermissionObjectName;
}

export const PROJECT_TAB_CONFIG: ProjectTabConfig[] = [
  { id: 'versions', label: 'Версии', permission: 'ProjectAnalysisVersions' },
  { id: 'data', label: 'Данные проекта', permission: 'ProjectPhysicalVolumes' },
  { id: 'forecast', label: 'Прогнозирование', permission: 'Planning' },
  { id: 'analysis', label: 'Анализ факта', permission: 'Assessment' },
  { id: 'compare', label: 'Сравнение версий', permission: 'VersionComparison' },
];

export function getVisibleTabs(
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined
): ProjectTabConfig[] {
  return PROJECT_TAB_CONFIG.filter((tab) =>
    canReadPermission(permissions, tab.permission, roleName)
  );
}

export function getDefaultTab(
  permissions: Record<string, AccessLevel> | undefined,
  roleName: string | undefined
): ProjectTabId {
  const visible = getVisibleTabs(permissions, roleName);
  return visible[0]?.id ?? 'versions';
}
