import axios from 'axios';
import { z } from 'zod';
import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';
import { parseApiContract } from '../services/apiContract';
import type { Project, ProjectFormData } from '../types/project';
import { createProjectSchema } from '../pages/projects/projectWriteSchema';

const projectSchema = z.object({
  id: z.number().int(),
  code: z.string(),
  name: z.string(),
  description: z.string().nullable().optional(),
  workNumber: z.string().nullable().optional(),
  contractNumber: z.string().nullable().optional(),
  calendarPlanNumber: z.string().nullable().optional(),
  type: z.string().nullable().optional(),
  customer: z.string().nullable().optional(),
  contractor: z.string().nullable().optional(),
  gip: z.string().nullable().optional(),
  workStatus: z.string().nullable().optional(),
  workStart: z.string().nullable().optional(),
  program: z.string().nullable().optional(),
  planVersionCount: z.number().int(),
  factVersionCount: z.number().int(),
  isArchived: z.boolean().optional(),
});
const projectListSchema = z.array(projectSchema);

function emptyToNull(value: string | null | undefined): string | null {
  if (value == null) return null;
  const trimmed = value.trim();
  return trimmed === '' ? null : trimmed;
}

/** Нормализация + Zod-валидация payload перед POST/PUT. */
export function sanitizeProjectPayload(payload: ProjectFormData): Record<string, unknown> {
  const workStart = emptyToNull(payload.workStart);
  const candidate = {
    code: payload.code.trim(),
    name: payload.name.trim(),
    workNumber: emptyToNull(payload.workNumber),
    contractNumber: emptyToNull(payload.contractNumber),
    calendarPlanNumber: emptyToNull(payload.calendarPlanNumber),
    type: emptyToNull(payload.type),
    customer: emptyToNull(payload.customer),
    contractor: emptyToNull(payload.contractor),
    gip: emptyToNull(payload.gip),
    workStatus: emptyToNull(payload.workStatus),
    workStart: workStart,
    program: emptyToNull(payload.program),
  };
  createProjectSchema.pick({ code: true, name: true }).parse(candidate);
  return candidate;
}

export async function fetchProjects(includeArchived = false): Promise<Project[]> {
  const { data } = await apiClient.get<Project[]>('/projects', {
    params: includeArchived ? { includeArchived: true } : undefined,
  });
  return parseApiContract('/projects', projectListSchema, data);
}

export async function fetchProjectById(id: number): Promise<Project | null> {
  try {
    const { data } = await apiClient.get<Project>(`/projects/${id}`);
    return parseApiContract(`/projects/${id}`, projectSchema, data);
  } catch (err) {
    if (axios.isAxiosError(err) && err.response?.status === 404) {
      return null;
    }
    throw err;
  }
}

export async function createProject(payload: ProjectFormData): Promise<Project> {
  const { data } = await apiClient.post<Project>('/projects', sanitizeProjectPayload(payload));
  return parseApiContract('/projects', projectSchema, data);
}

export async function updateProject(id: number, payload: ProjectFormData): Promise<Project> {
  const { data } = await apiClient.put<Project>(`/projects/${id}`, sanitizeProjectPayload(payload));
  return parseApiContract(`/projects/${id}`, projectSchema, data);
}

export async function archiveProject(id: number): Promise<Project> {
  const { data } = await apiClient.post<Project>(`/projects/${id}/archive`);
  return parseApiContract(`/projects/${id}/archive`, projectSchema, data);
}

export async function unarchiveProject(id: number): Promise<Project> {
  const { data } = await apiClient.post<Project>(`/projects/${id}/unarchive`);
  return parseApiContract(`/projects/${id}/unarchive`, projectSchema, data);
}

export async function deleteProject(id: number): Promise<void> {
  await apiClient.delete(`/projects/${id}`);
}

export async function exportProjectsExcel(): Promise<void> {
  const { blob, filename } = await downloadBlob('/projects/export/excel');
  triggerBrowserDownload(blob, filename);
}

export async function exportProjectsPdf(): Promise<void> {
  const { blob, filename } = await downloadBlob('/projects/export/pdf');
  triggerBrowserDownload(blob, filename);
}

/** Импорт списка проектов из Excel пока не реализован (API 501). Не экспортируем helper. */
