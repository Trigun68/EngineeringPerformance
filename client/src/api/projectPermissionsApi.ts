import { z } from 'zod';
import apiClient from '../services/apiClient';
import { parseApiContract } from '../services/apiContract';

export interface ProjectPermission {
  userId: number;
  username: string;
  displayName: string;
  roleName: string;
  level: number;
}

const projectPermissionSchema = z.object({
  userId: z.number().int(),
  username: z.string(),
  displayName: z.string(),
  roleName: z.string(),
  level: z.number().int(),
});
const projectPermissionListSchema = z.array(projectPermissionSchema);

export async function fetchProjectPermissions(projectId: number): Promise<ProjectPermission[]> {
  const { data } = await apiClient.get<ProjectPermission[]>(`/projects/${projectId}/permissions`);
  return parseApiContract(`/projects/${projectId}/permissions`, projectPermissionListSchema, data);
}

export interface ProjectPermissionUpsert {
  userId: number;
  level: number;
}

export async function saveProjectPermissions(
  projectId: number,
  participants: ProjectPermissionUpsert[],
): Promise<void> {
  await apiClient.put(`/projects/${projectId}/permissions`, { participants });
}
