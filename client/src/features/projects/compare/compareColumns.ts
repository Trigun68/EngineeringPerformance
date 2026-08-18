import type { ColDef } from 'ag-grid-community';

import type {
  VersionCompareDetailRow,
  VersionCompareFacilityRow,
  VersionCompareRow,
} from '../../../api/analyticsApi';
import { formatGridNumber } from '../../../utils/formatGridNumber';

export type CompareScenario = 'base' | 'optimistic' | 'pessimistic';

export type CompareScenarioMetrics = {
  plannedLaborHoursA?: number | null;
  plannedLaborHoursB?: number | null;
  plannedLaborHoursOptimisticA?: number | null;
  plannedLaborHoursOptimisticB?: number | null;
  plannedLaborHoursPessimisticA?: number | null;
  plannedLaborHoursPessimisticB?: number | null;
  plannedProductivityA?: number | null;
  plannedProductivityB?: number | null;
  plannedProductivityOptimisticA?: number | null;
  plannedProductivityOptimisticB?: number | null;
  plannedProductivityPessimisticA?: number | null;
  plannedProductivityPessimisticB?: number | null;
};

type VersionSide = 'A' | 'B';

interface CompareMetricColumnOptions<T extends CompareScenarioMetrics> {
  getPhysicalVolume: (row: T, side: VersionSide) => unknown;
  getZScore: (row: T, side: VersionSide) => number | null | undefined;
  getZDelta: (row: T) => number | null | undefined;
  zDeltaColumnId?: string;
}

const rightAlign = { cellStyle: { textAlign: 'right' as const } };

const PLAN_HOUR_COLORS = {
  pessimistic: '#ffebee',
  base: '#e8f5e9',
  optimistic: '#e3f2fd',
} as const;

type PlanLaborField =
  | 'plannedLaborHoursPessimisticA'
  | 'plannedLaborHoursA'
  | 'plannedLaborHoursOptimisticA'
  | 'plannedLaborHoursPessimisticB'
  | 'plannedLaborHoursB'
  | 'plannedLaborHoursOptimisticB';

function comparePlanLaborHourColumns<T extends CompareScenarioMetrics>(
  extras?: Partial<ColDef<T>>,
): ColDef<T>[] {
  const num2 = (value: unknown) => formatGridNumber(value, 2);
  const defs: Array<{ field: PlanLaborField; headerName: string; color: keyof typeof PLAN_HOUR_COLORS }> = [
    { field: 'plannedLaborHoursPessimisticA', headerName: 'План.Труд. (Песс. v. А)', color: 'pessimistic' },
    { field: 'plannedLaborHoursA', headerName: 'План.Труд. (Баз. v. А)', color: 'base' },
    { field: 'plannedLaborHoursOptimisticA', headerName: 'План.Труд. (Опт. v. А)', color: 'optimistic' },
    { field: 'plannedLaborHoursPessimisticB', headerName: 'План.Труд. (Песс. v. Б)', color: 'pessimistic' },
    { field: 'plannedLaborHoursB', headerName: 'План.Труд. (Баз. v. Б)', color: 'base' },
    { field: 'plannedLaborHoursOptimisticB', headerName: 'План.Труд. (Опт. v. Б)', color: 'optimistic' },
  ];
  return defs.map(({ field, headerName, color }) => ({
    ...extras,
    colId: field,
    field,
    headerName,
    width: 150,
    cellStyle: { textAlign: 'right' as const, backgroundColor: PLAN_HOUR_COLORS[color] },
    valueFormatter: (params: { value?: unknown }) => num2(params.value),
  })) as unknown as ColDef<T>[];
}

export function getComparePlannedLaborHours<T extends CompareScenarioMetrics>(
  row: T,
  scenario: CompareScenario,
  side: VersionSide,
): number | null | undefined {
  if (scenario === 'optimistic') {
    return side === 'A' ? row.plannedLaborHoursOptimisticA : row.plannedLaborHoursOptimisticB;
  }
  if (scenario === 'pessimistic') {
    return side === 'A' ? row.plannedLaborHoursPessimisticA : row.plannedLaborHoursPessimisticB;
  }
  return side === 'A' ? row.plannedLaborHoursA : row.plannedLaborHoursB;
}

export function getComparePlannedProductivity<T extends CompareScenarioMetrics>(
  row: T,
  scenario: CompareScenario,
  side: VersionSide,
): number | null | undefined {
  if (scenario === 'optimistic') {
    return side === 'A' ? row.plannedProductivityOptimisticA : row.plannedProductivityOptimisticB;
  }
  if (scenario === 'pessimistic') {
    return side === 'A' ? row.plannedProductivityPessimisticA : row.plannedProductivityPessimisticB;
  }
  return side === 'A' ? row.plannedProductivityA : row.plannedProductivityB;
}

export function createCompareSharedMetricColumns<T extends CompareScenarioMetrics & {
  factLaborHoursA?: number | null;
  factLaborHoursB?: number | null;
  factProductivityA?: number | null;
  factProductivityB?: number | null;
}>(
  scenario: CompareScenario,
  { getPhysicalVolume, getZScore, getZDelta, zDeltaColumnId }: CompareMetricColumnOptions<T>,
  planLaborExtras?: Partial<ColDef<T>>,
): ColDef<T>[] {
  const num2 = (value: unknown) => formatGridNumber(value, 2);
  const num4 = (value: unknown) => formatGridNumber(value, 4);

  return [
    {
      headerName: 'ФО (v. А)',
      width: 120,
      ...rightAlign,
      valueGetter: (params) => params.data ? getPhysicalVolume(params.data, 'A') : null,
      valueFormatter: (params) => typeof params.value === 'string' ? params.value : num2(params.value),
    },
    {
      headerName: 'ФО (v. Б)',
      width: 120,
      ...rightAlign,
      valueGetter: (params) => params.data ? getPhysicalVolume(params.data, 'B') : null,
      valueFormatter: (params) => typeof params.value === 'string' ? params.value : num2(params.value),
    },
    ...comparePlanLaborHourColumns<T>(planLaborExtras),
    {
      headerName: 'Ф.труд. (v. А)',
      width: 120,
      ...rightAlign,
      valueGetter: (params) => params.data?.factLaborHoursA ?? null,
      valueFormatter: (params) => num2(params.value),
    },
    {
      headerName: 'Ф.труд. (v. Б)',
      width: 120,
      ...rightAlign,
      valueGetter: (params) => params.data?.factLaborHoursB ?? null,
      valueFormatter: (params) => num2(params.value),
    },
    {
      headerName: 'Пл.произв. (v. А)',
      width: 135,
      ...rightAlign,
      valueGetter: (params) => params.data ? getComparePlannedProductivity(params.data, scenario, 'A') : null,
      valueFormatter: (params) => num4(params.value),
    },
    {
      headerName: 'Пл.произв. (v. Б)',
      width: 135,
      ...rightAlign,
      valueGetter: (params) => params.data ? getComparePlannedProductivity(params.data, scenario, 'B') : null,
      valueFormatter: (params) => num4(params.value),
    },
    {
      headerName: 'Ф.произв. (v. А)',
      width: 130,
      ...rightAlign,
      valueGetter: (params) => params.data?.factProductivityA ?? null,
      valueFormatter: (params) => num4(params.value),
    },
    {
      headerName: 'Ф.произв. (v. Б)',
      width: 130,
      ...rightAlign,
      valueGetter: (params) => params.data?.factProductivityB ?? null,
      valueFormatter: (params) => num4(params.value),
    },
    {
      headerName: 'Z-оценка (v. А)',
      width: 130,
      ...rightAlign,
      valueGetter: (params) => params.data ? getZScore(params.data, 'A') : null,
      valueFormatter: (params) => num4(params.value),
    },
    {
      headerName: 'Z-оценка (v. Б)',
      width: 130,
      ...rightAlign,
      valueGetter: (params) => params.data ? getZScore(params.data, 'B') : null,
      valueFormatter: (params) => num4(params.value),
    },
    {
      colId: zDeltaColumnId,
      headerName: 'Δ Z-score',
      width: 110,
      ...rightAlign,
      valueGetter: (params) => params.data ? getZDelta(params.data) : null,
      valueFormatter: (params) => num4(params.value),
    },
  ];
}

export function createCompareSummaryColumns(scenario: CompareScenario = 'base'): ColDef<VersionCompareRow>[] {
  return createCompareSharedMetricColumns<VersionCompareRow>(scenario, {
    getPhysicalVolume: (row, side) => side === 'A'
      ? row.factPhysicalVolumeDisplayA ?? row.factPhysicalVolumeA
      : row.factPhysicalVolumeDisplayB ?? row.factPhysicalVolumeB,
    getZScore: (row, side) => side === 'A' ? row.factZScoreA : row.factZScoreB,
    getZDelta: (row) => row.factZDelta,
    zDeltaColumnId: 'factZDelta',
  });
}

export function createCompareFacilityColumns(
  scenario: CompareScenario = 'base',
): ColDef<VersionCompareFacilityRow>[] {
  return [
    { field: 'facilityCode', headerName: 'Код', minWidth: 100 },
    { field: 'facilityName', headerName: 'Наименование', minWidth: 180, flex: 1 },
    ...createCompareSharedMetricColumns<VersionCompareFacilityRow>(scenario, {
      getPhysicalVolume: (row, side) => side === 'A' ? row.factPhysicalVolumeA : row.factPhysicalVolumeB,
      getZScore: (row, side) => side === 'A' ? row.zScoreA : row.zScoreB,
      getZDelta: (row) => row.zDelta,
      zDeltaColumnId: 'zDelta',
    }),
  ];
}

export function createCompareDetailColumns(
  scenario: CompareScenario = 'base',
): ColDef<VersionCompareDetailRow>[] {
  const groupable = { enableRowGroup: true, enablePivot: false };
  const rightGroupable = { ...rightAlign, ...groupable };
  const num2 = (value: unknown) => formatGridNumber(value, 2);

  return [
    { field: 'facilityCode', headerName: 'Шифр объекта', minWidth: 100, ...groupable },
    { field: 'facilityName', headerName: 'Наименование объекта', minWidth: 140, ...groupable },
    { field: 'bindingType', headerName: 'Тип привязки', width: 110, ...groupable },
    { field: 'documentSet', headerName: 'Комплект документов', minWidth: 130, ...groupable },
    { field: 'catalogId', headerName: 'ID по каталогу', width: 110, ...groupable },
    { field: 'tag', headerName: 'TAG', width: 90, ...groupable },
    { field: 'workId', headerName: 'ID работы', width: 100, ...groupable },
    { field: 'itemName', headerName: 'Наименование', minWidth: 140, ...groupable },
    { field: 'workComment', headerName: 'Комментарий', minWidth: 120, ...groupable },
    { field: 'estimationBasis', headerName: 'Описательная часть', minWidth: 140, ...groupable },
    { field: 'pvCode', headerName: 'Код', width: 90, ...groupable },
    { field: 'pvName', headerName: 'Наименование ФО', minWidth: 140, ...groupable },
    { field: 'isAssessment', headerName: 'Оценка', width: 80, ...groupable },
    {
      field: 'coefficient',
      headerName: 'Коэфф.',
      width: 90,
      ...rightGroupable,
      valueFormatter: (params) => num2(params.value),
    },
    { field: 'iicGroupName', headerName: 'Группа ФО IIC', minWidth: 220, flex: 1, ...groupable },
    { field: 'iicUnitName', headerName: 'Ед. изм.', width: 90, ...groupable },
    {
      field: 'normProductivity',
      headerName: 'Производит.',
      width: 110,
      ...rightGroupable,
      valueFormatter: (params) => num2(params.value),
    },
    {
      field: 'normStdDev',
      headerName: 'СО',
      width: 90,
      ...rightGroupable,
      valueFormatter: (params) => num2(params.value),
    },
    { field: 'isEquipment', headerName: 'Оборудование', width: 130, ...groupable },
    ...createCompareSharedMetricColumns<VersionCompareDetailRow>(scenario, {
      getPhysicalVolume: (row, side) => side === 'A' ? row.quantityA : row.quantityB,
      getZScore: (row, side) => side === 'A' ? row.zScoreA : row.zScoreB,
      getZDelta: (row) => row.zDelta,
      zDeltaColumnId: 'zDelta',
    }, { aggFunc: 'sumNullable' }),
    { field: 'presence', headerName: 'Наличие', width: 100, ...groupable },
  ];
}
