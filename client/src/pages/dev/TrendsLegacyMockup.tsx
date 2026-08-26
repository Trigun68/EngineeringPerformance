import { useEffect, useState } from 'react';
import { Box, Container, Stack } from '@mui/material';
import apiClient from '../../services/apiClient';
import { TrendsAnalyticsPage } from '../analytics/TrendsAnalyticsPage';
import type { TrendAnalyticsResult, TrendDetailRow } from '../../api/analyticsApi';
import type { Project } from '../../types/project';

// -----------------------------------------------------------------------------
// Дев-мокап восстановленной архивной вкладки «Трендовый анализ (архив)».
// Backend недоступен в этой среде — подменяем apiClient.get канонической формой
// ответов /projects, /analytics/trends/formulas, /analytics/trends,
// /analytics/trends/detail и /users/me/preferences, чтобы показать РЕАЛЬНЫЙ
// компонент с данными. Доступно без авторизации и без AppLayout.
// -----------------------------------------------------------------------------

const PROJECTS: Project[] = [
  {
    id: 1,
    code: 'PRJ-01',
    name: 'Реконструкция НПЗ',
    isArchived: false,
    planVersionCount: 3,
    factVersionCount: 5,
  },
];

const FORMULAS = [
  { id: 'v1-docset-proportional', displayName: 'V1 · Пропорционально комплекту документации', description: 'Базовая формула распределения нормо-часов по комплекту документации.' },
];

function isoWeeksAgo(weeksAgo: number): string {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() - weeksAgo * 7);
  return d.toISOString().slice(0, 10);
}

function buildTrendsResult(): TrendAnalyticsResult {
  const versions = ['Версия 1.0', 'Версия 1.1', 'Версия 2.0', 'Версия 2.1', 'Версия 3.0'];
  const versionIds = [101, 102, 103, 104, 105];
  const factBase = [820, 1450, 2380, 3120, 4050];
  const forecastBase = [900, 1500, 2200, 3000, 3900];

  const makeSeries = (metric: string, displayName: string, values: number[]) => ({
    metric,
    displayName,
    points: values.map((v, i) => ({
      metric,
      timestamp: `${isoWeeksAgo(20 - i * 4)}T00:00:00Z`,
      value: v,
      versionId: versionIds[i],
      versionName: versions[i],
    })),
  });

  const delta = factBase.map((f, i) => Number((f - forecastBase[i]).toFixed(0)));
  const cumulativeFact = factBase.reduce<number[]>((acc, v, i) => {
    acc.push((acc[i - 1] ?? 0) + v);
    return acc;
  }, []);

  return {
    granularity: 'version',
    formula: FORMULAS[0],
    series: [
      makeSeries('factLaborHours', 'Факт, ч', factBase),
      makeSeries('forecastLaborHours', 'Прогноз / План, ч', forecastBase),
      makeSeries('hoursDelta', 'Δ, ч', delta),
      makeSeries('cumulativeFactHours', 'Накопительно, ч', cumulativeFact),
    ],
    events: [
      { timestamp: `${isoWeeksAgo(16)}T00:00:00Z`, type: 'milestone', label: 'Веха: выпуск РД', versionId: 103 },
      { timestamp: `${isoWeeksAgo(8)}T00:00:00Z`, type: 'milestone', label: 'Веха: комплектация', versionId: 104 },
    ],
  };
}

function buildDetailRows(versionId: number): TrendDetailRow[] {
  const facilities = [
    { code: 'F-001', name: 'Насосная станция №1' },
    { code: 'F-002', name: 'Резервуарный парк' },
    { code: 'F-003', name: 'Эстакада трубопроводов' },
  ];
  const pvs = [
    { code: 'PV-10', name: 'Металлоконструкции' },
    { code: 'PV-20', name: 'Электромонтаж' },
  ];
  const interpretations = ['отличное', 'удовлетворительное', 'критическое'];

  return facilities.flatMap((f, fi) =>
    pvs.map((pv, pi) => ({
      timestamp: new Date().toISOString(),
      versionId,
      facilityCode: f.code,
      facilityName: f.name,
      pvCode: pv.code,
      pvName: pv.name,
      factLaborHours: 120 + fi * 60 + pi * 30,
      zScore: Number((((fi + pi) % 3) - 1).toFixed(2)),
      interpretation: interpretations[(fi + pi) % interpretations.length],
    })),
  );
}

export default function TrendsLegacyMockup() {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const original = apiClient.get.bind(apiClient);
    const mockTrends = buildTrendsResult();

    // @ts-expect-error — временная подмена только для этого дев-мокапа, тип не важен.
    apiClient.get = async (url: string, config?: { params?: Record<string, unknown> }) => {
      await new Promise((resolve) => setTimeout(resolve, 150));
      if (typeof url === 'string' && url.includes('/analytics/trends/detail')) {
        const versionId = Number(config?.params?.versionId) || 101;
        return { data: buildDetailRows(versionId) };
      }
      if (typeof url === 'string' && url.includes('/analytics/trends/formulas')) {
        return { data: FORMULAS };
      }
      if (typeof url === 'string' && url.includes('/analytics/trends')) {
        return { data: mockTrends };
      }
      if (typeof url === 'string' && url.includes('/projects')) {
        return { data: PROJECTS };
      }
      if (typeof url === 'string' && url.includes('/users/me/preferences')) {
        return { data: {} };
      }
      throw new Error(`Мокап не поддерживает запрос: ${url}`);
    };

    setReady(true);

    return () => {
      apiClient.get = original;
    };
  }, []);

  if (!ready) return null;

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default', py: 4 }}>
      <Container maxWidth="lg">
        <Stack spacing={2}>
          <TrendsAnalyticsPage />
        </Stack>
      </Container>
    </Box>
  );
}
