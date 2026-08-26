import type { PerformanceRow, VersionCompareDetailRow, VersionCompareFacilityRow, VersionCompareRow } from '../../api/analyticsApi';

export type GridTabId =
  | 'forecast-summary'
  | 'forecast-facility'
  | 'forecast-detail'
  | 'fact-performance'
  | 'fact-facility'
  | 'fact-detail'
  | 'compare-summary'
  | 'compare-facility'
  | 'compare-detail';

export interface ProjectsPageForecastSummaryRow {
  workDirectionId: number;
  displayName: string;
  normUnitName: string;
  factPhysicalVolume: number | null;
  factPhysicalVolumeDisplay?: string | null;
  normProductivity: number | null;
  baseHours: number | null;
  optimisticHours: number | null;
  pessimisticHours: number | null;
  isTotal: boolean;
  level: number;
  parentId?: number | null;
  code?: string;
  hierarchy?: string[];
}

export interface ProjectsPageFacilityRow {
  facilityCode: string;
  facilityName: string;
  laborHours: number | null;
  zScore: number | null;
  interpretation: string;
  baseHours?: number | null;
  optimisticHours?: number | null;
  pessimisticHours?: number | null;
}

export interface ProjectsPageDetailRow {
  rowId: number | null;
  classifierId?: number | null;
  facilityCode: string;
  facilityName: string;
  documentSet: string;
  catalogCode: string;
  catalogName: string;
  workId: string;
  workName: string;
  iicGroupName: string;
  iicUnitName: string;
  quantity: number;
  baseHours: number | null;
  optimisticHours: number | null;
  pessimisticHours: number | null;
  factLaborHours: number | null;
  factProductivity: number | null;
  zScore: number | null;
  interpretation: string;
  isEquipment: boolean;
  bindingType?: string;
  catalogId?: string;
  tag?: string;
  itemName?: string;
  workComment?: string;
  estimationBasis?: string;
  pvCode?: string;
  pvName?: string;
  isAssessment?: string;
  coefficient?: number | null;
  normProductivity?: number | null;
  normStdDev?: number | null;
  normId?: number | null;
  catalogVolumeId?: number | null;
  catalogEquipmentId?: number | null;
  catalogResource?: string;
}

export interface ForecastScenarioTotals {
  baseHours: number;
  optimisticHours: number;
  pessimisticHours: number;
}

export interface ProjectsPageForecastGrids {
  summary: ProjectsPageForecastSummaryRow[];
  facility: ProjectsPageFacilityRow[];
  detail: ProjectsPageDetailRow[];
  grandTotals: ForecastScenarioTotals;
}

export interface ProjectsPageFactGrids {
  performance: PerformanceRow[];
  facility: ProjectsPageFacilityRow[];
  detail: ProjectsPageDetailRow[];
}

export interface ProjectsPageCompareGrids {
  summary: VersionCompareRow[];
  facility: VersionCompareFacilityRow[];
  detail: VersionCompareDetailRow[];
}

export interface ProjectsPageData {
  forecast: ProjectsPageForecastGrids;
  fact: ProjectsPageFactGrids;
  compare: ProjectsPageCompareGrids | null;
}

export const GRID_TABS: { id: GridTabId; label: string; group: string }[] = [
  { id: 'forecast-summary', label: 'Прогноз — итого', group: 'Прогноз' },
  { id: 'forecast-facility', label: 'Прогноз — по объектам', group: 'Прогноз' },
  { id: 'forecast-detail', label: 'Прогноз — детально', group: 'Прогноз' },
  { id: 'fact-performance', label: 'Факт — итого', group: 'Факт' },
  { id: 'fact-facility', label: 'Факт — по объектам', group: 'Факт' },
  { id: 'fact-detail', label: 'Факт — детально', group: 'Факт' },
  { id: 'compare-summary', label: 'Сравнение — итого', group: 'Сравнение' },
  { id: 'compare-facility', label: 'Сравнение — по объектам', group: 'Сравнение' },
  { id: 'compare-detail', label: 'Сравнение — детально', group: 'Сравнение' },
];
