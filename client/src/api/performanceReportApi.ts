import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';

export interface PerformanceReportDto {
  project: {
    id: number;
    code: string;
    name: string;
    gip: string | null;
    customer: string | null;
    workStatus: string | null;
    type: string | null;
    planVersionCount: number;
    factVersionCount: number;
  };
  plan: PerformanceReportVersionBlock | null;
  fact: PerformanceReportVersionBlock | null;
  versionHistory: PerformanceReportHistoryPoint[];
  accuracy: {
    averagePercent: number | null;
    items: Array<{
      characteristicName: string;
      optionName: string | null;
      valuePercent: number | null;
    }>;
  };
  insights: {
    items: Array<{ kind: string; title: string; detail: string }>;
    topNegative: PerformanceReportKeyDiscipline[];
    topPositive: PerformanceReportKeyDiscipline[];
  };
  directions: PerformanceReportMergedDirectionRow[];
  normSourcesUsed: number;
}

export interface PerformanceReportVersionBlock {
  versionId: number;
  versionName: string;
  createdAt: string;
  direction: number;
  weightedZScore: number | null;
  weightedZScorePercentile: string;
  weightedZScoreInterpretation: string | null;
  totalLaborHours: number | null;
  unallocatedLaborHours: number | null;
  physicalVolumeTotalsByUnit: Array<{ unitName: string; totalVolume: number }>;
  directions: PerformanceReportDirectionRow[];
}

export interface PerformanceReportDirectionRow {
  workDirectionId: number;
  parentId: number | null;
  level: number;
  code: string;
  name: string;
  unitName: string | null;
  factPhysicalVolume: number | null;
  factLaborHours: number | null;
  subtreeHours: number | null;
  subtreePhysicalVolume?: number | null;
  factProductivity: number | null;
  normProductivity: number | null;
  normStdDev: number | null;
  zScore: number | null;
  interpretation: string | null;
}

export interface PerformanceReportMergedDirectionRow {
  workDirectionId: number;
  parentId: number | null;
  level: number;
  code: string;
  name: string;
  unitName: string | null;
  physicalVolume: number | null;
  planLaborHours: number | null;
  factLaborHours: number | null;
  planSubtreeHours: number | null;
  factSubtreeHours: number | null;
  planProductivity: number | null;
  factProductivity: number | null;
  normProductivity: number | null;
  normStdDev: number | null;
  planZScore: number | null;
  factZScore: number | null;
  planInterpretation: string | null;
  factInterpretation: string | null;
  subtreePhysicalVolume?: number | null;
}

export interface PerformanceReportHistoryPoint {
  versionId: number;
  versionName: string;
  createdAt: string;
  direction: number;
  weightedZScore: number | null;
  totalLaborHours: number | null;
  normSnapshotKey?: string | null;
  optimisticZScore?: number | null;
  optimisticLaborHours?: number | null;
  baseZScore?: number | null;
  baseLaborHours?: number | null;
  pessimisticZScore?: number | null;
  pessimisticLaborHours?: number | null;
}

export interface PerformanceReportKeyDiscipline {
  workDirectionId: number;
  name: string;
  code: string;
  zScore: number | null;
  subtreeHours: number;
  influenceScore: number;
  physicalVolume: number | null;
}

export async function fetchPerformanceReport(projectId: number): Promise<PerformanceReportDto> {
  const { data } = await apiClient.get<PerformanceReportDto>(
    `/projects/${projectId}/performance-report`,
  );
  return data;
}

export async function exportPerformanceReportPdf(projectId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/performance-report/export.pdf`,
  );
  triggerBrowserDownload(blob, filename);
}
