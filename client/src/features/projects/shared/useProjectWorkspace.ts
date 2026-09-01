import { createContext, useContext, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  fetchProjectById,
} from '@/api/projectsApi';
import { fetchProjectVersions, type ProjectVersion } from '@/api/projectVersionsApi';
import { fetchProjectPermissions, type ProjectPermission } from '@/api/projectPermissionsApi';
import { fetchProjectCharacteristics, type ProjectCharacteristicSelection } from '@/api/projectCharacteristicsApi';
import { usePermission } from '@/hooks/usePermission';
import { useAuthStore } from '@/store/useAuthStore';
import { canReadPermission } from '@/utils/permissionAccess';
import type { Project } from '@/types/project';

export interface ProjectWorkspaceData {
  project: Project | null;
  versions: ProjectVersion[];
  participants: ProjectPermission[];
  planVersionId: number | null;
  planVersionLabel: string;
  factVersionId: number | null;
  factVersionLabel: string;
  compareVersionAId: number | null;
  compareVersionBId: number | null;
  deviationPercent: number | null;
}

function computeWorkspaceData(
  project: Project,
  versions: ProjectVersion[],
  participants: ProjectPermission[],
  characteristics: ProjectCharacteristicSelection[],
): ProjectWorkspaceData {
  const planVersion =
    versions.find((v) => v.direction === 1 && v.isPrimary)
    ?? versions.find((v) => v.direction === 1)
    ?? versions[0]
    ?? null;
  const factVersion =
    versions.find((v) => v.direction === 2 && v.isPrimary)
    ?? versions.find((v) => v.direction === 2)
    ?? planVersion;

  const percents = characteristics
    .map((c) => c.selectedValuePercent)
    .filter((v): v is number => v != null);
  const deviationPercent =
    percents.length > 0
      ? Math.round((percents.reduce((a, b) => a + b, 0) / percents.length) * 100) / 100
      : null;

  return {
    project,
    versions,
    participants,
    planVersionId: planVersion?.id ?? null,
    planVersionLabel: planVersion ? `${planVersion.versionName} — ${planVersion.name}` : '',
    factVersionId: factVersion?.id ?? null,
    factVersionLabel: factVersion ? `${factVersion.versionName} — ${factVersion.name}` : '',
    compareVersionAId: planVersion?.id ?? null,
    compareVersionBId: factVersion?.id ?? null,
    deviationPercent,
  };
}

export function useProjectWorkspace(projectId: number | null, canRead: boolean) {
  const user = useAuthStore((s) => s.user);
  const permissions = user?.permissions;
  const roleName = user?.roleName;
  const { canEdit: canEditProject } = usePermission('Projects');

  const visibleWorkspaceTabs = useMemo(
    () =>
      [
        { value: 'forecast' as const, label: 'Прогнозирование', permission: 'Planning' },
        { value: 'fact' as const, label: 'Оценка факта', permission: 'Assessment' },
        { value: 'compare' as const, label: 'Сравнение версий', permission: 'VersionComparison' },
        { value: 'report' as const, label: 'Отчет', permission: 'Reports' },
      ].filter((tab) => canReadPermission(permissions, tab.permission, roleName)),
    [permissions, roleName],
  );

  const projectQuery = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => fetchProjectById(projectId!),
    enabled: !!projectId && canRead,
  });

  const versionsQuery = useQuery({
    queryKey: ['projectVersions', projectId],
    queryFn: () => fetchProjectVersions(projectId!),
    enabled: !!projectId && canRead,
  });

  const characteristicsQuery = useQuery({
    queryKey: ['projectCharacteristics', projectId],
    queryFn: () => fetchProjectCharacteristics(projectId!).catch(() => []),
    enabled: !!projectId && canRead,
  });

  const permissionsQuery = useQuery({
    queryKey: ['projectPermissions', projectId],
    queryFn: () => fetchProjectPermissions(projectId!).catch(() => [] as ProjectPermission[]),
    enabled: !!projectId && canRead,
  });

  const data = useMemo<ProjectWorkspaceData | null>(() => {
    if (!projectQuery.data) return null;
    return computeWorkspaceData(
      projectQuery.data,
      versionsQuery.data ?? [],
      permissionsQuery.data ?? [],
      characteristicsQuery.data ?? [],
    );
  }, [projectQuery.data, versionsQuery.data, permissionsQuery.data, characteristicsQuery.data]);

  const loading = projectQuery.isLoading || versionsQuery.isLoading;
  const error = projectQuery.error ?? versionsQuery.error;

  return {
    data,
    loading,
    error,
    visibleWorkspaceTabs,
    canRead,
    canEdit: canRead && canEditProject,
  };
}

export interface ProjectWorkspaceContextValue {
  data: ProjectWorkspaceData | null;
  loading: boolean;
  error: unknown;
  visibleWorkspaceTabs: ReadonlyArray<{ value: string; label: string; permission: string }>;
  canRead: boolean;
  canEdit: boolean;
}

export const ProjectWorkspaceContext = createContext<ProjectWorkspaceContextValue | null>(null);

export function useProjectWorkspaceContext(): ProjectWorkspaceContextValue {
  const ctx = useContext(ProjectWorkspaceContext);
  if (!ctx) throw new Error('useProjectWorkspaceContext must be used within ProjectWorkspaceProvider');
  return ctx;
}
