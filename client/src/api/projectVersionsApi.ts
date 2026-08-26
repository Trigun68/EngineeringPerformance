import { z } from 'zod';
import apiClient from '../services/apiClient';
import { parseApiContract } from '../services/apiContract';

export interface ProjectVersion {
  id: number;
  projectId: number;
  name: string;
  versionName: string;
  description?: string | null;
  direction: number;
  physicalVolumeTypeId?: number | null;
  sortOrder: number;
  isArchived: boolean;
  createdAt?: string;
  planningScenario?: number;
  isMain: boolean;
}

export interface ProjectVersionWriteRequest {
  versionName: string;
  description?: string | null;
  direction: number;
  physicalVolumeTypeId?: number | null;
  name?: string | null;
  planningScenario?: number | null;
  /** Дата фиксации норм IIC (CreatedAt + AnalysisVersionSnapshot). ISO date или datetime. */
  asOfDate?: string | null;
}

export interface CopyVersionRequest {
  newVersionName: string;
  description?: string | null;
  /** 1=Plan, 2=Fact. Plan→Fact копирует ФО и оборудование; трудоёмкость — отдельно. */
  targetDirection?: number | null;
}

const projectVersionSchema = z.object({
  id: z.number().int(),
  projectId: z.number().int(),
  name: z.string(),
  versionName: z.string(),
  description: z.string().nullable().optional(),
  direction: z.number().int(),
  physicalVolumeTypeId: z.number().int().nullable().optional(),
  sortOrder: z.number().int(),
  isArchived: z.boolean(),
  createdAt: z.string().optional(),
  isMain: z.boolean().optional().default(false),
});
const projectVersionListSchema = z.array(projectVersionSchema);

export async function fetchProjectVersions(projectId: number): Promise<ProjectVersion[]> {
  const { data } = await apiClient.get<ProjectVersion[]>(`/projects/${projectId}/versions`);
  return parseApiContract(`/projects/${projectId}/versions`, projectVersionListSchema, data);
}

export async function createProjectVersion(
  projectId: number,
  body: ProjectVersionWriteRequest,
): Promise<ProjectVersion> {
  const { data } = await apiClient.post<ProjectVersion>(`/projects/${projectId}/versions`, body);
  return parseApiContract(`/projects/${projectId}/versions`, projectVersionSchema, data);
}

export async function updateProjectVersion(
  projectId: number,
  versionId: number,
  body: ProjectVersionWriteRequest,
): Promise<ProjectVersion> {
  const { data } = await apiClient.put<ProjectVersion>(`/projects/${projectId}/versions/${versionId}`, body);
  return parseApiContract(`/projects/${projectId}/versions/${versionId}`, projectVersionSchema, data);
}

export async function copyProjectVersion(
  projectId: number,
  versionId: number,
  body: CopyVersionRequest,
): Promise<ProjectVersion> {
  const { data } = await apiClient.post<ProjectVersion>(
    `/projects/${projectId}/versions/${versionId}/copy`,
    body,
    { timeout: 300_000 },
  );
  return parseApiContract(`/projects/${projectId}/versions/${versionId}/copy`, projectVersionSchema, data);
}

export async function deleteProjectVersion(projectId: number, versionId: number): Promise<void> {
  await apiClient.delete(`/projects/${projectId}/versions/${versionId}`);
}

/** Делает версию основной для её направления (Plan или Fact); Forecast не поддерживается. */
export async function setMainProjectVersion(projectId: number, versionId: number): Promise<ProjectVersion> {
  const { data } = await apiClient.post<ProjectVersion>(
    `/projects/${projectId}/versions/${versionId}/set-main`,
  );
  return parseApiContract(`/projects/${projectId}/versions/${versionId}/set-main`, projectVersionSchema, data);
}

export async function recalculateLaborIntensities(
  projectId: number,
  versionId: number,
): Promise<{ updatedRows: number }> {
  const { data } = await apiClient.post<{ updatedRows: number }>(
    `/projects/${projectId}/versions/${versionId}/recalculate`,
  );
  return data;
}

export interface AggregatedLaborIntensitiesResponse {
  level: number;
  rows: AggregatedLaborIntensityRow[];
}

export interface AggregatedLaborIntensityRow {
  key: string;
  label: string;
  totalBase: number;
  totalOptimistic: number;
  totalPessimistic: number;
}

export async function fetchAggregatedLaborIntensities(
  projectId: number,
  versionId: number,
  level: 'Total' | 'ByObject' = 'Total',
): Promise<AggregatedLaborIntensitiesResponse> {
  const { data } = await apiClient.get<AggregatedLaborIntensitiesResponse>(
    `/projects/${projectId}/versions/${versionId}/aggregated-labor-intensities`,
    { params: { level: level === 'ByObject' ? 2 : 1 } },
  );
  return data;
}
