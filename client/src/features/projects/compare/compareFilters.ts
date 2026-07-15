import type {
  VersionCompareDetailRow,
  VersionCompareFacilityRow,
  VersionCompareRow,
} from '../../../api/analyticsApi';

/** Значение presence при наличии строки в обеих версиях (паритет с VersionCompareMerge). */
export const COMPARE_PRESENCE_BOTH = 'A+B';

export function isPresentInBothVersions(presence: string): boolean {
  return presence === COMPARE_PRESENCE_BOTH || presence === 'Both';
}

function round4(v: number | null | undefined): number {
  return Math.round((v ?? 0) * 10000) / 10000;
}

function round2(v: number | null | undefined): number {
  return Math.round((v ?? 0) * 100) / 100;
}

export function hasSummaryDifference(row: VersionCompareRow): boolean {
  return round4(row.zScoreA) !== round4(row.zScoreB)
    || row.interpretationA !== row.interpretationB;
}

export function hasFacilityDifference(row: VersionCompareFacilityRow): boolean {
  return round2(row.hoursDelta) !== 0
    || round4(row.zDelta) !== 0
    || row.interpretationA !== row.interpretationB;
}

export function hasDetailDifference(row: VersionCompareDetailRow): boolean {
  if (!isPresentInBothVersions(row.presence)) return true;

  const quantityIdentical = round4(row.quantityA) === round4(row.quantityB);
  const hoursIdentical = round2(row.factLaborHoursA) === round2(row.factLaborHoursB);
  const prodIdentical = round4(row.factProductivityA) === round4(row.factProductivityB);

  return !(quantityIdentical && hoursIdentical && prodIdentical);
}

/** Фильтр summary с сохранением предков видимых узлов (паритет WPF FilterCompareRows). */
export function filterCompareSummaryRows(
  rows: VersionCompareRow[],
  showOnlyDifferences: boolean,
): VersionCompareRow[] {
  if (!showOnlyDifferences) return rows;

  const byId = new Map(rows.map((r) => [r.workDirectionId, r]));
  const visible = new Set<number>();

  for (const row of rows) {
    if (hasSummaryDifference(row)) {
      visible.add(row.workDirectionId);
    }
  }

  let changed = true;
  while (changed) {
    changed = false;
    for (const id of [...visible]) {
      const row = byId.get(id);
      if (row?.parentId != null && !visible.has(row.parentId)) {
        visible.add(row.parentId);
        changed = true;
      }
    }
  }

  return rows.filter((r) => visible.has(r.workDirectionId));
}

export function filterCompareFacilityRows(
  rows: VersionCompareFacilityRow[],
  showOnlyDifferences: boolean,
): VersionCompareFacilityRow[] {
  if (!showOnlyDifferences) return rows;
  return rows.filter(hasFacilityDifference);
}

export function filterCompareDetailRows(
  rows: VersionCompareDetailRow[],
  showOnlyDifferences: boolean,
): VersionCompareDetailRow[] {
  if (!showOnlyDifferences) return rows;
  return rows.filter(hasDetailDifference);
}
