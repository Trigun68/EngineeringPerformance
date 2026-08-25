import { downloadBlob, triggerBrowserDownload } from '../services/apiClient';

export type ComparePdfSection = 'summary' | 'facility' | 'detail' | 'full';

export async function exportForecastSummaryPdf(projectId: number, versionId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast-summary/pdf`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportFactSummaryPdf(projectId: number, versionId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/fact-summary/pdf`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportFactFacilityPdf(projectId: number, versionId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/fact-facility/pdf`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportFactDetailPdf(projectId: number, versionId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/fact-detail/pdf`,
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportComparePdf(
  projectId: number,
  versionA: number,
  versionB: number,
  options?: {
    section?: ComparePdfSection;
    showOnlyDifferences?: boolean;
    top?: number;
  },
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/compare/export.pdf`,
    {
      versionA,
      versionB,
      section: options?.section ?? 'full',
      showOnlyDifferences: options?.showOnlyDifferences ?? false,
      top: options?.top ?? 20,
    },
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportTrendsPdf(
  projectId: number,
  options?: { dateFrom?: string; dateTo?: string; formulaId?: string },
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    '/analytics/trends/export.pdf',
    {
      projectId,
      dateFrom: options?.dateFrom,
      dateTo: options?.dateTo,
      formulaId: options?.formulaId,
    },
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportCombinedProjectPdf(
  projectId: number,
  options: {
    planVersionId: number;
    factVersionId: number;
    compareVersionAId: number;
    compareVersionBId: number;
    formulaId?: string;
    includeAppendix?: boolean;
  },
): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/export/combined.pdf`,
    {
      planVersionId: options.planVersionId,
      factVersionId: options.factVersionId,
      compareVersionAId: options.compareVersionAId,
      compareVersionBId: options.compareVersionBId,
      formulaId: options.formulaId,
      includeAppendix: options.includeAppendix ?? false,
    },
  );
  triggerBrowserDownload(blob, filename);
}
