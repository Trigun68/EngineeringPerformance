import type { ProjectVersionPlanningParameter } from '../../../api/planningParametersApi';

/** Уровень редактирования Z (совпадает с backend ZParameterLevel). */
export type ZParameterLevel = 1 | 2 | 3 | 4;

export const Z_PARAMETER_LEVEL = {
  ByProject: 1,
  Level1: 2,
  Level2: 3,
  EachElement: 4,
} as const satisfies Record<string, ZParameterLevel>;

export const Z_PARAMETER_LEVEL_LABELS: Record<ZParameterLevel, string> = {
  1: 'По проекту',
  2: 'По элементам 1 уровня',
  3: 'По элементам 2 уровня',
  4: 'По каждому элементу',
};

export interface ZTriple {
  zPessimistic: number;
  zBase: number;
  zOptimistic: number;
}

export function buildDepthMap(rows: ProjectVersionPlanningParameter[]): Map<number, number> {
  const byId = new Map(rows.map((r) => [r.iicWorkDirectionId, r]));
  const depthCache = new Map<number, number>();

  const depthOf = (directionId: number): number => {
    const cached = depthCache.get(directionId);
    if (cached !== undefined) return cached;
    const row = byId.get(directionId);
    if (!row || row.parentId == null) {
      depthCache.set(directionId, 0);
      return 0;
    }
    const d = depthOf(row.parentId) + 1;
    depthCache.set(directionId, d);
    return d;
  };

  for (const row of rows) {
    depthOf(row.iicWorkDirectionId);
  }
  return depthCache;
}

/** Максимальная видимая глубина; null = все; -1 = только виртуальный проект. */
export function maxVisibleDepth(level: ZParameterLevel): number | null {
  switch (level) {
    case Z_PARAMETER_LEVEL.ByProject:
      return -1;
    case Z_PARAMETER_LEVEL.Level1:
      return 0;
    case Z_PARAMETER_LEVEL.Level2:
      return 1;
    case Z_PARAMETER_LEVEL.EachElement:
      return null;
    default:
      return null;
  }
}

export function isDirectionEditable(level: ZParameterLevel, depth: number): boolean {
  const max = maxVisibleDepth(level);
  if (max === -1) return false;
  if (max === null) return true;
  return depth <= max;
}

export function isProjectRowEditable(level: ZParameterLevel): boolean {
  return level === Z_PARAMETER_LEVEL.ByProject;
}

export function filterVisibleRows(
  treeRows: Array<ProjectVersionPlanningParameter & { depth: number }>,
  level: ZParameterLevel,
): Array<ProjectVersionPlanningParameter & { depth: number }> {
  const max = maxVisibleDepth(level);
  if (max === -1) return [];
  if (max === null) return treeRows;
  return treeRows.filter((r) => r.depth <= max);
}

/** Потомки directionId включая сам узел; для project — все id направлений. */
export function collectCascadeTargetIds(
  rows: ProjectVersionPlanningParameter[],
  source: 'project' | number,
): number[] {
  if (source === 'project') {
    return rows.map((r) => r.iicWorkDirectionId);
  }

  const childrenByParent = new Map<number | null, number[]>();
  for (const row of rows) {
    const key = row.parentId;
    const list = childrenByParent.get(key) ?? [];
    list.push(row.iicWorkDirectionId);
    childrenByParent.set(key, list);
  }

  const result: number[] = [];
  const walk = (id: number) => {
    result.push(id);
    for (const childId of childrenByParent.get(id) ?? []) {
      walk(childId);
    }
  };
  walk(source);
  return result;
}

export function cascadeZ(
  rows: ProjectVersionPlanningParameter[],
  source: 'project' | number,
  z: ZTriple,
): ProjectVersionPlanningParameter[] {
  const targets = new Set(collectCascadeTargetIds(rows, source));
  return rows.map((row) =>
    targets.has(row.iicWorkDirectionId)
      ? {
          ...row,
          zPessimistic: z.zPessimistic,
          zBase: z.zBase,
          zOptimistic: z.zOptimistic,
        }
      : row,
  );
}

export function leafDirectionIds(rows: ProjectVersionPlanningParameter[]): number[] {
  const parents = new Set(rows.map((r) => r.parentId).filter((id): id is number => id != null));
  return rows.filter((r) => !parents.has(r.iicWorkDirectionId)).map((r) => r.iicWorkDirectionId);
}

/**
 * Средневзвешенный Z по листьям. Если все веса 0 — среднее арифметическое листьев.
 */
export function weightedAvgZ(
  rows: ProjectVersionPlanningParameter[],
  pick: (row: ProjectVersionPlanningParameter) => number,
): number {
  const leaves = leafDirectionIds(rows);
  if (leaves.length === 0) return 0;

  const leafRows = rows.filter((r) => leaves.includes(r.iicWorkDirectionId));
  let weightedSum = 0;
  let weightTotal = 0;
  for (const row of leafRows) {
    const w = row.laborHoursWeight ?? 0;
    if (w <= 0) continue;
    weightedSum += pick(row) * w;
    weightTotal += w;
  }

  if (weightTotal > 0) {
    return weightedSum / weightTotal;
  }

  return leafRows.reduce((sum, row) => sum + pick(row), 0) / leafRows.length;
}

export function projectRollup(rows: ProjectVersionPlanningParameter[]): ZTriple {
  return {
    zPessimistic: weightedAvgZ(rows, (r) => r.zPessimistic),
    zBase: weightedAvgZ(rows, (r) => r.zBase),
    zOptimistic: weightedAvgZ(rows, (r) => r.zOptimistic),
  };
}

/**
 * Средневзвешенный Fact Z по листьям (вес — factLaborHours из оценки факта).
 * Если весов нет — среднее по листьям с известным Z.
 */
export function weightedFactZ(
  rows: ProjectVersionPlanningParameter[],
  factZByDirection: Map<number, number | null>,
  factHoursByDirection?: Map<number, number | null>,
): number | null {
  const leaves = leafDirectionIds(rows);
  if (leaves.length === 0) return null;

  const leafRows = rows.filter((r) => leaves.includes(r.iicWorkDirectionId));
  let weightedSum = 0;
  let weightTotal = 0;
  const withZ: number[] = [];

  for (const row of leafRows) {
    const z = factZByDirection.get(row.iicWorkDirectionId);
    if (z == null || !Number.isFinite(z)) continue;
    withZ.push(z);
    const w = factHoursByDirection?.get(row.iicWorkDirectionId)
      ?? row.laborHoursWeight
      ?? 0;
    if (w > 0) {
      weightedSum += z * w;
      weightTotal += w;
    }
  }

  if (weightTotal > 0) return weightedSum / weightTotal;
  if (withZ.length === 0) return null;
  return withZ.reduce((sum, z) => sum + z, 0) / withZ.length;
}
