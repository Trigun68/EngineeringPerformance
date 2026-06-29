import { z } from 'zod';
import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';
import { parseApiContract } from '../services/apiContract';

export interface IicNorm {
  id: number;
  workDirectionId: number;
  workDirectionName?: string | null;
  performance?: number | null;
  estimate?: number | null;
  standardDeviation?: number | null;
  unitId?: number | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
}

const iicNormSchema = z.object({
  id: z.number().int(),
  workDirectionId: z.number().int(),
  workDirectionName: z.string().nullable().optional(),
  performance: z.number().nullable().optional(),
  estimate: z.number().nullable().optional(),
  standardDeviation: z.number().nullable().optional(),
  unitId: z.number().int().nullable().optional(),
  effectiveFrom: z.string(),
  effectiveTo: z.string().nullable().optional(),
});
const iicNormListSchema = z.array(iicNormSchema);

export async function fetchIicNorms(asOf?: string): Promise<IicNorm[]> {
  const { data } = await apiClient.get<IicNorm[]>('/iic-norms', {
    params: asOf ? { asOf } : undefined,
  });
  return parseApiContract('/iic-norms', iicNormListSchema, data);
}

export async function fetchIicNormHistory(workDirectionId: number): Promise<IicNorm[]> {
  const { data } = await apiClient.get<IicNorm[]>('/iic-norms', {
    params: { workDirectionId },
  });
  return parseApiContract('/iic-norms?workDirectionId', iicNormListSchema, data);
}

export interface IicNormHistoryItem {
  id?: number | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  performance?: number | null;
  estimate?: number | null;
  standardDeviation?: number | null;
  unitId?: number | null;
}

export interface IicNormWriteRequest {
  workDirectionId: number;
  performance?: number | null;
  estimate?: number | null;
  standardDeviation?: number | null;
  unitId?: number | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  history?: IicNormHistoryItem[];
}

export async function createIicNorm(body: IicNormWriteRequest): Promise<IicNorm> {
  const { data } = await apiClient.post<IicNorm>('/registers/iic-work-direction-norms', body);
  return parseApiContract('/registers/iic-work-direction-norms', iicNormSchema, data);
}

export async function updateIicNorm(id: number, body: IicNormWriteRequest): Promise<IicNorm> {
  const { data } = await apiClient.put<IicNorm>(`/iic-norms/${id}`, body);
  return parseApiContract(`/iic-norms/${id}`, iicNormSchema, data);
}

export async function deleteIicNorm(id: number): Promise<void> {
  await apiClient.delete(`/registers/iic-work-direction-norms/${id}`);
}

export async function exportIicNormsExcel(): Promise<void> {
  const { blob, filename } = await downloadBlob(
    '/registers/iic-work-direction-norms/export.xlsx',
    undefined,
    'iic-work-direction-norms.xlsx',
  );
  triggerBrowserDownload(blob, filename);
}
