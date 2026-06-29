import { z } from 'zod';
import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';
import { parseApiContract } from '../services/apiContract';
import type { Project, ProjectFormData } from '../types/project';

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
  versionCount: z.number().int(),
});
const projectListSchema = z.array(projectSchema);

export async function fetchProjects(): Promise<Project[]> {
  const { data } = await apiClient.get<Project[]>('/projects');
  return parseApiContract('/projects', projectListSchema, data);
}

export async function createProject(payload: ProjectFormData): Promise<Project> {
  const { data } = await apiClient.post<Project>('/projects', payload);
  return parseApiContract('/projects', projectSchema, data);
}

export async function updateProject(id: number, payload: ProjectFormData): Promise<Project> {
  const { data } = await apiClient.put<Project>(`/projects/${id}`, payload);
  return parseApiContract(`/projects/${id}`, projectSchema, data);
}

export async function deleteProject(id: number): Promise<void> {
  await apiClient.delete(`/projects/${id}`);
}

export async function exportProjectsExcel(): Promise<void> {
  const { blob, filename } = await downloadBlob('/projects/export/excel', undefined, 'projects.xlsx');
  triggerBrowserDownload(blob, filename);
}

export async function exportProjectsPdf(): Promise<void> {
  const { blob, filename } = await downloadBlob('/projects/export/pdf', undefined, 'projects.pdf');
  triggerBrowserDownload(blob, filename);
}

export async function importProjectsExcel(file: File): Promise<void> {
  const formData = new FormData();
  formData.append('file', file);
  await apiClient.post('/projects/import/excel', formData, {
    headers: { 'Content-Type': 'multipart/form-data' },
  });
}
