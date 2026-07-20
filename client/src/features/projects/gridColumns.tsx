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
      width: 110,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'normProductivity',
      headerName: 'Норм. производит.',
      width: 130,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'baseHours',
      headerName: 'Базовый',
      width: 120,
      cellStyle: { textAlign: 'right', backgroundColor: '#e8f5e9' },
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'optimisticHours',
      headerName: 'Оптимистический',
      width: 150,
      cellStyle: { textAlign: 'right', backgroundColor: '#e3f2fd' },
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'pessimisticHours',
      headerName: 'Пессимистический',
      width: 160,
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
      width: 120,
      cellStyle: { textAlign: 'right', backgroundColor: '#e8f5e9' },
      valueFormatter: (p) => num0(p.value ?? p.data?.laborHours),
    },
    {
      field: 'optimisticHours',
      headerName: 'Оптимистический',
      width: 130,
      cellStyle: { textAlign: 'right', backgroundColor: '#e3f2fd' },
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'pessimisticHours',
      headerName: 'Пессимистический',
      width: 140,
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
    width: field === 'baseHours' ? 110 : field === 'optimisticHours' ? 130 : 140,
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
      valueFormatter: (p) => num0(p.value),
    },
    { field: 'iicGroupName', headerName: 'Группа ФО IIC', minWidth: 220, flex: 1, ...groupable },
    { field: 'iicUnitName', headerName: 'Ед. изм.', width: 90, ...groupable },
    {
      field: 'normProductivity',
      headerName: 'Производит.',
      width: 110,
      ...rightGroupable,
      valueFormatter: (p) => num0(Math.max(0, Number(p.value) || 0)),
    },
    {
      field: 'normStdDev',
      headerName: 'СО',
      width: 90,
      ...rightGroupable,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'quantity',
      headerName: 'Кол-во',
      width: 90,
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
    { field: 'bindingType', headerName: 'Тип привязки', width: 110 },
    { field: 'documentSet', headerName: 'Комплект документов', minWidth: 130 },
    { field: 'catalogId', headerName: 'ID по каталогу', width: 110 },
    { field: 'tag', headerName: 'TAG', width: 90 },
    { field: 'workId', headerName: 'ID работы', width: 100 },
    { field: 'itemName', headerName: 'Наименование', minWidth: 140 },
    { field: 'workComment', headerName: 'Комментарий', minWidth: 120 },
    { field: 'estimationBasis', headerName: 'Описательная часть', minWidth: 140 },
    { field: 'pvCode', headerName: 'Код', width: 90 },
    { field: 'pvName', headerName: 'Наименование ФО', minWidth: 140 },
    { field: 'isAssessment', headerName: 'Оценка', width: 80 },
    {
      field: 'coefficient',
      headerName: 'Коэфф.',
      width: 90,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    { field: 'iicGroupName', headerName: 'Группа ФО IIC', minWidth: 220, flex: 1 },
    {
      field: 'normProductivity',
      headerName: 'Производит.',
      width: 110,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'normStdDev',
      headerName: 'СО',
      width: 90,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'quantity',
      headerName: 'Кол-во',
      width: 90,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'baseHours',
      headerName: 'Базовый, ч',
      width: 110,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'optimisticHours',
      headerName: 'Оптимист., ч',
      width: 110,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'pessimisticHours',
      headerName: 'Пессимист., ч',
      width: 120,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    {
      field: 'factLaborHours',
      headerName: 'Факт часы',
      width: 100,
      ...rightAlign,
      valueFormatter: (p) => num0(p.value),
    },
    { field: 'zScore', headerName: 'Z', width: 80 },
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
    { field: 'workDirectionCode', headerName: 'Код', width: 100 },
    { field: 'normUnitName', headerName: 'Ед. изм.', width: 90 },
    {
      field: 'factLaborHours',
      headerName: 'Факт трудоёмк.',
      width: 130,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'factProductivity',
      headerName: 'Факт производит.',
      width: 130,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'normProductivity',
      headerName: 'Норм производит.',
      width: 130,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'zScore',
      headerName: 'Оценка производ.',
      width: 120,
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
      width: 140,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'zScore',
      headerName: 'Z-score',
      width: 100,
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
    { field: 'pvCode', headerName: 'Код ФО', width: 90, ...groupable },
    { field: 'pvName', headerName: 'Наименование ФО', minWidth: 140, ...groupable },
    { field: 'workId', headerName: 'ID работы', width: 100, ...groupable },
    { field: 'workName', headerName: 'Наименование работы', minWidth: 140, ...groupable },
    { field: 'iicGroupName', headerName: 'Группа ФО IIC', minWidth: 200, flex: 1, ...groupable },
    { field: 'iicUnitName', headerName: 'Ед. изм.', width: 90, ...groupable },
    {
      field: 'quantity',
      headerName: 'Кол-во',
      width: 90,
      ...rightGroupable,
      valueFormatter: (p) => num2(p.value),
      enableValue: true,
      allowedAggFuncs: ['sumNullable'],
      aggFunc: 'sumNullable',
    },
    {
      field: 'factLaborHours',
      headerName: 'Факт.трудоёмк., ч',
      width: 130,
      ...rightGroupable,
      valueFormatter: (p) => num2(p.value),
      enableValue: true,
      allowedAggFuncs: ['sumNullable'],
      aggFunc: 'sumNullable',
    },
    {
      field: 'factProductivity',
      headerName: 'Факт производит.',
      width: 130,
      ...rightGroupable,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'normProductivity',
      headerName: 'Норм производит.',
      width: 130,
      ...rightGroupable,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'zScore',
      headerName: 'Z-score',
      width: 100,
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
      width: 120,
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
      width: 120,
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
      width: 110,
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
      width: 120,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'factLaborHoursB',
      headerName: `${versionBName}, ч`,
      width: 120,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'hoursDelta',
      headerName: 'Δ часы',
      width: 100,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'zScoreA',
      headerName: 'Z (A)',
      width: 90,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'zScoreB',
      headerName: 'Z (B)',
      width: 90,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'zDelta',
      headerName: 'Δ Z',
      width: 90,
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
    { field: 'pvCode', headerName: 'Код ФО', width: 90 },
    { field: 'pvName', headerName: 'Наименование ФО', minWidth: 140 },
    { field: 'workId', headerName: 'ID работы', width: 100 },
    { field: 'workName', headerName: 'Наименование работы', minWidth: 140 },
    {
      field: 'quantityA',
      headerName: 'Кол-во A',
      width: 90,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'quantityB',
      headerName: 'Кол-во B',
      width: 90,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'quantityDelta',
      headerName: 'Δ кол-во',
      width: 90,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'factLaborHoursA',
      headerName: 'Факт ч A',
      width: 100,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'factLaborHoursB',
      headerName: 'Факт ч B',
      width: 100,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'hoursDelta',
      headerName: 'Δ часы',
      width: 90,
      ...rightAlign,
      valueFormatter: (p) => num2(p.value),
    },
    {
      field: 'factProductivityA',
      headerName: 'Произв. A',
      width: 100,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'factProductivityB',
      headerName: 'Произв. B',
      width: 100,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    {
      field: 'productivityDelta',
      headerName: 'Δ произв.',
      width: 100,
      ...rightAlign,
      valueFormatter: (p) => num4(p.value),
    },
    { field: 'iicGroupName', headerName: 'Группа IIC', minWidth: 160 },
    { field: 'iicUnitName', headerName: 'Ед. изм.', width: 90 },
    { field: 'presence', headerName: 'Наличие', width: 100 },
  ];
}
