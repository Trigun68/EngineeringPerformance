import { z } from 'zod';
import apiClient, { downloadBlob, downloadBlobPost, triggerBrowserDownload } from '../services/apiClient';
import { parseApiContract } from '../services/apiContract';
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
  recalculatedVersionCount?: number;
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
  recalculatedVersionCount: z.number().int().optional(),
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
  const { blob, filename } = await downloadBlobPost(
    '/registers/iic-work-direction-norms/export',
    { tabName, headers, rows },
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportIicNormsExcel(): Promise<void> {
  const { blob, filename } = await downloadBlob(
    '/registers/iic-work-direction-norms/export.xlsx',
  );
  triggerBrowserDownload(blob, filename);
}

export interface WorkCostRow {
  id: number;
  workDirectionId: number;
  parentId?: number | null;
  sortOrder: number;
  workDirectionName: string;
  workDirectionCode?: string | null;
  ratePerHour?: number | null;
  effectiveFrom?: string | null;
  periodId?: number | null;
}

const workCostRowSchema = z.object({
  id: z.number().int(),
  workDirectionId: z.number().int(),
  parentId: z.number().int().nullable().optional(),
  sortOrder: z.number().int(),
  workDirectionName: z.string(),
  workDirectionCode: z.string().nullable().optional(),
  ratePerHour: z.number().nullable().optional(),
  effectiveFrom: z.string().nullable().optional(),
  periodId: z.number().int().nullable().optional(),
});
const workCostListSchema = z.array(workCostRowSchema);

export async function fetchWorkCosts(asOf?: string): Promise<WorkCostRow[]> {
  const { data } = await apiClient.get<WorkCostRow[]>('/registers/work-costs', {
    params: asOf ? { asOf } : undefined,
  });
  return parseApiContract('/registers/work-costs', workCostListSchema, data) as WorkCostRow[];
}

export interface WorkCostBatchItem {
  workDirectionId: number;
  ratePerHour: number;
}

export interface WorkCostBatchRequest {
  effectiveFrom: string;
  level: 1 | 2 | 3 | 4;
  items: WorkCostBatchItem[];
}

export interface WorkCostBatchResult {
  writtenCount: number;
}

const workCostBatchResultSchema = z.object({
  writtenCount: z.number().int(),
});

export async function saveWorkCostsBatch(body: WorkCostBatchRequest): Promise<WorkCostBatchResult> {
  const { data } = await apiClient.post<WorkCostBatchResult>('/registers/work-costs/batch', body);
  return parseApiContract('/registers/work-costs/batch', workCostBatchResultSchema, data);
}

export async function exportWorkCostsExcelFromGrid(
  tabName: string,
  headers: string[],
  rows: string[][],
): Promise<void> {
  const { blob, filename } = await downloadBlobPost(
    '/registers/work-costs/export',
    { tabName, headers, rows },
  );
  triggerBrowserDownload(blob, filename);
}

export interface WorkCostPeriod {
  effectiveFrom: string;
  effectiveTo?: string | null;
  effectiveToVaries: boolean;
  directionCount: number;
  uniformRate?: number | null;
}

const workCostPeriodSchema = z.object({
  effectiveFrom: z.string(),
  effectiveTo: z.string().nullable().optional(),
  effectiveToVaries: z.boolean(),
  directionCount: z.number().int(),
  uniformRate: z.number().nullable().optional(),
});
const workCostPeriodListSchema = z.array(workCostPeriodSchema);

export async function fetchWorkCostPeriods(): Promise<WorkCostPeriod[]> {
  const { data } = await apiClient.get<WorkCostPeriod[]>('/registers/work-costs/periods');
  return parseApiContract('/registers/work-costs/periods', workCostPeriodListSchema, data);
}

export async function fetchWorkCostPeriod(effectiveFrom: string): Promise<WorkCostRow[]> {
  const { data } = await apiClient.get<WorkCostRow[]>('/registers/work-costs', {
    params: { effectiveFrom },
  });
  return parseApiContract('/registers/work-costs', workCostListSchema, data) as WorkCostRow[];
}

const workCostPeriodDeleteSchema = z.object({
  deletedCount: z.number().int(),
});

export async function deleteWorkCostPeriod(effectiveFrom: string): Promise<{ deletedCount: number }> {
  const { data } = await apiClient.delete('/registers/work-costs/periods', {
    params: { effectiveFrom },
  });
  return parseApiContract('/registers/work-costs/periods', workCostPeriodDeleteSchema, data);
}
