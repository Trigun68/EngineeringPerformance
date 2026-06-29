import apiClient from '../services/apiClient';

export interface SystemMetricsRed {
  totalRequests: number;
  averageDurationMs: number;
  p95DurationMs: number;
  serverErrorCount: number;
  windowMinutes: number;
}

export interface MetricsTimeSeriesPoint {
  timestampUtc: string;
  value: number;
}

export interface ErrorDistributionItem {
  errorType: string;
  count: number;
}

export interface DependencyHealth {
  isConfigured: boolean;
  isConnected: boolean;
  status: string;
}

export interface SystemMetrics {
  red: SystemMetricsRed;
  requestRateSeries: MetricsTimeSeriesPoint[];
  errorDistribution: ErrorDistributionItem[];
  responseTimeTrend24h: MetricsTimeSeriesPoint[];
  database: DependencyHealth;
  redis: DependencyHealth;
  collectedAtUtc: string;
}

export interface RolePermissionGap {
  roleId: number;
  roleName: string;
  missingObjects: string[];
}

export interface PermissionDrift {
  hasDrift: boolean;
  unknownObjects: string[];
  missingObjectsByRole: RolePermissionGap[];
  checkedAtUtc: string;
}

export async function fetchSystemMetrics(windowMinutes = 15): Promise<SystemMetrics> {
  const { data } = await apiClient.get<SystemMetrics>('/monitoring/system-metrics', {
    params: { windowMinutes },
  });
  return data;
}

export async function fetchPermissionDrift(): Promise<PermissionDrift> {
  const { data } = await apiClient.get<PermissionDrift>('/monitoring/permission-drift');
  return data;
}
