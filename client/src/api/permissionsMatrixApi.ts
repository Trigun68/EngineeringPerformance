import apiClient from '../services/apiClient';

export type MatrixAccessLevel = 'None' | 'Read' | 'Edit';

export interface PermissionMatrixRole {
  id: number;
  name: string;
}

export interface PermissionMatrixObjectNode {
  objectName: string;
  displayName: string;
  parentGroup: string | null;
  level: number;
  hierarchyPath: string[];
  isGroupHeader: boolean;
}

export interface PermissionMatrix {
  objects: string[];
  objectHierarchy: PermissionMatrixObjectNode[];
  roles: PermissionMatrixRole[];
  matrix: Record<string, Record<string, MatrixAccessLevel>>;
}

interface PermissionMatrixObjectResponse {
  objectName?: string;
  ObjectName?: string;
  displayName?: string;
  DisplayName?: string;
  parentGroup?: string | null;
  ParentGroup?: string | null;
  level?: number;
  Level?: number;
  hierarchyPath?: string[];
  HierarchyPath?: string[];
  isGroupHeader?: boolean;
  IsGroupHeader?: boolean;
}

interface PermissionMatrixResponse {
  objects?: string[];
  Objects?: string[];
  objectHierarchy?: PermissionMatrixObjectResponse[];
  ObjectHierarchy?: PermissionMatrixObjectResponse[];
  roles?: Array<{ id?: number; Id?: number; name?: string; Name?: string }>;
  Roles?: Array<{ id?: number; Id?: number; name?: string; Name?: string }>;
  matrix?: Record<string, Record<string, MatrixAccessLevel>>;
  Matrix?: Record<string, Record<string, MatrixAccessLevel>>;
}

function normalizeObjectNode(raw: PermissionMatrixObjectResponse): PermissionMatrixObjectNode {
  return {
    objectName: raw.objectName ?? raw.ObjectName ?? '',
    displayName: raw.displayName ?? raw.DisplayName ?? '',
    parentGroup: raw.parentGroup ?? raw.ParentGroup ?? null,
    level: raw.level ?? raw.Level ?? 0,
    hierarchyPath: raw.hierarchyPath ?? raw.HierarchyPath ?? [],
    isGroupHeader: raw.isGroupHeader ?? raw.IsGroupHeader ?? false,
  };
}

function normalizeMatrix(data: PermissionMatrixResponse): PermissionMatrix {
  const rolesRaw = data.roles ?? data.Roles ?? [];
  const hierarchyRaw = data.objectHierarchy ?? data.ObjectHierarchy ?? [];
  return {
    objects: data.objects ?? data.Objects ?? [],
    objectHierarchy: hierarchyRaw.map(normalizeObjectNode),
    roles: rolesRaw.map((role) => ({
      id: role.id ?? role.Id ?? 0,
      name: role.name ?? role.Name ?? '',
    })),
    matrix: data.matrix ?? data.Matrix ?? {},
  };
}

export async function fetchPermissionMatrix(): Promise<PermissionMatrix> {
  const { data } = await apiClient.get<PermissionMatrixResponse>('/permissions/matrix');
  return normalizeMatrix(data);
}

export async function updatePermissionMatrix(
  matrix: Record<string, Record<string, MatrixAccessLevel>>,
): Promise<void> {
  await apiClient.put('/permissions/matrix', { matrix });
}
