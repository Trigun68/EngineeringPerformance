import type { PerformanceRow } from '../../../api/analyticsApi';

export interface PerformanceTreeRow extends PerformanceRow {
  hierarchy: string[];
}

export function buildPerformanceTreeRows(rows: PerformanceRow[]): PerformanceTreeRow[] {
  const byId = new Map(
    rows.filter((r) => !r.isTotal && r.workDirectionId >= 0).map((r) => [r.workDirectionId, r]),
  );

  const buildPath = (id: number, visited = new Set<number>()): string[] => {
    if (visited.has(id)) return [`cycle-${id}`];
    visited.add(id);
    const row = byId.get(id);
    if (!row) return [`dir-${id}`];
    if (row.parentId == null) return [row.workDirectionName];
    return [...buildPath(row.parentId, visited), row.workDirectionName];
  };

  return rows.map((row) => {
    if (row.isTotal || row.workDirectionId < 0) {
      return {
        ...row,
        hierarchy: [row.workDirectionName],
      };
    }
    return {
      ...row,
      hierarchy: buildPath(row.workDirectionId),
    };
  });
}
