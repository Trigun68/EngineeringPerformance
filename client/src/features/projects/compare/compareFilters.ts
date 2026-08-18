import type {
  VersionCompareDetailRow,
  VersionCompareFacilityRow,
  VersionCompareRow,
} from '../../../api/analyticsApi';
import {
  getComparePlannedLaborHours,
  type CompareScenario,
} from './compareColumns';

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

const COMPARE_PLAN_HOUR_SCENARIOS: CompareScenario[] = ['pessimistic', 'base', 'optimistic'];

function planLaborHoursIdentical(row: VersionCompareRow): boolean {
  return COMPARE_PLAN_HOUR_SCENARIOS.every((scenario) =>
    round2(getComparePlannedLaborHours(row, scenario, 'A'))
    === round2(getComparePlannedLaborHours(row, scenario, 'B')));
}

/**
 * Итого: FactZ + PlanZ + любая из трёх пар плановых часов (песс./баз./опт.).
 * Паритет docs/features/version-comparison.md.
 */
export function hasSummaryDifference(row: VersionCompareRow): boolean {
  const factZIdentical = round4(row.factZScoreA) === round4(row.factZScoreB);
  const planZIdentical = round4(row.planZScoreA) === round4(row.planZScoreB);
  return !(factZIdentical && planZIdentical && planLaborHoursIdentical(row));
}

/**
 * По объектам: факт.часы + Z. Оба hours null → идентично (скрыть).
 * Плановые метрики / ФО не участвуют.
 */
export function hasFacilityDifference(row: VersionCompareFacilityRow): boolean {
  if (row.factLaborHoursA == null && row.factLaborHoursB == null) {
    return false;
  }
  const hoursIdentical = round2(row.factLaborHoursA) === round2(row.factLaborHoursB);
  const zIdentical = round4(row.zScoreA) === round4(row.zScoreB);
  return !(hoursIdentical && zIdentical);
}

/**
 * Детально: qty + факт.часы + факт.произв.; presence ≠ A+B всегда видимо.
 * Плановые метрики и Z не участвуют.
 */
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
  _scenario: CompareScenario = 'base',
): VersionCompareRow[] {
  if (!showOnlyDifferences) return rows;

  const byId = new Map(rows.filter((r) => !r.isTotal).map((r) => [r.workDirectionId, r]));
  const visible = new Set<number>();

  for (const row of rows) {
    if (row.isTotal) continue;
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

  return rows.filter(
    (r) =>
      visible.has(r.workDirectionId)
      || (Boolean(r.isTotal) && r.level === -1 && visible.size > 0),
  );
}

export function filterCompareFacilityRows(
  rows: VersionCompareFacilityRow[],
  showOnlyDifferences: boolean,
  _scenario: CompareScenario = 'base',
): VersionCompareFacilityRow[] {
  if (!showOnlyDifferences) return rows;
  return rows.filter((row) => hasFacilityDifference(row));
}

export function filterCompareDetailRows(
  rows: VersionCompareDetailRow[],
  showOnlyDifferences: boolean,
  _scenario: CompareScenario = 'base',
): VersionCompareDetailRow[] {
  if (!showOnlyDifferences) return rows;
  return rows.filter((row) => hasDetailDifference(row));
}
