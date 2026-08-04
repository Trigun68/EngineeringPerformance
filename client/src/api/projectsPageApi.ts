import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';
import { buildExportFileName } from '../utils/directoryExport';
import type {
  ProjectsPageCompareGrids,
  ProjectsPageData,
  ProjectsPageFactGrids,
  ProjectsPageForecastGrids,
} from '../features/projects/types';

export async function fetchProjectsPageData(
  projectId: number,
  versionId: number,
  compareVersionId?: number | null,
): Promise<ProjectsPageData> {
  const { data } = await apiClient.get<ProjectsPageData>(
    `/projects/${projectId}/versions/${versionId}/page-data`,
    { params: { compareVersionId: compareVersionId ?? undefined } },
  );
  return data;
}

export type {
  ProjectsPageData,
  ProjectsPageForecastGrids,
  ProjectsPageFactGrids,
  ProjectsPageCompareGrids,
};

export async function exportProjectsPagePdf(
  projectId: number,
  versionId: number,
  compareVersionId?: number | null,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/pdf`,
    { compareVersionId: compareVersionId ?? undefined },
    `Project_${projectId}_v${versionId}.pdf`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportProjectsPageExcel(
  projectId: number,
  versionId: number,
  compareVersionId?: number | null,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/excel`,
    { compareVersionId: compareVersionId ?? undefined },
    `Project_${projectId}_v${versionId}.xlsx`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastFacilityExcel(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-facility/excel`,
    {},
    `ForecastFacility_${projectId}_v${versionId}.xlsx`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastFacilityPdf(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-facility/pdf`,
    {},
    `ForecastFacility_${projectId}_v${versionId}.pdf`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastDetailExcel(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-detail/excel`,
    {},
    `ForecastDetail_${projectId}_v${versionId}.xlsx`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastDetailExcelVisible(
  projectId: number,
  versionId: number,
  tabName: string,
  headers: string[],
  rows: string[][],
): Promise<void> {
  const { data } = await apiClient.post<ArrayBuffer>(
    `/projects/${projectId}/versions/${versionId}/export/forecast-detail/excel-visible`,
    { tabName, headers, rows },
    { responseType: 'arraybuffer' },
  );
  const blob = new Blob([data], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
  triggerBrowserDownload(blob, buildExportFileName(tabName || `ForecastDetail_visible_${projectId}_v${versionId}`));
}

export async function exportForecastDetailPdf(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-detail/pdf`,
    {},
    `ForecastDetail_${projectId}_v${versionId}.pdf`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastDetailSummaryTable(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-detail/summary-table`,
    {},
    `ForecastDetailSummary_${projectId}_v${versionId}.xlsx`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportFactFacilityExcel(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/fact-facility/excel`,
    {},
    `FactFacility_${projectId}_v${versionId}.xlsx`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportFactDetailExcel(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/fact-detail/excel`,
    {},
    `FactDetail_${projectId}_v${versionId}.xlsx`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportFactDetailExcelVisible(
  projectId: number,
  versionId: number,
  tabName: string,
  headers: string[],
  rows: string[][],
): Promise<void> {
  const { data } = await apiClient.post<ArrayBuffer>(
    `/projects/${projectId}/versions/${versionId}/export/fact-detail/excel-visible`,
    { tabName, headers, rows },
    { responseType: 'arraybuffer' },
  );
  const blob = new Blob([data], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
  triggerBrowserDownload(blob, buildExportFileName(tabName || `FactDetail_visible_${projectId}_v${versionId}`));
}

export async function exportFactDetailSummaryTable(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/fact-detail/summary-table`,
    {},
    `FactDetailSummary_${projectId}_v${versionId}.xlsx`,
  );
  triggerBrowserDownload(blob, filename);
}
