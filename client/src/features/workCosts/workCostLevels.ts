/** Уровень задания стоимости — те же глубины, что ZParameterLevel. */
export type WorkCostLevel = 1 | 2 | 3 | 4;

export const WORK_COST_LEVEL = {
  Organization: 1,
  Level1: 2,
  Level2: 3,
  EachElement: 4,
} as const satisfies Record<string, WorkCostLevel>;

export const WORK_COST_LEVEL_LABELS: Record<WorkCostLevel, string> = {
  1: 'В целом по организации',
  2: 'По элементам 1 уровня',
  3: 'По элементам 2 уровня',
  4: 'По каждому элементу',
};

export interface WorkCostTreeNode {
  id: number;
  parentId: number | null;
  ratePerHour: number | null;
}

export function buildDepthMap(rows: WorkCostTreeNode[]): Map<number, number> {
  const byId = new Map(rows.map((r) => [r.id, r]));
  const cache = new Map<number, number>();

  const depthOf = (id: number): number => {
    const cached = cache.get(id);
    if (cached !== undefined) return cached;
    const row = byId.get(id);
    if (!row || row.parentId == null) {
      cache.set(id, 0);
      return 0;
    }
    const depth = depthOf(row.parentId) + 1;
    cache.set(id, depth);
    return depth;
  };

  for (const row of rows) depthOf(row.id);
  return cache;
}

/** Максимальная видимая глубина; null = все; -1 = только виртуальная «организация». */
export function maxVisibleDepth(level: WorkCostLevel): number | null {
  switch (level) {
    case WORK_COST_LEVEL.Organization:
      return -1;
    case WORK_COST_LEVEL.Level1:
      return 0;
    case WORK_COST_LEVEL.Level2:
      return 1;
    case WORK_COST_LEVEL.EachElement:
      return null;
    default:
      return null;
  }
}

export function isDirectionEditable(level: WorkCostLevel, depth: number): boolean {
  const max = maxVisibleDepth(level);
  if (max === -1) return false;
  if (max === null) return true;
  return depth <= max;
}

export function isOrganizationRowEditable(level: WorkCostLevel): boolean {
  return level === WORK_COST_LEVEL.Organization;
}

export function filterVisibleRows<T extends { depth: number }>(
  treeRows: T[],
  level: WorkCostLevel,
): T[] {
  const max = maxVisibleDepth(level);
  if (max === -1) return [];
  if (max === null) return treeRows;
  return treeRows.filter((r) => r.depth <= max);
}

export function collectCascadeTargetIds(
  rows: WorkCostTreeNode[],
  source: 'organization' | number,
): number[] {
  if (source === 'organization') {
    return rows.map((r) => r.id);
  }

  const childrenByParent = new Map<number | null, number[]>();
  for (const row of rows) {
    const list = childrenByParent.get(row.parentId) ?? [];
    list.push(row.id);
    childrenByParent.set(row.parentId, list);
  }

  const result: number[] = [];
  const walk = (id: number) => {
    result.push(id);
    for (const childId of childrenByParent.get(id) ?? []) walk(childId);
  };
  walk(source);
  return result;
}

export function cascadeRate(
  rows: WorkCostTreeNode[],
  source: 'organization' | number,
  rate: number,
): WorkCostTreeNode[] {
  const targets = new Set(collectCascadeTargetIds(rows, source));
  return rows.map((row) => (targets.has(row.id) ? { ...row, ratePerHour: rate } : row));
}

export function formatRateDisplay(value: number): string {
  return Number.isFinite(value) ? value.toFixed(2).replace('.', ',') : '0,00';
}

/** Парсит ставку с точкой или запятой; незавершённый ввод → null. */
export function parseRateInput(raw: string): number | null {
  const normalized = raw.trim().replace(/\s/g, '').replace(',', '.');
  if (
    normalized === ''
    || normalized === '-'
    || normalized === '.'
    || normalized === '-.'
  ) {
    return null;
  }
  const value = Number(normalized);
  return Number.isFinite(value) ? value : null;
}
