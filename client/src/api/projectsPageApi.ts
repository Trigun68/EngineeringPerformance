import apiClient, { downloadBlob, downloadBlobPost, triggerBrowserDownload } from '../services/apiClient';
import { decodeEncodedGrid, type EncodedGrid } from './decodeEncodedGrid';
import type { VersionCompareDetailRow } from './analyticsApi';
import type {
  ProjectsPageCompareGrids,
  ProjectsPageData,
  ProjectsPageDetailRow,
  ProjectsPageFactGrids,
  ProjectsPageForecastGrids,
} from '../features/projects/types';

/** Сетевой вид ответа: гриды «Детально» приходят упакованными (ISS-019). */
interface ProjectsPageDataResponse extends Omit<ProjectsPageData, 'forecast' | 'fact' | 'compare'> {
  forecast: Omit<ProjectsPageForecastGrids, 'detail'> & { detail: EncodedGrid };
  fact: Omit<ProjectsPageFactGrids, 'detail'> & { detail: EncodedGrid };
  compare: (Omit<ProjectsPageCompareGrids, 'detail'> & { detail: EncodedGrid }) | null;
}

function decodePageData(data: ProjectsPageDataResponse): ProjectsPageData {
  return {
    ...data,
    forecast: { ...data.forecast, detail: decodeEncodedGrid<ProjectsPageDetailRow>(data.forecast.detail) },
    fact: { ...data.fact, detail: decodeEncodedGrid<ProjectsPageDetailRow>(data.fact.detail) },
    compare: data.compare
      ? { ...data.compare, detail: decodeEncodedGrid<VersionCompareDetailRow>(data.compare.detail) }
      : null,
  };
}

export async function fetchProjectsPageData(
  projectId: number,
  versionId: number,
  compareVersionId?: number | null,
  signal?: AbortSignal,
): Promise<ProjectsPageData> {
  const { data } = await apiClient.get<ProjectsPageDataResponse>(
    `/projects/${projectId}/versions/${versionId}/page-data`,
    { params: { compareVersionId: compareVersionId ?? undefined }, signal },
  );
  return decodePageData(data);
}

export async function fetchProjectsPageDetails(
  projectId: number,
  versionId: number,
  compareVersionId?: number | null,
  signal?: AbortSignal,
): Promise<{
  forecastDetail: ProjectsPageDetailRow[];
  factDetail: ProjectsPageDetailRow[];
  compareDetail: VersionCompareDetailRow[] | null;
}> {
  const { data } = await apiClient.get<{
    forecastDetail: EncodedGrid;
    factDetail: EncodedGrid;
    compareDetail: EncodedGrid | null;
  }>(`/projects/${projectId}/versions/${versionId}/page-data/detail`, {
    params: { compareVersionId: compareVersionId ?? undefined },
    signal,
  });
  return {
    forecastDetail: decodeEncodedGrid<ProjectsPageDetailRow>(data.forecastDetail),
    factDetail: decodeEncodedGrid<ProjectsPageDetailRow>(data.factDetail),
    compareDetail: data.compareDetail
      ? decodeEncodedGrid<VersionCompareDetailRow>(data.compareDetail)
      : null,
  };
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
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastSummaryExcel(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-summary/excel`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastSummaryPdf(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-summary/pdf`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastFacilityExcel(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-facility/excel`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastFacilityPdf(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-facility/pdf`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastDetailExcel(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-detail/excel`,
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
  const { blob, filename } = await downloadBlobPost(
    `/projects/${projectId}/versions/${versionId}/export/forecast-detail/excel-visible`,
    { tabName, headers, rows },
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastDetailPdf(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-detail/pdf`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastDetailSummaryTable(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-detail/summary-table`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportFactSummaryExcel(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/fact-summary/excel`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportFactFacilityExcel(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/fact-facility/excel`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportFactDetailExcel(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/fact-detail/excel`,
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
  const { blob, filename } = await downloadBlobPost(
    `/projects/${projectId}/versions/${versionId}/export/fact-detail/excel-visible`,
    { tabName, headers, rows },
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportFactDetailSummaryTable(
  projectId: number,
  versionId: number,
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/fact-detail/summary-table`,
  );
  triggerBrowserDownload(blob, filename);
}
