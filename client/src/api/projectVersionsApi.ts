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
}

export interface ProjectVersionWriteRequest {
  versionName: string;
  description?: string | null;
  direction: number;
  physicalVolumeTypeId?: number | null;
  name?: string | null;
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

export async function copyProjectVersion(projectId: number, versionId: number): Promise<ProjectVersion> {
  const { data } = await apiClient.post<ProjectVersion>(`/projects/${projectId}/versions/${versionId}/copy`);
  return parseApiContract(`/projects/${projectId}/versions/${versionId}/copy`, projectVersionSchema, data);
}

export async function deleteProjectVersion(projectId: number, versionId: number): Promise<void> {
  await apiClient.delete(`/projects/${projectId}/versions/${versionId}`);
}
