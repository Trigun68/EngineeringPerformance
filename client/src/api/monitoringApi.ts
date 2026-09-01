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

export interface ServiceResource {
  available: boolean;
  status: string | null;
  cpuCores: number;
  rssMib: number;
}

export interface SqlLaneConnections {
  interactive: number;
  analytics: number;
  other: number;
  total: number;
}

export interface BackendRuntime {
  gcHeapMib: number;
  allocRateBytesPerSecond: number;
  gen2Collections: number;
  threadPoolBusy: number;
  threadPoolQueue: number;
  sqlRunnable: number;
  redisHitRatio: number;
  redisOpsPerSec: number;
  redisEvictedKeys: number;
  redisFragmentation: number;
  sqlBatchRequestsPerSec: number;
  sqlBufferCacheHitRatio: number;
}

export interface ResourceSnapshot {
  activeUsers: number;
  activeSessions: number;
  kestrelConnections: number;
  currentHttpRequests: number;
  sqlConnections: SqlLaneConnections;
  redisClients: number;
  analyticsGateBusy: number;
  analyticsGateLimit: number;
  backend: ServiceResource;
  redis: ServiceResource;
  sql: ServiceResource;
  cpuCoresPerUser: number | null;
  rssMibPerUser: number | null;
  runtime: BackendRuntime;
}

export interface ResourceSeries {
  backendCpuCores: MetricsTimeSeriesPoint[];
  redisCpuCores: MetricsTimeSeriesPoint[];
  sqlCpuCores: MetricsTimeSeriesPoint[];
  backendRssMib: MetricsTimeSeriesPoint[];
  redisRssMib: MetricsTimeSeriesPoint[];
  sqlRssMib: MetricsTimeSeriesPoint[];
  activeUsers: MetricsTimeSeriesPoint[];
  kestrelConnections: MetricsTimeSeriesPoint[];
  sqlInteractive: MetricsTimeSeriesPoint[];
  sqlAnalytics: MetricsTimeSeriesPoint[];
  redisClients: MetricsTimeSeriesPoint[];
  threadPoolQueue: MetricsTimeSeriesPoint[];
  sqlRunnable: MetricsTimeSeriesPoint[];
  redisHitRatio: MetricsTimeSeriesPoint[];
  allocRateMibPerSec: MetricsTimeSeriesPoint[];
  gen2Collections: MetricsTimeSeriesPoint[];
  sqlMemoryLimitMib: number;
}

export interface ResourceMetrics {
  snapshot: ResourceSnapshot;
  series: ResourceSeries;
  verdicts: string[];
  collectedAtUtc: string;
  windowMinutes: number;
  bucketSeconds: number;
  historyAvailableFromUtc: string | null;
}

export async function fetchResourceMetrics(windowMinutes = 60): Promise<ResourceMetrics> {
  const { data } = await apiClient.get<ResourceMetrics>('/monitoring/resource-metrics', {
    params: { windowMinutes },
  });
  return data;
}
