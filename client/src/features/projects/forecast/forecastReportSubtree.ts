import type { ProjectVersionPhysicalVolume } from '../../../api/analyticsApi';
import type { ProjectsPageForecastSummaryRow } from '../types';

type CardRow = Pick<
  ProjectsPageForecastSummaryRow,
  'workDirectionId' | 'parentId' | 'baseHours' | 'optimisticHours' | 'pessimisticHours'
>;

export interface SubtreeHours {
  baseHours: number;
  optimisticHours: number;
  pessimisticHours: number;
}

/** Id узла и всех потомков среди карточек модалки (`parentId` → `workDirectionId`). */
export function collectSubtreeDirectionIds(cardRows: CardRow[], rootId: number): number[] {
  const childrenByParent = new Map<number, number[]>();
  for (const row of cardRows) {
    if (row.parentId == null) continue;
    const list = childrenByParent.get(row.parentId);
    if (list) list.push(row.workDirectionId);
    else childrenByParent.set(row.parentId, [row.workDirectionId]);
  }

  const ids: number[] = [];
  const visited = new Set<number>();
  const stack = [rootId];
  while (stack.length > 0) {
    const id = stack.pop()!;
    if (visited.has(id)) continue;
    visited.add(id);
    ids.push(id);
    const children = childrenByParent.get(id);
    if (!children) continue;
    for (let i = children.length - 1; i >= 0; i -= 1) {
      stack.push(children[i]);
    }
  }
  return ids;
}

/** Сумма сохранённых часов own+потомки (null как 0), те же колонки что в гриде «Итого». */
export function sumSubtreeHours(cardRows: CardRow[], subtreeIds: readonly number[]): SubtreeHours {
  const idSet = new Set(subtreeIds);
  let baseHours = 0;
  let optimisticHours = 0;
  let pessimisticHours = 0;
  for (const row of cardRows) {
    if (!idSet.has(row.workDirectionId)) continue;
    baseHours += row.baseHours ?? 0;
    optimisticHours += row.optimisticHours ?? 0;
    pessimisticHours += row.pessimisticHours ?? 0;
  }
  return { baseHours, optimisticHours, pessimisticHours };
}

export function collectSubtreeVolumes(
  byDirection: Record<string, ProjectVersionPhysicalVolume[]>,
  subtreeIds: readonly number[],
): ProjectVersionPhysicalVolume[] {
  const rows: ProjectVersionPhysicalVolume[] = [];
  for (const id of subtreeIds) {
    const chunk = byDirection[String(id)];
    if (chunk && chunk.length > 0) rows.push(...chunk);
  }
  return rows;
}

/** ФО/оборудование, отнесённые только к этому направлению (без потомков). */
export function volumesForDirection(
  byDirection: Record<string, ProjectVersionPhysicalVolume[]>,
  directionId: number,
): ProjectVersionPhysicalVolume[] {
  return byDirection[String(directionId)] ?? [];
}

export function sumVolumeTotals(rows: readonly ProjectVersionPhysicalVolume[]): number {
  let sum = 0;
  for (const row of rows) sum += row.total;
  return sum;
}
