import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Alert, Box, CircularProgress, Divider, Stack, Tab, Tabs, Typography } from '@mui/material';
import {
  fetchForecast,
  fetchPerformance,
  fetchProjectReportSummary,
  fetchVersionAnalysis,
  type PerformanceRow,
} from '../../../api/analyticsApi';
import { fetchProjectCharacteristics } from '../../../api/projectCharacteristicsApi';
import { exportProjectReportPdf } from '../../../api/visualReportsApi';
import type { Project } from '../../../types/project';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import { getApiErrorMessage } from '../../../services/apiError';
import {
  buildInsights,
  buildTree,
  flattenTree,
  keyDisciplines,
  type VersionTrendPoint,
} from './reportCalc';
import { ReportHeader } from './components/ReportHeader';
import { PlanFactSummary } from './components/PlanFactSummary';
import { ZScale } from './components/ZScale';
import { TopPerformersPanel } from './components/TopPerformersPanel';
import { DirectionBars } from './components/DirectionBars';
import { PerformanceTree } from './components/PerformanceTree';
import { ForecastTable } from './components/ForecastTable';
import { AccuracyPanel } from './components/AccuracyPanel';
import { PhysicalVolumesGrid } from './components/PhysicalVolumesGrid';
import { PhysicalVolumeGroupsTable } from './components/PhysicalVolumeGroupsTable';
import { VersionTrend } from './components/VersionTrend';
import { InsightsPanel } from './components/InsightsPanel';

interface ReportsTabProps {
  projectId: number;
  versionId: number;
  project: Project;
  versions: ProjectVersion[];
}

type DetailTab = 'tree' | 'forecast' | 'accuracy' | 'volumes';

export function ReportsTab({ projectId, versionId, project, versions }: ReportsTabProps) {
  const [detailTab, setDetailTab] = useState<DetailTab>('tree');
  const [exportingPdf, setExportingPdf] = useState(false);

  const handleExportPdf = async () => {
    setExportingPdf(true);
    try {
      await exportProjectReportPdf(projectId, versionId);
    } finally {
      setExportingPdf(false);
    }
  };

  const selectedVersion = versions.find((v) => v.id === versionId) ?? null;

  const nonArchivedVersions = useMemo(
    () => [...versions].filter((v) => !v.isArchived).sort((a, b) => a.sortOrder - b.sortOrder),
    [versions],
  );

  const query = useQuery({
    queryKey: ['reports-tab', projectId, versionId, nonArchivedVersions.map((v) => v.id).join(',')],
    queryFn: async () => {
      const [performance, analysis, forecast, characteristics, reportSummary, trendAnalyses] = await Promise.all([
        fetchPerformance(projectId, versionId),
        fetchVersionAnalysis(projectId, versionId),
        fetchForecast(projectId, versionId),
        fetchProjectCharacteristics(projectId).catch(() => []),
        fetchProjectReportSummary(projectId),
        Promise.all(
          nonArchivedVersions.map(async (v) => {
            try {
              const a = await fetchVersionAnalysis(projectId, v.id);
              return { version: v, summary: a.summary };
            } catch {
              return { version: v, summary: null };
            }
          }),
        ),
      ]);

      const trendPoints: VersionTrendPoint[] = trendAnalyses.map(({ version, summary }) => {
        const createdAt = version.createdAt ? new Date(version.createdAt) : null;
        return {
          versionId: version.id,
          versionName: version.versionName,
          direction: version.direction,
          date: createdAt ? createdAt.toLocaleDateString('ru-RU') : null,
          sortDate: createdAt ? createdAt.getTime() : 0,
          weightedZScore: summary?.weightedZScore ?? null,
          totalHours: summary?.totalFactLaborHours ?? null,
        };
      });

      return { performance, analysis, forecast, characteristics, reportSummary, trendPoints };
    },
    staleTime: 30_000,
  });

  const roots = useMemo(
    () => buildTree(query.data?.performance ?? ([] as PerformanceRow[])),
    [query.data?.performance],
  );
  const flat = useMemo(() => flattenTree(roots), [roots]);
  const leaves = useMemo(() => flat.filter((r) => r.children.length === 0), [flat]);

  const accuracyScore = useMemo(() => {
    const withValue = (query.data?.characteristics ?? []).filter(
      (c) => c.selectedValuePercent !== null && c.selectedValuePercent !== undefined,
    );
    if (withValue.length === 0) return null;
    return withValue.reduce((s, c) => s + (c.selectedValuePercent ?? 0), 0) / withValue.length;
  }, [query.data?.characteristics]);

  const summary = query.data?.analysis.summary;

  const insights = useMemo(
    () => (summary ? buildInsights(leaves, summary, accuracyScore) : []),
    [leaves, summary, accuracyScore],
  );
  const keyRows = useMemo(() => keyDisciplines(leaves), [leaves]);
  const totalSubtreeHours = useMemo(
    () => roots.reduce((s, r) => s + (r.subtreeHours ?? 0), 0),
    [roots],
  );

  const trendPoints = query.data?.trendPoints ?? [];
  const reportSummary = query.data?.reportSummary;

  if (!selectedVersion) {
    return <Alert severity="warning">Версия анализа не найдена.</Alert>;
  }

  if (query.isLoading) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 6 }}>
        <CircularProgress />
      </Box>
    );
  }

  if (query.isError) {
    return <Alert severity="error">{getApiErrorMessage(query.error, 'Не удалось загрузить отчёт.')}</Alert>;
  }

  if (!query.data || !summary || !reportSummary) {
    return null;
  }

  return (
    <Stack spacing={3}>
      <ReportHeader
        project={project}
        version={selectedVersion}
        onExportPdf={() => void handleExportPdf()}
        exporting={exportingPdf}
      />

      <PlanFactSummary summary={reportSummary} accuracyScore={accuracyScore} />

<<<<<<< HEAD
      <ZScale
        planZScore={reportSummary.planWeightedZScore}
        factZScore={reportSummary.factWeightedZScore}
        percentile={reportSummary.factPercentile}
        planPercentile={reportSummary.planPercentile}
      />
=======
      <ZScale planZScore={reportSummary.planWeightedZScore} factZScore={reportSummary.factWeightedZScore} />
>>>>>>> origin/master

      <TopPerformersPanel leaves={leaves} />

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', xl: '1fr 1fr' }, gap: 2 }}>
        <DirectionBars
          rows={roots}
          title="Z-оценка по корневым направлениям"
          description="Агрегированная оценка по поддереву, взвешенная по трудозатратам."
        />
        <DirectionBars
          rows={leaves}
          title="Z-оценка по дисциплинам"
          description="Листовые направления работ — источник фактической производительности."
        />
      </Box>

      <VersionTrend points={trendPoints} />

      <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: { xs: 2, md: 3 } }}>
        <Stack spacing={0.5} sx={{ mb: 1 }}>
          <Typography variant="subtitle1" sx={{ fontWeight: 600 }}>
            Детализация версии
          </Typography>
          <Typography variant="body2" color="text.secondary">
            Дерево направлений работ, сценарии прогноза трудоёмкости и точность сопоставления с нормативной
            выборкой.
          </Typography>
        </Stack>

        <Tabs
          value={detailTab}
          onChange={(_, value) => setDetailTab(value)}
          sx={{ borderBottom: 1, borderColor: 'divider', mb: 2 }}
        >
          <Tab value="tree" label="Направления работ" />
          <Tab value="forecast" label="Прогноз трудоёмкости" />
          <Tab value="accuracy" label="Точность сравнения" />
          <Tab value="volumes" label="Физические объёмы" />
        </Tabs>

        {detailTab === 'tree' && <PerformanceTree rows={roots} />}
        {detailTab === 'forecast' && <ForecastTable rows={query.data.forecast.plannedVolumes} />}
        {detailTab === 'accuracy' && (
          <AccuracyPanel characteristics={query.data.characteristics} score={accuracyScore} />
        )}
        {detailTab === 'volumes' && (
          <Stack spacing={3}>
            <PhysicalVolumesGrid items={summary.physicalVolumeTotalsByUnit} />
            <Divider />
            <Stack spacing={0.5}>
              <Typography variant="subtitle2" sx={{ fontWeight: 600 }}>
                Все группы физобъёмов ИИЦ
              </Typography>
              <Typography variant="body2" color="text.secondary">
                Плоский список листовых направлений работ с сортировкой, поиском и фильтром по интерпретации.
              </Typography>
            </Stack>
            <PhysicalVolumeGroupsTable leaves={leaves} />
          </Stack>
        )}
      </Box>

      <InsightsPanel insights={insights} keyRows={keyRows} totalHours={totalSubtreeHours} />

      <Divider />

      <Typography variant="caption" color="text.secondary" sx={{ lineHeight: 1.6, pb: 1 }}>
        Методика: факт. производительность = часы / физический объём; оценка = ln(производительность);
        FactEstimate строки — средневзвешенное по часам поддерева; Z = (FactEstimate − ln(норма)) / σ.
        Средневзвешенная Z-оценка проекта = Σ(Z корня × часы поддерева) / Σ(часы поддерева). Нормы и σ взяты из
        снимка версии {selectedVersion.versionName}
        {selectedVersion.createdAt ? ` от ${new Date(selectedVersion.createdAt).toLocaleDateString('ru-RU')}` : ''}.
      </Typography>
    </Stack>
  );
}
