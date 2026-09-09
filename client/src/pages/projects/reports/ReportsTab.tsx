import { useMemo, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Alert,
  Box,
  CircularProgress,
  Divider,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material';
import {
  fetchForecast,
  fetchPerformance,
  fetchProjectReportSummary,
  fetchVersionAnalysis,
  type PerformanceRow,
} from '../../../api/analyticsApi';
import { fetchPerformanceReport } from '../../../api/performanceReportApi';
import { fetchProjectCharacteristics } from '../../../api/projectCharacteristicsApi';
import { exportProjectReportPdf } from '../../../api/visualReportsApi';
import { ExportActionButton } from '../../../components/export/ExportActionButton';
import type { Project } from '../../../types/project';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import { getApiErrorMessage } from '../../../services/apiError';
import { queryKeys } from '../../../services/queryKeys';
import {
  buildInsights,
  buildTree,
  factHourRows,
  historyToTrendPoints,
  keyDisciplines,
  MUTED_COLOR,
  selectVersionTrendPoints,
} from './reportCalc';
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
import { wpColors } from '../../../theme/wpColors';

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

  const query = useQuery({
    queryKey: queryKeys.reports.tab(projectId, versionId),
    queryFn: async () => {
      const [performance, analysis, forecast, characteristics, reportSummary, performanceReport] = await Promise.all([
        fetchPerformance(projectId, versionId),
        fetchVersionAnalysis(projectId, versionId),
        fetchForecast(projectId, versionId),
        fetchProjectCharacteristics(projectId).catch(() => []),
        fetchProjectReportSummary(projectId),
        fetchPerformanceReport(projectId),
      ]);

      const trendPoints = selectVersionTrendPoints(
        historyToTrendPoints(performanceReport.versionHistory),
        reportSummary.factVersionId,
      );

      return { performance, analysis, forecast, characteristics, reportSummary, trendPoints };
    },
    staleTime: 30_000,
  });

  const roots = useMemo(
    () => buildTree(query.data?.performance ?? ([] as PerformanceRow[])),
    [query.data?.performance],
  );

  const hourRows = useMemo(() => factHourRows(roots), [roots]);

  const accuracyScore = useMemo(() => {
    const withValue = (query.data?.characteristics ?? []).filter(
      (c) => c.selectedValuePercent !== null && c.selectedValuePercent !== undefined,
    );
    if (withValue.length === 0) return null;
    return withValue.reduce((s, c) => s + (c.selectedValuePercent ?? 0), 0) / withValue.length;
  }, [query.data?.characteristics]);

  const summary = query.data?.analysis.summary;

  const insights = useMemo(
    () => (summary ? buildInsights(hourRows, summary, accuracyScore) : []),
    [hourRows, summary, accuracyScore],
  );
  const keyRows = useMemo(() => keyDisciplines(hourRows), [hourRows]);
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
    <Box
      data-testid="report-v1-page"
      sx={{ bgcolor: wpColors.pageBg, borderRadius: '16px', py: { xs: 2, md: 3 }, px: { xs: 1.5, md: 3 } }}
    >
      <Stack spacing={3} sx={{ width: '100%' }}>
        <Stack direction="row" justifyContent="flex-end">
          <ExportActionButton
            kind="pdf"
            loading={exportingPdf}
            onClick={() => void handleExportPdf()}
            disabled={exportingPdf}
            data-testid="report-export-pdf"
          />
        </Stack>

        <PlanFactSummary summary={reportSummary} accuracyScore={accuracyScore} />

        <ZScale planZScore={reportSummary.planWeightedZScore} factZScore={reportSummary.factWeightedZScore} />

        <VersionTrend points={trendPoints} />

        <Accordion
          defaultExpanded
          disableGutters
          elevation={0}
          sx={{
            bgcolor: 'transparent',
            '&:before': { display: 'none' },
            border: 'none',
          }}
        >
          <AccordionSummary
            expandIcon={<ExpandMoreIcon />}
            data-testid="report-v1-more-toggle"
            sx={{ px: 0, minHeight: 40, '& .MuiAccordionSummary-content': { my: 0.5 } }}
          >
            <Typography sx={{ fontSize: 12, fontWeight: 700, color: MUTED_COLOR, letterSpacing: 0.4 }}>
              Дополнительно — {project.code}
            </Typography>
          </AccordionSummary>
          <AccordionDetails sx={{ px: 0, pt: 1 }}>
            <Stack spacing={3}>
              <TopPerformersPanel rows={hourRows} />

              <DirectionBars
                rows={hourRows}
                title="Оценка Z по группам с факт-часами"
                description="Собственные факт-часы узла: корень после свёртки на первый уровень или заявленная группа ИИС."
              />

              <Box sx={{ border: 1, borderColor: 'divider', borderRadius: 1, p: { xs: 2, md: 3 }, bgcolor: '#FFFFFF' }}>
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
                        Все группы физобъёмов IIC
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        Плоский список групп ИИС с собственными факт-часами: сортировка, поиск и фильтр по интерпретации.
                      </Typography>
                    </Stack>
                    <PhysicalVolumeGroupsTable rows={hourRows} />
                  </Stack>
                )}
              </Box>

              <InsightsPanel insights={insights} keyRows={keyRows} totalHours={totalSubtreeHours} />

              <Typography variant="caption" color="text.secondary" sx={{ lineHeight: 1.6, pb: 1 }}>
                Методика: факт. производительность = часы / физический объём; оценка = ln(производительность);
                FactEstimate строки — средневзвешенное по часам поддерева; Z = (FactEstimate − ln(норма)) / σ.
                Средневзвешенная Z-оценка проекта = Σ(Z корня × часы поддерева) / Σ(часы поддерева). Нормы и σ взяты из
                снимка версии {selectedVersion.versionName}
                {selectedVersion.createdAt ? ` от ${new Date(selectedVersion.createdAt).toLocaleDateString('ru-RU')}` : ''}.
              </Typography>
            </Stack>
          </AccordionDetails>
        </Accordion>
      </Stack>
    </Box>
  );
}
