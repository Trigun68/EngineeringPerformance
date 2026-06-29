import type { ColDef, ValueFormatterParams } from 'ag-grid-community';
import type { ForecastIicVolumeRow, PerformanceRow, VersionCompareDetailRow, VersionCompareFacilityRow, VersionCompareRow } from '../../api/analyticsApi';
import type { ProjectsPageDetailRow, ProjectsPageFacilityRow } from './types';

export type DictionaryFormatters = {
  getUnitName: (id: number | null | undefined) => string;
  getDirectionName: (id: number | null | undefined) => string;
};

export function createForecastSummaryColumns(
  formatters: DictionaryFormatters,
): ColDef<ForecastIicVolumeRow>[] {
  return [
    { field: 'workDirectionId', headerName: 'ID', maxWidth: 90 },
    {
      field: 'name',
      headerName: 'Направление',
      minWidth: 200,
      valueFormatter: (p: ValueFormatterParams<ForecastIicVolumeRow>) =>
        formatters.getDirectionName(p.data?.workDirectionId) || p.value?.toString() || '',
    },
    { field: 'unitName', headerName: 'Ед. изм.', maxWidth: 110 },
    { field: 'plannedVolume', headerName: 'План. объём' },
    { field: 'baseHours', headerName: 'Базовые часы' },
    { field: 'optimisticHours', headerName: 'Оптим.' },
    { field: 'pessimisticHours', headerName: 'Пессим.' },
    { field: 'range', headerName: 'Диапазон', minWidth: 120 },
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

export function createDetailColumns(): ColDef<ProjectsPageDetailRow>[] {
  return [
    { field: 'facilityCode', headerName: 'Код', minWidth: 100 },
    { field: 'facilityName', headerName: 'Объект', minWidth: 140 },
    { field: 'catalogCode', headerName: 'Шифр' },
    { field: 'catalogName', headerName: 'Наименование', minWidth: 180 },
    { field: 'iicGroupName', headerName: 'Группа IIC', minWidth: 160 },
    { field: 'iicUnitName', headerName: 'Ед. IIC' },
    { field: 'quantity', headerName: 'Кол-во' },
    { field: 'baseHours', headerName: 'База' },
    { field: 'factLaborHours', headerName: 'Факт часы' },
    { field: 'zScore', headerName: 'Z' },
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

export function createCompareSummaryColumns(): ColDef<VersionCompareRow>[] {
  return [
    { field: 'name', headerName: 'Направление', minWidth: 200 },
    { field: 'zScoreA', headerName: 'Z (A)' },
    { field: 'zScoreB', headerName: 'Z (B)' },
    { field: 'zDelta', headerName: 'ΔZ' },
    { field: 'interpretationA', headerName: 'Инт. A' },
    { field: 'interpretationB', headerName: 'Инт. B' },
  ];
}

export function createCompareFacilityColumns(): ColDef<VersionCompareFacilityRow>[] {
  return [
    { field: 'facilityCode', headerName: 'Код' },
    { field: 'facilityName', headerName: 'Объект', minWidth: 160 },
    { field: 'factLaborHoursA', headerName: 'Часы A' },
    { field: 'factLaborHoursB', headerName: 'Часы B' },
    { field: 'zDelta', headerName: 'ΔZ' },
  ];
}

export function createCompareDetailColumns(): ColDef<VersionCompareDetailRow>[] {
  return [
    { field: 'facilityCode', headerName: 'Код' },
    { field: 'pvName', headerName: 'Позиция', minWidth: 180 },
    { field: 'quantityA', headerName: 'Кол-во A' },
    { field: 'quantityB', headerName: 'Кол-во B' },
    { field: 'presence', headerName: 'Наличие' },
  ];
}
