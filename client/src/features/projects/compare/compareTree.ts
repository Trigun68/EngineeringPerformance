import type { VersionCompareRow } from '../../../api/analyticsApi';

export interface CompareTreeRow extends VersionCompareRow {
  hierarchy: string[];
}

export function buildCompareTreeRows(rows: VersionCompareRow[]): CompareTreeRow[] {
  const byId = new Map(rows.filter((r) => !r.isTotal).map((r) => [r.workDirectionId, r]));

  const buildPath = (id: number, visited = new Set<number>()): string[] => {
    if (visited.has(id)) return [`cycle-${id}`];
    visited.add(id);
    const row = byId.get(id);
    if (!row) return [`dir-${id}`];
    if (row.parentId == null) return [row.name];
    const parent = byId.get(row.parentId);
    if (!parent) return [row.name];
    return [...buildPath(row.parentId, visited), row.name];
  };

  return rows.map((row) => {
    if (row.isTotal) {
      return {
        ...row,
        hierarchy: [row.name],
      };
    }
    return {
      ...row,
      hierarchy: buildPath(row.workDirectionId),
    };
  });
}
