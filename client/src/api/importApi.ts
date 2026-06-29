import apiClient from '../services/apiClient';

export type ImportDataType = 'physical-volumes' | 'labor-intensities' | 'equipment';

export interface ImportRowError {
  rowNumber: number;
  message: string;
}

export interface ImportResult {
  importedRows: number;
  iterationId: number;
  details?: string | null;
  rowErrors?: ImportRowError[];
}

export interface ImportIteration {
  importedAt: string;
  recordCount: number;
  description?: string | null;
  sourceFileName?: string | null;
}

export interface LaborImportPreview {
  unmatchedDocumentTypes: string[];
  totalDataRows: number;
}

export async function importProjectData(
  projectId: number,
  versionId: number,
  dataType: ImportDataType,
  file: File,
  options?: { description?: string; addMissingLaborToCatalog?: boolean },
): Promise<ImportResult> {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await apiClient.post<ImportResult>(
    `/projects/${projectId}/versions/${versionId}/import/${dataType}`,
    formData,
    {
      headers: { 'Content-Type': 'multipart/form-data' },
      params: {
        description: options?.description,
        addMissingLaborToCatalog: options?.addMissingLaborToCatalog,
      },
    },
  );
  return data;
}

export async function previewLaborImport(
  projectId: number,
  versionId: number,
  file: File,
): Promise<LaborImportPreview> {
  const formData = new FormData();
  formData.append('file', file);
  const { data } = await apiClient.post<LaborImportPreview>(
    `/projects/${projectId}/versions/${versionId}/import/labor-intensities/preview`,
    formData,
    { headers: { 'Content-Type': 'multipart/form-data' } },
  );
  return data;
}

export async function fetchImportIterations(
  projectId: number,
  versionId: number,
  dataType: ImportDataType,
): Promise<ImportIteration[]> {
  const { data } = await apiClient.get<ImportIteration[]>(
    `/projects/${projectId}/versions/${versionId}/import/${dataType}/iterations`,
  );
  return data;
}

export async function rollbackImportIteration(
  projectId: number,
  versionId: number,
  dataType: ImportDataType,
  importedAt: string,
): Promise<number> {
  const { data } = await apiClient.delete<{ deletedRecords: number }>(
    `/projects/${projectId}/versions/${versionId}/import/${dataType}/iterations`,
    { params: { importedAt } },
  );
  return data.deletedRecords;
}
