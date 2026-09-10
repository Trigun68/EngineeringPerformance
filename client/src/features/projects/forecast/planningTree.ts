import type { ProjectVersionPlanningParameter } from '../../../api/planningParametersApi';
import { fetchPerformance } from '../../../api/analyticsApi';

export interface PlanningTreeRow extends ProjectVersionPlanningParameter {
  depth: number;
  hasChildren: boolean;
}

export interface FactZMaps {
  zMap: Map<number, number | null>;
  hoursMap: Map<number, number | null>;
}

export function mapsFromPerformance(performance: Awaited<ReturnType<typeof fetchPerformance>>): FactZMaps {
  const zMap = new Map<number, number | null>();
  const hoursMap = new Map<number, number | null>();
  for (const row of performance) {
    if (row.isTotal) continue;
    zMap.set(row.workDirectionId, row.zScore);
    hoursMap.set(row.workDirectionId, row.factLaborHours);
  }
  return { zMap, hoursMap };
}

export function buildPlanningTreeRows(rows: ProjectVersionPlanningParameter[]): PlanningTreeRow[] {
  const parentIds = new Set(rows.map((r) => r.parentId).filter((id): id is number => id != null));
  const byParent = rows.reduce<Map<number | null, ProjectVersionPlanningParameter[]>>((map, row) => {
    const key = row.parentId;
    const list = map.get(key) ?? [];
    list.push(row);
    map.set(key, list);
    return map;
  }, new Map());

  for (const list of byParent.values()) {
    list.sort((a, b) => a.sortOrder - b.sortOrder || a.name.localeCompare(b.name, 'ru'));
  }

  const result: PlanningTreeRow[] = [];
  const walk = (parentId: number | null, depth: number) => {
    for (const row of byParent.get(parentId) ?? []) {
      result.push({
        ...row,
        depth,
        hasChildren: parentIds.has(row.iicWorkDirectionId),
      });
      walk(row.iicWorkDirectionId, depth + 1);
    }
  };
  walk(null, 0);
  return result;
}
