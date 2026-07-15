import apiClient, { downloadBlob, triggerBrowserDownload } from '../services/apiClient';

export interface PerformanceRow {
  workDirectionId: number;
  workDirectionName: string;
  workDirectionCode: string;
  parentId: number | null;
  level: number;
  normUnitName: string | null;
  factPhysicalVolume: number | null;
  factLaborHours: number | null;
  factProductivity: number | null;
  factEstimate: number | null;
  normProductivity: number | null;
  normEstimate: number | null;
  normStdDev: number | null;
  zScore: number | null;
  interpretation: string | null;
  subtreeHours: number | null;
}

export interface ProjectVersionAnalysis {
  projectName: string;
  projectCode: string | null;
  versionName: string;
  directions: ProjectVersionDirection[];
  laborIntensities: ProjectVersionLaborRow[];
  summary: ProjectVersionAnalysisSummary;
}

export interface ProjectVersionDirection {
  id: number;
  parentId: number | null;
  level: number;
  code: string;
  name: string;
  unitName: string | null;
  factPhysicalVolume: number | null;
  factLaborHours: number | null;
  factProductivity: number | null;
  factEstimate: number | null;
  normProductivity: number | null;
  normEstimate: number | null;
  normStdDev: number | null;
  zScore: number | null;
  interpretation: string | null;
}

export interface ProjectVersionLaborRow {
  documentationCode: string;
  brand: string | null;
  workDirectionName: string | null;
  plannedHours: number;
  actualHours: number;
}

export interface ProjectVersionAnalysisSummary {
  totalFactLaborHours: number | null;
  weightedZScore: number | null;
  weightedZScorePercentile: string;
  weightedZScoreInterpretation: string | null;
  unallocatedLaborHours: number | null;
}

export async function fetchPerformance(
  projectId: number,
  versionId: number,
): Promise<PerformanceRow[]> {
  const { data } = await apiClient.get<PerformanceRow[]>(
    `/projects/${projectId}/versions/${versionId}/performance`,
  );
  return data;
}

export async function fetchVersionAnalysis(
  projectId: number,
  versionId: number,
): Promise<ProjectVersionAnalysis> {
  const { data } = await apiClient.get<ProjectVersionAnalysis>(
    `/projects/${projectId}/versions/${versionId}/analysis`,
  );
  return data;
}

export interface ForecastIicVolumeRow {
  workDirectionId: number;
  parentId: number | null;
  name: string;
  unitName: string | null;
  normProductivity: number | null;
  normEstimate: number | null;
  normStandardDeviation: number | null;
  plannedVolume: number;
  optimisticHours: number;
  baseHours: number;
  pessimisticHours: number;
  range: string;
  hierarchyPath: string;
}

export interface ForecastScenario {
  scenarioName: string;
  zScore: number;
  totalLaborHours: number;
  disciplines: ForecastIicVolumeRow[];
}

export interface ScenarioInterpretation {
  scenarioName: string;
  laborHours: number;
  percentOfBase: number;
  interpretation: string;
}

export interface TopDiscipline {
  rank: number;
  disciplineName: string;
  baseHours: number;
}

export interface ProjectForecastReport {
  projectName: string;
  projectCode: string;
  versionName: string;
  projectCharacteristics: Record<string, string>;
  plannedVolumes: ForecastIicVolumeRow[];
  scenarios: ForecastScenario[];
  top3Disciplines: TopDiscipline[];
  interpretations: ScenarioInterpretation[];
}

export interface ForecastCalculationOptions {
  method?: 'regression' | 'element';
  zOptimistic?: number;
  zBase?: number;
  zPessimistic?: number;
}

export interface ForecastCalculateRequest {
  nodePlannedTotals?: Record<number, number>;
  calculation?: ForecastCalculationOptions;
}

export async function fetchForecast(
  projectId: number,
  versionId: number,
  calculation?: ForecastCalculationOptions,
): Promise<ProjectForecastReport> {
  const { data } = await apiClient.get<ProjectForecastReport>(
    `/projects/${projectId}/versions/${versionId}/forecast`,
    {
      params: calculation
        ? {
            method: calculation.method,
            zOptimistic: calculation.zOptimistic,
            zBase: calculation.zBase,
            zPessimistic: calculation.zPessimistic,
          }
        : undefined,
    },
  );
  return data;
}

export async function previewForecast(
  projectId: number,
  versionId: number,
  nodePlannedTotals: Record<number, number>,
  calculation?: ForecastCalculationOptions,
): Promise<ProjectForecastReport> {
  const { data } = await apiClient.post<ProjectForecastReport>(
    `/projects/${projectId}/versions/${versionId}/forecast/preview`,
    {
      nodePlannedTotals,
      calculation: calculation
        ? {
            method: calculation.method,
            zOptimistic: calculation.zOptimistic,
            zBase: calculation.zBase,
            zPessimistic: calculation.zPessimistic,
          }
        : undefined,
    },
  );
  return data;
}

export async function calculateForecast(
  projectId: number,
  versionId: number,
  request: ForecastCalculateRequest = {},
): Promise<ProjectForecastReport> {
  const { data } = await apiClient.post<ProjectForecastReport>(
    `/projects/${projectId}/versions/${versionId}/forecast/calculate`,
    {
      nodePlannedTotals: request.nodePlannedTotals,
      calculation: request.calculation
        ? {
            method: request.calculation.method,
            zOptimistic: request.calculation.zOptimistic,
            zBase: request.calculation.zBase,
            zPessimistic: request.calculation.zPessimistic,
          }
        : undefined,
    },
  );
  return data;
}

export async function saveForecastPlannedVolumes(
  projectId: number,
  versionId: number,
  items: { workDirectionId: number; quantity: number }[],
): Promise<void> {
  await apiClient.put(
    `/projects/${projectId}/versions/${versionId}/forecast/planned-volumes`,
    { items },
  );
}

export interface ProjectVersionPhysicalVolume {
  code: string;
  name: string;
  unitName: string | null;
  quantity: number;
  coefficient: number;
  total: number;
}

export interface ForecastAssessmentVolumes {
  physicalVolumesByDirection: Record<string, ProjectVersionPhysicalVolume[]>;
  equipmentVolumesByDirection: Record<string, ProjectVersionPhysicalVolume[]>;
}

export async function fetchForecastAssessmentVolumes(
  projectId: number,
  versionId: number,
): Promise<ForecastAssessmentVolumes> {
  const { data } = await apiClient.get<ForecastAssessmentVolumes>(
    `/projects/${projectId}/versions/${versionId}/forecast/assessment-volumes`,
  );
  return data;
}

export interface VersionCompareRow {
  workDirectionId: number;
  parentId: number | null;
  name: string;
  zScoreA: number | null;
  zScoreB: number | null;
  zDelta: number | null;
  interpretationA: string;
  interpretationB: string;
}

export interface VersionCompareFacilityRow {
  facilityCode: string;
  facilityName: string;
  factLaborHoursA: number | null;
  factLaborHoursB: number | null;
  hoursDelta: number | null;
  zScoreA: number | null;
  zScoreB: number | null;
  zDelta: number | null;
  interpretationA: string;
  interpretationB: string;
}

export interface VersionCompareDetailRow {
  facilityCode: string;
  facilityName: string;
  documentSet: string;
  pvCode: string;
  pvName: string;
  workId: string;
  workName: string;
  quantityA: number | null;
  quantityB: number | null;
  quantityDelta: number | null;
  factLaborHoursA: number | null;
  factLaborHoursB: number | null;
  hoursDelta: number | null;
  factProductivityA: number | null;
  factProductivityB: number | null;
  productivityDelta: number | null;
  iicGroupName: string;
  iicUnitName: string;
  presence: string;
}

export interface VersionCompareResult {
  summary: VersionCompareRow[];
  facility: VersionCompareFacilityRow[];
  detail: VersionCompareDetailRow[];
}

export async function fetchVersionCompare(
  projectId: number,
  versionA: number,
  versionB: number,
): Promise<VersionCompareResult> {
  const { data } = await apiClient.get<VersionCompareResult>(`/projects/${projectId}/compare`, {
    params: { versionA, versionB },
  });
  return data;
}

export interface CompareChartPoint {
  category: string;
  workDirectionId: number | null;
  valueA: number | null;
  valueB: number | null;
  delta: number | null;
}

export interface CompareChartResult {
  level: string;
  metric: string;
  versionAName: string;
  versionBName: string;
  points: CompareChartPoint[];
}

export async function fetchCompareChart(
  projectId: number,
  versionA: number,
  versionB: number,
  level: 'iic' | 'facility' = 'iic',
  metric: 'zScore' | 'hours' = 'zScore',
  top = 20,
): Promise<CompareChartResult> {
  const { data } = await apiClient.get<CompareChartResult>(`/projects/${projectId}/compare/chart`, {
    params: { versionA, versionB, level, metric, top },
  });
  return data;
}

export interface TrendPoint {
  timestamp: string;
  versionId: number;
  versionName: string;
  metric: string;
  value: number | null;
  label?: string | null;
}

export interface TrendSeries {
  metric: string;
  displayName: string;
  points: TrendPoint[];
}

export interface TrendAnalyticsResult {
  granularity: string;
  formula: { id: string; displayName: string; description: string };
  series: TrendSeries[];
  events: { timestamp: string; type: string; label: string; versionId?: number | null }[];
}

export interface TrendDetailRow {
  timestamp: string;
  versionId: number;
  facilityCode: string;
  facilityName: string;
  pvCode: string;
  pvName: string;
  factLaborHours: number | null;
  zScore: number | null;
  interpretation: string;
}

export async function fetchTrendFormulas(): Promise<{ id: string; displayName: string; description: string }[]> {
  const { data } = await apiClient.get<{ id: string; displayName: string; description: string }[]>(
    '/analytics/trends/formulas',
  );
  return data;
}

export async function fetchTrends(params: {
  projectId: number;
  dateFrom?: string;
  dateTo?: string;
  granularity?: string;
  formulaId?: string;
}): Promise<TrendAnalyticsResult> {
  const { data } = await apiClient.get<TrendAnalyticsResult>('/analytics/trends', { params });
  return data;
}

export async function fetchTrendDetail(
  projectId: number,
  versionId: number,
  formulaId?: string,
): Promise<TrendDetailRow[]> {
  const { data } = await apiClient.get<TrendDetailRow[]>('/analytics/trends/detail', {
    params: { projectId, versionId, formulaId },
  });
  return data;
}

export interface GroupAnalysisProjectRow {
  projectId: number;
  projectCode: string;
  projectName: string;
  versionId: number | null;
  versionName: string | null;
  totalFactLaborHours: number | null;
  weightedZScore: number | null;
  weightedZInterpretation: string | null;
}

export interface GroupAnalysisResult {
  groupCriterion: string;
  groupValue: string | null;
  projects: GroupAnalysisProjectRow[];
  aggregateWeightedZScore: number | null;
  aggregateWeightedZInterpretation: string | null;
}

export interface GroupTrendInfo {
  message: string;
  trendsPagePath: string;
  peerProjectCount: number;
}

export async function fetchGroupAnalysis(projectId: number): Promise<GroupAnalysisResult> {
  const { data } = await apiClient.get<GroupAnalysisResult>(`/projects/${projectId}/group-analysis`);
  return data;
}

export async function fetchGroupTrend(projectId: number): Promise<GroupTrendInfo> {
  const { data } = await apiClient.get<GroupTrendInfo>(`/projects/${projectId}/group-trend`);
  return data;
}

export async function exportAnalysisExcel(projectId: number, versionId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/analysis.xlsx`,
    undefined,
    'analysis.xlsx',
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportAnalysisPdf(projectId: number, versionId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/analysis.pdf`,
    undefined,
    'analysis.pdf',
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastExcel(projectId: number, versionId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast.xlsx`,
    undefined,
    'forecast.xlsx',
  );
  triggerBrowserDownload(blob, filename);
}

export async function exportForecastPdf(projectId: number, versionId: number): Promise<void> {
  const { blob, filename } = await downloadBlob(
    `/projects/${projectId}/versions/${versionId}/export/forecast.pdf`,
    undefined,
    'forecast.pdf',
  );
  triggerBrowserDownload(blob, filename);
}
