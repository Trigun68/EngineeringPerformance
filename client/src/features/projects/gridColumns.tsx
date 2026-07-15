import type { ColDef, ValueFormatterParams } from 'ag-grid-community';
import type { PerformanceRow, VersionCompareDetailRow, VersionCompareFacilityRow, VersionCompareRow } from '../../api/analyticsApi';
import type { ProjectsPageDetailRow, ProjectsPageFacilityRow, ProjectsPageForecastSummaryRow } from './types';

import { formatGridNumber } from '../../utils/formatGridNumber';
import { interpretationCellStyle } from '../../utils/interpretationStyle';

export type DictionaryFormatters = {
  getUnitName: (id: number | null | undefined) => string;
  getDirectionName: (id: number | null | undefined) => string;
};

const num0 = (v: unknown) => formatGridNumber(v, 0);

const rightAlign = { cellStyle: { textAlign: 'right' as const } };

export function createForecastSummaryColumns(): ColDef<ProjectsPageForecastSummaryRow>[] {
  return [
    {
      field: 'factPhysicalVolume',
      headerName: 'Факт ФО',
      maxWidth: 110,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'normProductivity',
      headerName: 'Норм. производит.',
      maxWidth: 130,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'baseHours',
      headerName: 'Базовый',
      maxWidth: 120,
      cellStyle: { textAlign: 'right', backgroundColor: '#e8f5e9' },
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'optimisticHours',
      headerName: 'Оптимистический',
      maxWidth: 150,
      cellStyle: { textAlign: 'right', backgroundColor: '#e3f2fd' },
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'pessimisticHours',
      headerName: 'Пессимистический',
      maxWidth: 160,
      cellStyle: { textAlign: 'right', backgroundColor: '#ffebee' },
      valueFormatter: (p) => num0(p.value),
    },
  ];
}

export function createForecastFacilityColumns(): ColDef<ProjectsPageFacilityRow>[] {
  return [
    { field: 'facilityCode', headerName: 'Шифр объекта', minWidth: 120 },
    { field: 'facilityName', headerName: 'Наименование объекта', minWidth: 200, flex: 1 },
    {
      field: 'baseHours',
      headerName: 'Базовый',
      maxWidth: 120,
      cellStyle: { textAlign: 'right', backgroundColor: '#e8f5e9' },
      valueFormatter: (p) => num0(p.value ?? p.data?.laborHours),
    },
    {
      field: 'optimisticHours',
      headerName: 'Оптимистический',
      maxWidth: 130,
      cellStyle: { textAlign: 'right', backgroundColor: '#e3f2fd' },
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'pessimisticHours',
      headerName: 'Пессимистический',
      maxWidth: 140,
      cellStyle: { textAlign: 'right', backgroundColor: '#ffebee' },
      valueFormatter: (p) => num0(p.value),
    },
  ];
}

export function createFacilityColumns(): ColDef<ProjectsPageFacilityRow>[] {
  return [
    { field: 'facilityCode', headerName: 'Код объекта', minWidth: 120 },
    { field: 'facilityName', headerName: 'Объект', minWidth: 180 },
    { field: 'laborHours', headerName: 'Часы' },
    { field: 'zScore', headerName: 'Z' },
    { field: 'interpretation', headerName: 'Интерпретация', minWidth: 140 },
  ];
}

export function createForecastDetailColumns(): ColDef<ProjectsPageDetailRow>[] {
  const groupable = { enableRowGroup: true, enablePivot: false };
  const rightGroupable = { ...rightAlign, ...groupable };
  const hourScenarioCol = (
    field: 'baseHours' | 'optimisticHours' | 'pessimisticHours',
    headerName: string,
    backgroundColor: string,
  ): ColDef<ProjectsPageDetailRow> => ({
    field,
    headerName,
    maxWidth: field === 'baseHours' ? 110 : field === 'optimisticHours' ? 130 : 140,
    cellStyle: { textAlign: 'right', backgroundColor },
    valueFormatter: (p) => num0(p.value),
    enableValue: true,
    allowedAggFuncs: ['sumNullable'],
    aggFunc: 'sumNullable',
    ...groupable,
  });
  return [
    { field: 'facilityCode', headerName: 'Шифр объекта', minWidth: 100, ...groupable },
    { field: 'facilityName', headerName: 'Наименование объекта', minWidth: 140, ...groupable },
    { field: 'bindingType', headerName: 'Тип привязки', maxWidth: 110, ...groupable },
    { field: 'documentSet', headerName: 'Комплект документов', minWidth: 130, ...groupable },
    { field: 'catalogId', headerName: 'ID по каталогу', maxWidth: 110, ...groupable },
    { field: 'tag', headerName: 'TAG', maxWidth: 90, ...groupable },
    { field: 'workId', headerName: 'ID работы', maxWidth: 100, ...groupable },
    { field: 'itemName', headerName: 'Наименование', minWidth: 140, ...groupable },
    { field: 'workComment', headerName: 'Комментарий', minWidth: 120, ...groupable },
    { field: 'estimationBasis', headerName: 'Описательная часть', minWidth: 140, ...groupable },
    { field: 'pvCode', headerName: 'Код', maxWidth: 90, ...groupable },
    { field: 'pvName', headerName: 'Наименование ФО', minWidth: 140, ...groupable },
    { field: 'isAssessment', headerName: 'Оценка', maxWidth: 80, ...groupable },
    {
      field: 'coefficient',
      headerName: 'Коэфф.',
      maxWidth: 90,
      ...rightGroupable,
      valueFormatter: (p) => num0(p.value),
    },
    { field: 'iicGroupName', headerName: 'Группа ФО IIC', minWidth: 220, flex: 1, ...groupable },
    { field: 'iicUnitName', headerName: 'Ед. изм.', maxWidth: 90, ...groupable },
    {
      field: 'normProductivity',
      headerName: 'Производит.',
      maxWidth: 110,
      ...rightGroupable,
      valueFormatter: (p) => num0(Math.max(0, Number(p.value) || 0)),
    },
    {
      field: 'normStdDev',
      headerName: 'СО',
      maxWidth: 90,
      ...rightGroupable,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'quantity',
      headerName: 'Кол-во',
      maxWidth: 90,
      ...rightGroupable,
      valueFormatter: (p) => num0(p.value),
      enableValue: true,
      allowedAggFuncs: ['sumNullable'],
      aggFunc: 'sumNullable',
    },
    hourScenarioCol('baseHours', 'Базовый', '#e8f5e9'),
    hourScenarioCol('optimisticHours', 'Оптимистический', '#e3f2fd'),
    hourScenarioCol('pessimisticHours', 'Пессимистический', '#ffebee'),
  ];
}

export function createDetailColumns(): ColDef<ProjectsPageDetailRow>[] {
  return [
    { field: 'facilityCode', headerName: 'Шифр объекта', minWidth: 100 },
    { field: 'facilityName', headerName: 'Наименование объекта', minWidth: 140 },
    { field: 'bindingType', headerName: 'Тип привязки', maxWidth: 110 },
    { field: 'documentSet', headerName: 'Комплект документов', minWidth: 130 },
    { field: 'catalogId', headerName: 'ID по каталогу', maxWidth: 110 },
    { field: 'tag', headerName: 'TAG', maxWidth: 90 },
    { field: 'workId', headerName: 'ID работы', maxWidth: 100 },
    { field: 'itemName', headerName: 'Наименование', minWidth: 140 },
    { field: 'workComment', headerName: 'Комментарий', minWidth: 120 },
    { field: 'estimationBasis', headerName: 'Описательная часть', minWidth: 140 },
    { field: 'pvCode', headerName: 'Код', maxWidth: 90 },
    { field: 'pvName', headerName: 'Наименование ФО', minWidth: 140 },
    { field: 'isAssessment', headerName: 'Оценка', maxWidth: 80 },
    {
      field: 'coefficient',
      headerName: 'Коэфф.',
      maxWidth: 90,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    { field: 'iicGroupName', headerName: 'Группа ФО IIC', minWidth: 220, flex: 1 },
    {
      field: 'normProductivity',
      headerName: 'Производит.',
      maxWidth: 110,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'normStdDev',
      headerName: 'СО',
      maxWidth: 90,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'quantity',
      headerName: 'Кол-во',
      maxWidth: 90,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'baseHours',
      headerName: 'Базовый, ч',
      maxWidth: 110,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'optimisticHours',
      headerName: 'Оптимист., ч',
      maxWidth: 110,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'pessimisticHours',
      headerName: 'Пессимист., ч',
      maxWidth: 120,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'factLaborHours',
      headerName: 'Факт часы',
      maxWidth: 100,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    { field: 'zScore', headerName: 'Z', maxWidth: 80 },
  ];
}

export function createPerformanceColumns(
  formatters: DictionaryFormatters,
): ColDef<PerformanceRow>[] {
  return [
    {
      field: 'workDirectionName',
      headerName: 'Направление',
      minWidth: 200,
      valueFormatter: (p: ValueFormatterParams<PerformanceRow>) =>
        formatters.getDirectionName(p.data?.workDirectionId) || p.value?.toString() || '',
    },
    { field: 'factPhysicalVolume', headerName: 'ФО' },
    { field: 'factLaborHours', headerName: 'Часы' },
    { field: 'factProductivity', headerName: 'Произв.' },
    { field: 'zScore', headerName: 'Z' },
    { field: 'interpretation', headerName: 'Интерпретация' },
  ];
}

export function createFactPerformanceColumns(): ColDef<PerformanceRow>[] {
  const num2 = (v: unknown) => formatGridNumber(v, 2);
  const num4 = (v: unknown) => formatGridNumber(v, 4);
  return [
    { field: 'workDirectionCode', headerName: 'Код', maxWidth: 100 },
    { field: 'normUnitName', headerName: 'Ед. изм.', maxWidth: 90 },
    {
      field: 'factLaborHours',
      headerName: 'Факт трудоёмк.',
      maxWidth: 130,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'factProductivity',
      headerName: 'Факт производит.',
      maxWidth: 130,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'normProductivity',
      headerName: 'Норм производит.',
      maxWidth: 130,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'zScore',
      headerName: 'Оценка производ.',
      maxWidth: 120,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    { field: 'interpretation', headerName: 'Интерпретация', minWidth: 160, flex: 1,
      cellStyle: (p) => interpretationCellStyle(p.value as string) },
  ];
}

export function createFactFacilityColumns(): ColDef<ProjectsPageFacilityRow>[] {
  const num2 = (v: unknown) => formatGridNumber(v, 2);
  const num4 = (v: unknown) => formatGridNumber(v, 4);
  return [
    { field: 'facilityCode', headerName: 'Код', minWidth: 100 },
    { field: 'facilityName', headerName: 'Наименование', minWidth: 200, flex: 1 },
    {
      field: 'laborHours',
      headerName: 'Факт.трудоёмк., ч',
      maxWidth: 140,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'zScore',
      headerName: 'Z-score',
      maxWidth: 100,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    { field: 'interpretation', headerName: 'Интерпретация', minWidth: 160,
      cellStyle: (p) => interpretationCellStyle(p.value as string) },
  ];
}

export function createFactDetailColumns(): ColDef<ProjectsPageDetailRow>[] {
  const num2 = (v: unknown) => formatGridNumber(v, 2);
  const num4 = (v: unknown) => formatGridNumber(v, 4);
  const groupable = { enableRowGroup: true, enablePivot: false };
  const rightGroupable = { ...rightAlign, ...groupable };
  const interpretationCol = (field: 'interpretation'): ColDef<ProjectsPageDetailRow> => ({
    field,
    headerName: 'Интерпретация',
    minWidth: 160,
    ...groupable,
    cellStyle: (p) => interpretationCellStyle(p.value as string),
  });
  return [
    { field: 'facilityCode', headerName: 'Шифр объекта', minWidth: 100, ...groupable },
    { field: 'facilityName', headerName: 'Наименование объекта', minWidth: 140, ...groupable },
    { field: 'documentSet', headerName: 'Комплект документов', minWidth: 130, ...groupable },
    { field: 'pvCode', headerName: 'Код ФО', maxWidth: 90, ...groupable },
    { field: 'pvName', headerName: 'Наименование ФО', minWidth: 140, ...groupable },
    { field: 'workId', headerName: 'ID работы', maxWidth: 100, ...groupable },
    { field: 'workName', headerName: 'Наименование работы', minWidth: 140, ...groupable },
    { field: 'iicGroupName', headerName: 'Группа ФО IIC', minWidth: 200, flex: 1, ...groupable },
    { field: 'iicUnitName', headerName: 'Ед. изм.', maxWidth: 90, ...groupable },
    {
      field: 'quantity',
      headerName: 'Кол-во',
      maxWidth: 90,
      ...rightGroupable,
      valueFormatter: (p) => num2(p.value),
      enableValue: true,
      allowedAggFuncs: ['sumNullable'],
      aggFunc: 'sumNullable',
    },
    {
      field: 'factLaborHours',
      headerName: 'Факт.трудоёмк., ч',
      maxWidth: 130,
      ...rightGroupable,
      valueFormatter: (p) => num2(p.value),
      enableValue: true,
      allowedAggFuncs: ['sumNullable'],
      aggFunc: 'sumNullable',
    },
    {
      field: 'factProductivity',
      headerName: 'Факт производит.',
      maxWidth: 130,
      ...rightGroupable,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'normProductivity',
      headerName: 'Норм производит.',
      maxWidth: 130,
      ...rightGroupable,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'zScore',
      headerName: 'Z-score',
      maxWidth: 100,
      ...rightGroupable,
      valueFormatter: (p) => num4(p.value),
    },
    interpretationCol('interpretation'),
  ];
}

export function createCompareSummaryColumns(
  versionAName = 'Версия А',
  versionBName = 'Версия Б',
): ColDef<VersionCompareRow>[] {
  const num4 = (v: unknown) => formatGridNumber(v, 4);
  return [
    {
      field: 'zScoreA',
      headerName: versionAName,
      maxWidth: 120,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'interpretationA',
      headerName: 'Оценка А',
      minWidth: 120,
      cellStyle: (p) => interpretationCellStyle(p.value as string),
    },
    {
      field: 'zScoreB',
      headerName: versionBName,
      maxWidth: 120,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'interpretationB',
      headerName: 'Оценка Б',
      minWidth: 120,
      cellStyle: (p) => interpretationCellStyle(p.value as string),
    },
    {
      field: 'zDelta',
      headerName: 'Δ Z-score',
      maxWidth: 110,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
  ];
}

export function createCompareFacilityColumns(
  versionAName = 'Версия А',
  versionBName = 'Версия Б',
): ColDef<VersionCompareFacilityRow>[] {
  const num2 = (v: unknown) => formatGridNumber(v, 2);
  const num4 = (v: unknown) => formatGridNumber(v, 4);
  return [
    { field: 'facilityCode', headerName: 'Код', minWidth: 100 },
    { field: 'facilityName', headerName: 'Наименование', minWidth: 180, flex: 1 },
    {
      field: 'factLaborHoursA',
      headerName: `${versionAName}, ч`,
      maxWidth: 120,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'factLaborHoursB',
      headerName: `${versionBName}, ч`,
      maxWidth: 120,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'hoursDelta',
      headerName: 'Δ часы',
      maxWidth: 100,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'zScoreA',
      headerName: 'Z (A)',
      maxWidth: 90,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'zScoreB',
      headerName: 'Z (B)',
      maxWidth: 90,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'zDelta',
      headerName: 'Δ Z',
      maxWidth: 90,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    { field: 'interpretationA', headerName: 'Оценка А', minWidth: 110,
      cellStyle: (p) => interpretationCellStyle(p.value as string) },
    { field: 'interpretationB', headerName: 'Оценка Б', minWidth: 110,
      cellStyle: (p) => interpretationCellStyle(p.value as string) },
  ];
}

export function createCompareDetailColumns(): ColDef<VersionCompareDetailRow>[] {
  const num2 = (v: unknown) => formatGridNumber(v, 2);
  const num4 = (v: unknown) => formatGridNumber(v, 4);
  return [
    { field: 'facilityCode', headerName: 'Шифр объекта', minWidth: 100 },
    { field: 'facilityName', headerName: 'Наименование объекта', minWidth: 140 },
    { field: 'documentSet', headerName: 'Комплект документов', minWidth: 120 },
    { field: 'pvCode', headerName: 'Код ФО', maxWidth: 90 },
    { field: 'pvName', headerName: 'Наименование ФО', minWidth: 140 },
    { field: 'workId', headerName: 'ID работы', maxWidth: 100 },
    { field: 'workName', headerName: 'Наименование работы', minWidth: 140 },
    {
      field: 'quantityA',
      headerName: 'Кол-во A',
      maxWidth: 90,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'quantityB',
      headerName: 'Кол-во B',
      maxWidth: 90,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'quantityDelta',
      headerName: 'Δ кол-во',
      maxWidth: 90,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'factLaborHoursA',
      headerName: 'Факт ч A',
      maxWidth: 100,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'factLaborHoursB',
      headerName: 'Факт ч B',
      maxWidth: 100,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'hoursDelta',
      headerName: 'Δ часы',
      maxWidth: 90,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'factProductivityA',
      headerName: 'Произв. A',
      maxWidth: 100,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'factProductivityB',
      headerName: 'Произв. B',
      maxWidth: 100,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'productivityDelta',
      headerName: 'Δ произв.',
      maxWidth: 100,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    { field: 'iicGroupName', headerName: 'Группа IIC', minWidth: 160 },
    { field: 'iicUnitName', headerName: 'Ед. изм.', maxWidth: 90 },
    { field: 'presence', headerName: 'Наличие', maxWidth: 100 },
  ];
}
