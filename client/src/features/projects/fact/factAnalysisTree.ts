import type {
  ProjectVersionDirection,
  ProjectVersionDirectionLaborRow,
  ProjectVersionPhysicalVolumeItem,
} from '../../../api/analyticsApi';
import {
  collectSubtreeDirectionIds,
  sumSubtreeHours,
  sumVolumeTotals,
} from '../forecast/forecastReportSubtree';

export interface FactCardHours {
  baseHours: number;
  optimisticHours: number;
  pessimisticHours: number;
  factLaborHours: number;
}

function asHourRows(directions: readonly ProjectVersionDirection[]) {
  return directions.map((d) => ({
    workDirectionId: d.id,
    parentId: d.parentId,
    baseHours: d.baseHours ?? null,
    optimisticHours: d.optimisticHours ?? null,
    pessimisticHours: d.pessimisticHours ?? null,
  }));
}

/** Карточки IIC в порядке дерева (родитель, затем потомки), без плейсхолдера «(пусто)». */
export function orderDirectionsForCards(
  directions: readonly ProjectVersionDirection[],
): ProjectVersionDirection[] {
  const cards = directions.filter((d) => d.id >= 0 && d.level >= 0);
  const children = new Map<number | null, ProjectVersionDirection[]>();
  for (const row of cards) {
    const key = row.parentId ?? null;
    const list = children.get(key);
    if (list) list.push(row);
    else children.set(key, [row]);
  }

  const result: ProjectVersionDirection[] = [];
  const visit = (parentId: number | null) => {
    for (const child of children.get(parentId) ?? []) {
      result.push(child);
      visit(child.id);
    }
  };
  visit(null);

  const placed = new Set(result.map((d) => d.id));
  for (const row of cards) {
    if (!placed.has(row.id)) result.push(row);
  }
  return result;
}

export function subtreeIdsForCard(
  cardRows: readonly ProjectVersionDirection[],
  rootId: number,
): number[] {
  return collectSubtreeDirectionIds(asHourRows(cardRows), rootId);
}

export function sumFactCardHours(
  cardRows: readonly ProjectVersionDirection[],
  subtreeIds: readonly number[],
): FactCardHours {
  const hours = sumSubtreeHours(asHourRows(cardRows), subtreeIds);
  const idSet = new Set(subtreeIds);
  let factLaborHours = 0;
  for (const row of cardRows) {
    if (!idSet.has(row.id)) continue;
    factLaborHours += row.factLaborHours ?? 0;
  }
  return { ...hours, factLaborHours };
}

export function collectOwnAndDescendantVolumes(
  cardRows: readonly ProjectVersionDirection[],
  subtreeIds: readonly number[],
  kind: 'physicalVolumes' | 'equipmentVolumes',
): ProjectVersionPhysicalVolumeItem[] {
  const idSet = new Set(subtreeIds);
  const rows: ProjectVersionPhysicalVolumeItem[] = [];
  for (const row of cardRows) {
    if (!idSet.has(row.id)) continue;
    const chunk = row[kind];
    if (chunk && chunk.length > 0) rows.push(...chunk);
  }
  return rows;
}

export function collectOwnAndDescendantLabor(
  cardRows: readonly ProjectVersionDirection[],
  subtreeIds: readonly number[],
): ProjectVersionDirectionLaborRow[] {
  const idSet = new Set(subtreeIds);
  const rows: ProjectVersionDirectionLaborRow[] = [];
  for (const row of cardRows) {
    if (!idSet.has(row.id)) continue;
    const chunk = row.directionLaborIntensities;
    if (chunk && chunk.length > 0) rows.push(...chunk);
  }
  return rows;
}

/** ФО/оборудование, отнесённые только к этому направлению (без потомков). */
export function ownVolumes(
  node: ProjectVersionDirection,
  kind: 'physicalVolumes' | 'equipmentVolumes',
): ProjectVersionPhysicalVolumeItem[] {
  return node[kind] ?? [];
}

/** Трудоёмкость версии, отнесённая только к этому направлению (без потомков). */
export function ownLabor(node: ProjectVersionDirection): ProjectVersionDirectionLaborRow[] {
  return node.directionLaborIntensities ?? [];
}

export function factFoForCard(
  node: ProjectVersionDirection,
  volumeRows: readonly ProjectVersionPhysicalVolumeItem[],
): number | string | null {
  if (volumeRows.length > 0) return sumVolumeTotals(volumeRows);
  return node.factPhysicalVolume;
}
