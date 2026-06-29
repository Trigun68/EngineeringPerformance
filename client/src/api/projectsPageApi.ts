import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';
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
