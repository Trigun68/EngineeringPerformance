import { z } from 'zod';
import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';
import { parseApiContract } from '../services/apiContract';
import { buildExportFileName } from '../utils/directoryExport';
import type { DeviationFormulaDto, NormFormulaDto } from '../types/norms';

export type { DeviationFormulaDto, NormFormulaDto };

export interface IicNorm {
  id: number;
  workDirectionId: number;
  workDirectionName?: string | null;
  workDirectionCode?: string | null;
  performance?: number | null;
  estimate?: number | null;
  standardDeviation?: number | null;
  unitId?: number | null;
  resolvedUnitId?: number | null;
  unitName?: string | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  normFormula?: NormFormulaDto | null;
  deviationFormula?: DeviationFormulaDto | null;
  migratedAt?: string | null;
}

const normFormulaSchema = z
  .object({
    type: z.number(),
    coefficients: z.record(z.string(), z.number()),
    minVolume: z.number(),
    maxVolume: z.number(),
    extrapolation: z.number(),
    piecewisePoints: z.array(z.object({ volume: z.number(), norm: z.number() })).nullable().optional(),
    polynomialCoeffs: z.array(z.number()).nullable().optional(),
  })
  .passthrough();

const deviationFormulaSchema = z
  .object({
    modelType: z.number(),
    coefficients: z.record(z.string(), z.number()),
  })
  .passthrough();

const iicNormSchema = z.object({
  id: z.number().int(),
  workDirectionId: z.number().int(),
  workDirectionName: z.string().nullable().optional(),
  workDirectionCode: z.string().nullable().optional(),
  performance: z.number().nullable().optional(),
  estimate: z.number().nullable().optional(),
  standardDeviation: z.number().nullable().optional(),
  unitId: z.number().int().nullable().optional(),
  resolvedUnitId: z.number().int().nullable().optional(),
  unitName: z.string().nullable().optional(),
  effectiveFrom: z.string(),
  effectiveTo: z.string().nullable().optional(),
  createdAt: z.string().optional(),
  modifiedAt: z.string().nullable().optional(),
  normFormula: normFormulaSchema.nullable().optional(),
  deviationFormula: deviationFormulaSchema.nullable().optional(),
  migratedAt: z.string().nullable().optional(),
});
const iicNormListSchema = z.array(iicNormSchema);

export async function fetchIicNorms(asOf?: string): Promise<IicNorm[]> {
  const { data } = await apiClient.get<IicNorm[]>('/iic-norms', {
    params: asOf ? { asOf } : undefined,
  });
  return parseApiContract('/iic-norms', iicNormListSchema, data) as IicNorm[];
}

export async function fetchIicNormHistory(workDirectionId: number): Promise<IicNorm[]> {
  const { data } = await apiClient.get<IicNorm[]>('/registers/iic-work-direction-norms', {
    params: { workDirectionId },
  });
  return parseApiContract('/registers/iic-work-direction-norms?workDirectionId', iicNormListSchema, data) as IicNorm[];
}

export async function fetchIicNormById(id: number): Promise<IicNorm> {
  const { data } = await apiClient.get<IicNorm>(`/registers/iic-work-direction-norms/${id}`);
  return parseApiContract(`/registers/iic-work-direction-norms/${id}`, iicNormSchema, data) as IicNorm;
}

export interface IicNormHistoryItem {
  id?: number | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  performance?: number | null;
  estimate?: number | null;
  standardDeviation?: number | null;
  unitId?: number | null;
  normFormula?: NormFormulaDto | null;
  deviationFormula?: DeviationFormulaDto | null;
  migratedAt?: string | null;
}

export interface IicNormWriteRequest {
  workDirectionId: number;
  performance?: number | null;
  estimate?: number | null;
  standardDeviation?: number | null;
  unitId?: number | null;
  effectiveFrom: string;
  effectiveTo?: string | null;
  normFormula?: NormFormulaDto | null;
  deviationFormula?: DeviationFormulaDto | null;
}

export async function createIicNorm(body: IicNormWriteRequest): Promise<IicNorm> {
  const { data } = await apiClient.post<IicNorm>('/registers/iic-work-direction-norms', body);
  return parseApiContract('/registers/iic-work-direction-norms', iicNormSchema, data) as IicNorm;
}

export async function updateIicNorm(id: number, body: IicNormWriteRequest): Promise<IicNorm> {
  const { data } = await apiClient.put<IicNorm>(`/registers/iic-work-direction-norms/${id}`, body);
  return parseApiContract(`/registers/iic-work-direction-norms/${id}`, iicNormSchema, data) as IicNorm;
}

export async function deleteIicNorm(id: number): Promise<void> {
  await apiClient.delete(`/registers/iic-work-direction-norms/${id}`);
}

export async function exportIicNormsExcelFromGrid(
  tabName: string,
  headers: string[],
  rows: string[][],
): Promise<void> {
  const { data } = await apiClient.post<ArrayBuffer>(
    '/registers/iic-work-direction-norms/export',
    { tabName, headers, rows },
    { responseType: 'arraybuffer' },
  );

  const filename = buildExportFileName(tabName);
  const blob = new Blob([data], {
    type: 'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  });
  triggerBrowserDownload(blob, filename);
}

export async function exportIicNormsExcel(): Promise<void> {
  const { blob, filename } = await downloadBlob(
    '/registers/iic-work-direction-norms/export.xlsx',
    undefined,
    'iic-work-direction-norms.xlsx',
  );
  triggerBrowserDownload(blob, filename);
}
