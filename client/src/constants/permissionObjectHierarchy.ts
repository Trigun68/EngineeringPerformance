import type { MatrixAccessLevel, PermissionMatrix, PermissionMatrixObjectNode } from '../api/permissionsMatrixApi';
import type { AccessLevel } from '../types/auth';
import { toRussianObjectName } from './accessControlConstants';
import { canReadPermission } from '../utils/permissionAccess';

export const PROJECTS_GROUP_ID = 'ProjectsGroup';

/** Порядок объектов в матрице — зеркало backend PermissionMatrixHierarchy. */
export const PERMISSION_MATRIX_NODES: readonly PermissionMatrixObjectNode[] = [
  { objectName: 'Dashboard', displayName: 'Главная', parentGroup: null, level: 0, hierarchyPath: ['Dashboard'], isGroupHeader: false },
  { objectName: PROJECTS_GROUP_ID, displayName: 'Проекты', parentGroup: null, level: 0, hierarchyPath: [PROJECTS_GROUP_ID], isGroupHeader: true },
  { objectName: 'Projects', displayName: 'Проекты', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'Projects'], isGroupHeader: false },
  { objectName: 'Assessment', displayName: 'Оценка', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'Assessment'], isGroupHeader: false },
  { objectName: 'Planning', displayName: 'Планирование', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'Planning'], isGroupHeader: false },
  { objectName: 'VersionComparison', displayName: 'Сравнение версий', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'VersionComparison'], isGroupHeader: false },
  { objectName: 'Reports', displayName: 'Отчёты', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'Reports'], isGroupHeader: false },
  { objectName: 'GroupAnalysis', displayName: 'Анализ по группам', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'GroupAnalysis'], isGroupHeader: false },
  { objectName: 'GroupTrend', displayName: 'Тренд по группам', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'GroupTrend'], isGroupHeader: false },
  { objectName: 'ProjectAnalysisVersions', displayName: 'Версии анализа проекта', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'ProjectAnalysisVersions'], isGroupHeader: false },
  { objectName: 'ProjectPhysicalVolumes', displayName: 'Физ. объёмы по проекту', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'ProjectPhysicalVolumes'], isGroupHeader: false },
  { objectName: 'ProjectEquipments', displayName: 'Оборудование по проекту', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'ProjectEquipments'], isGroupHeader: false },
  { objectName: 'ProjectLaborIntensities', displayName: 'Трудоёмкость по проекту', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'ProjectLaborIntensities'], isGroupHeader: false },
  { objectName: 'ProjectDataUpload', displayName: 'Загрузка данных проекта', parentGroup: PROJECTS_GROUP_ID, level: 1, hierarchyPath: [PROJECTS_GROUP_ID, 'ProjectDataUpload'], isGroupHeader: false },
  { objectName: 'References', displayName: 'Справочники', parentGroup: null, level: 0, hierarchyPath: ['References'], isGroupHeader: true },
  { objectName: 'PhysicalVolumeTypes', displayName: 'Виды справочников физ. объёмов', parentGroup: 'References', level: 1, hierarchyPath: ['References', 'PhysicalVolumeTypes'], isGroupHeader: false },
  { objectName: 'PhysicalVolumes', displayName: 'Номенклатура физических объёмов', parentGroup: 'References', level: 1, hierarchyPath: ['References', 'PhysicalVolumes'], isGroupHeader: false },
  { objectName: 'Equipment', displayName: 'Номенклатура оборудования', parentGroup: 'References', level: 1, hierarchyPath: ['References', 'Equipment'], isGroupHeader: false },
  { objectName: 'LaborIntensities', displayName: 'Трудоёмкость', parentGroup: 'References', level: 1, hierarchyPath: ['References', 'LaborIntensities'], isGroupHeader: false },
  { objectName: 'IicWorkDirections', displayName: 'Группы физ. объёмов (IIC)', parentGroup: 'References', level: 1, hierarchyPath: ['References', 'IicWorkDirections'], isGroupHeader: false },
  { objectName: 'IicWorkDirectionNorms', displayName: 'Нормы групп физ. объёмов (IIC)', parentGroup: 'References', level: 1, hierarchyPath: ['References', 'IicWorkDirectionNorms'], isGroupHeader: false },
  { objectName: 'EngineeringWorkCosts', displayName: 'Стоимость работ', parentGroup: 'References', level: 1, hierarchyPath: ['References', 'EngineeringWorkCosts'], isGroupHeader: false },
  { objectName: 'UnitsOfMeasurement', displayName: 'Единицы измерения', parentGroup: 'References', level: 1, hierarchyPath: ['References', 'UnitsOfMeasurement'], isGroupHeader: false },
  { objectName: 'ProjectCharacteristics', displayName: 'Характеристики проекта', parentGroup: 'References', level: 1, hierarchyPath: ['References', 'ProjectCharacteristics'], isGroupHeader: false },
  { objectName: 'SettingsPage', displayName: 'Настройки', parentGroup: null, level: 0, hierarchyPath: ['SettingsPage'], isGroupHeader: true },
  { objectName: 'Users', displayName: 'Пользователи', parentGroup: 'SettingsPage', level: 1, hierarchyPath: ['SettingsPage', 'Users'], isGroupHeader: false },
  { objectName: 'UserActionLog', displayName: 'Журнал действий', parentGroup: null, level: 0, hierarchyPath: ['UserActionLog'], isGroupHeader: false },
  { objectName: 'HelpPage', displayName: 'Справка', parentGroup: null, level: 0, hierarchyPath: ['HelpPage'], isGroupHeader: false },
] as const;

export interface PermissionMatrixRowModel {
  id: string;
  objectName: string;
  displayName: string;
  parentGroup: string | null;
  level: number;
  hierarchyPath: string[];
  isHeader: boolean;
  levels: Record<string, MatrixAccessLevel>;
}

function levelFor(matrix: PermissionMatrix, objectName: string, roleId: number): MatrixAccessLevel {
  return matrix.matrix[objectName]?.[String(roleId)] ?? 'None';
}

function resolveHierarchyNodes(matrix: PermissionMatrix): PermissionMatrixObjectNode[] {
  if (matrix.objectHierarchy?.length) {
    return matrix.objectHierarchy;
  }
  return [...PERMISSION_MATRIX_NODES];
}

function toRowId(node: PermissionMatrixObjectNode): string {
  return node.isGroupHeader ? `section:${node.objectName}` : node.objectName;
}

/**
 * Строит строки матрицы с глубокой иерархией.
 * Все объекты из иерархии отображаются — администратор должен видеть и назначать права,
 * в том числе для объектов с «Нет доступа» у всех ролей (как в WPF AccessRightsViewModel).
 */
export function buildPermissionMatrixRows(matrix: PermissionMatrix): PermissionMatrixRowModel[] {
  const nodes = resolveHierarchyNodes(matrix);
  const rows: PermissionMatrixRowModel[] = [];

  const childrenByGroup = nodes
    .filter((node) => node.parentGroup != null && !node.isGroupHeader)
    .reduce<Record<string, PermissionMatrixObjectNode[]>>((acc, node) => {
      const key = node.parentGroup as string;
      const list = acc[key] ?? [];
      list.push(node);
      acc[key] = list;
      return acc;
    }, {});

  for (const node of nodes) {
    if (node.parentGroup != null) continue;

    if (node.isGroupHeader) {
      const childNodes = childrenByGroup[node.objectName] ?? [];

      rows.push({
        id: toRowId(node),
        objectName: node.objectName,
        displayName: node.displayName || toRussianObjectName(node.objectName),
        parentGroup: null,
        level: node.level,
        hierarchyPath: [...node.hierarchyPath],
        isHeader: true,
        levels: {},
      });

      for (const child of childNodes) {
        const levels = Object.fromEntries(
          matrix.roles.map((role) => [String(role.id), levelFor(matrix, child.objectName, role.id)]),
        ) as Record<string, MatrixAccessLevel>;

        rows.push({
          id: toRowId(child),
          objectName: child.objectName,
          displayName: child.displayName || toRussianObjectName(child.objectName),
          parentGroup: child.parentGroup,
          level: child.level,
          hierarchyPath: [...child.hierarchyPath],
          isHeader: false,
          levels,
        });
      }
      continue;
    }

    const levels = Object.fromEntries(
      matrix.roles.map((role) => [String(role.id), levelFor(matrix, node.objectName, role.id)]),
    ) as Record<string, MatrixAccessLevel>;

    rows.push({
      id: toRowId(node),
      objectName: node.objectName,
      displayName: node.displayName || toRussianObjectName(node.objectName),
      parentGroup: null,
      level: node.level,
      hierarchyPath: [...node.hierarchyPath],
      isHeader: false,
      levels,
    });
  }

  return rows;
}

/** Видимость строки при свёрнутых группах. */
export function isMatrixRowVisible(
  row: PermissionMatrixRowModel,
  collapsedSections: ReadonlySet<string>,
): boolean {
  for (let i = 0; i < row.hierarchyPath.length - 1; i += 1) {
    if (collapsedSections.has(row.hierarchyPath[i])) return false;
  }
  return true;
}

/** Проверка видимости объекта по правам пользователя (None = скрыть). */
export function isObjectVisibleForUser(
  permissions: Record<string, AccessLevel> | undefined,
  objectName: string,
  roleName: string | undefined,
): boolean {
  return canReadPermission(permissions, objectName, roleName);
}

export const INDENT_PX_PER_LEVEL = 20;
