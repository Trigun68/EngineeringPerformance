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
  factProductivity: number | null;
  normProductivity: number | null;
  normStdDev: number | null;
  zScore: number | null;
  interpretation: string | null;
}

export interface PerformanceReportHistoryPoint {
  versionId: number;
  versionName: string;
  createdAt: string;
  direction: number;
  weightedZScore: number | null;
  totalLaborHours: number | null;
}

export interface PerformanceReportKeyDiscipline {
  workDirectionId: number;
  name: string;
  code: string;
  zScore: number | null;
  subtreeHours: number;
  influenceScore: number;
}

export async function fetchPerformanceReport(
  projectId: number,
  planVersionId?: number | null,
  factVersionId?: number | null,
): Promise<PerformanceReportDto> {
  const { data } = await apiClient.get<PerformanceReportDto>(
    `/projects/${projectId}/performance-report`,
    {
      params: {
        planVersionId: planVersionId ?? undefined,
        factVersionId: factVersionId ?? undefined,
      },
    },
  );
  return data;
}

export async function exportPerformanceReportPdf(
  projectId: number,
  planVersionId?: number | null,
  factVersionId?: number | null,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/performance-report/export.pdf`,
    {
      planVersionId: planVersionId ?? undefined,
      factVersionId: factVersionId ?? undefined,
    },
    `PerformanceReport_${projectId}.pdf`,
  );
  triggerBrowserDownload(blob, filename);
}
