import apiClient from '../services/apiClient';
import type { DependencyHealth, MetricsTimeSeriesPoint, SystemMetrics } from './monitoringApi';

export type { SystemMetrics, MetricsTimeSeriesPoint, DependencyHealth };

export interface SerilogLogEntry {
  timestampUtc: string;
  level: string;
  message: string;
  exception: string | null;
  sourceContext: string | null;
}

export interface LogViewerResult {
  entries: SerilogLogEntry[];
  totalCount: number;
}

export interface ServiceHealth {
  name: string;
  isConfigured: boolean;
  isHealthy: boolean;
  status: string;
  responseTimeMs: number | null;
}

export interface HealthCheckResult {
  checkedAtUtc: string;
  services: ServiceHealth[];
  responseTimeTrend24h: MetricsTimeSeriesPoint[];
}

export interface ConfigAuditEntry {
  id: number;
  userId: number;
  userName: string;
  timestampUtc: string;
  category: string;
  entityType: string | null;
  entityId: number | null;
  description: string | null;
}

export async function fetchSystemMetrics(windowMinutes = 15): Promise<SystemMetrics> {
  const { data } = await apiClient.get<SystemMetrics>('/monitoring/system-metrics', {
    params: { windowMinutes },
  });
  return data;
}

export async function fetchSystemLogs(params?: {
  take?: number;
  level?: string;
  search?: string;
}): Promise<LogViewerResult> {
  const { data } = await apiClient.get<LogViewerResult>('/monitoring/logs', { params });
  return data;
}

export async function fetchSystemHealth(): Promise<HealthCheckResult> {
  const { data } = await apiClient.get<HealthCheckResult>('/monitoring/health');
  return data;
}

export async function fetchConfigAudit(params?: {
  from?: string;
  to?: string;
  userId?: number;
  search?: string;
  take?: number;
}): Promise<ConfigAuditEntry[]> {
  const { data } = await apiClient.get<ConfigAuditEntry[]>('/monitoring/audit-config', { params });
  return data;
}
