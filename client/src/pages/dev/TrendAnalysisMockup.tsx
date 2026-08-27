import { useEffect, useState } from 'react';
import { Box, Container, Stack, Typography } from '@mui/material';
import apiClient from '../../services/apiClient';
import { TrendAnalysisPanel } from '../../features/analytics/trends/TrendAnalysisPanel';
import type { ZTrendResponse } from '../../api/zTrendApi';

// -----------------------------------------------------------------------------
// Дев-мокап реальной вкладки «Анализ тренда оценки факта» (страница Аналитика).
// Backend недоступен в этой среде — подменяем apiClient.post канонической формой
// ответа /analytics/trends/z-score, чтобы показать РЕАЛЬНЫЙ компонент с данными,
// а не отдельный статический макет.
// Доступно без авторизации и без AppLayout — только для просмотра дизайна.
// -----------------------------------------------------------------------------

const NORM_COLOR_A = '#2E86C1';
const NORM_COLOR_B = '#8E44AD';

function isoWeeksAgo(weeksAgo: number): string {
  const d = new Date();
  d.setUTCDate(d.getUTCDate() - weeksAgo * 7);
  return d.toISOString().slice(0, 10);
}

function buildSegmentPoints(
  weeks: number,
  baseZ: number,
  amplitude: number,
  normId: number,
): { date: string; zScore: number | null; weight: number; projectCount: number; normId: number; flag: string }[] {
  return Array.from({ length: weeks }, (_, i) => {
    const t = i / Math.max(1, weeks - 1);
    const wobble = Math.sin(t * Math.PI * 2.4 + normId) * amplitude;
    const z = baseZ + wobble;
    const flagged = i === Math.floor(weeks * 0.65);
    return {
      date: isoWeeksAgo(weeks - 1 - i),
      zScore: Number(z.toFixed(2)),
      weight: 480 + Math.round(Math.sin(t * Math.PI * 3) * 90),
      projectCount: 3 + (i % 3),
      normId,
      flag: flagged ? 'clipped' : 'ok',
    };
  });
}

function buildGroup(
  groupId: number,
  groupName: string,
  weeks: number,
  baseZPlan: number,
  baseZFact: number,
): ZTrendResponse['overall'] {
  const planPoints = buildSegmentPoints(weeks, baseZPlan, 0.35, groupId * 10 + 1);
  const factPoints = buildSegmentPoints(weeks, baseZFact, 0.55, groupId * 10 + 2);
  return {
    groupId,
    groupName,
    plan: {
      seriesType: 'Plan',
      segments: [
        {
          normId: groupId * 10 + 1,
          normName: `Норма IIC-${groupId}.1`,
          normGroupId: groupId,
          normGroupName: groupName,
          color: NORM_COLOR_A,
          segmentKey: `plan-${groupId}-1`,
          effectiveFrom: isoWeeksAgo(weeks),
          effectiveTo: null,
          points: planPoints,
        },
      ],
    },
    fact: {
      seriesType: 'Fact',
      segments: [
        {
          normId: groupId * 10 + 2,
          normName: `Норма IIC-${groupId}.2`,
          normGroupId: groupId,
          normGroupName: groupName,
          color: NORM_COLOR_B,
          segmentKey: `fact-${groupId}-2`,
          effectiveFrom: isoWeeksAgo(weeks),
          effectiveTo: null,
          points: factPoints,
        },
      ],
    },
  };
}

function buildMockZTrendResponse(): ZTrendResponse {
  const weeks = 10;
  const overall = buildGroup(0, 'Все направления', weeks, -0.28, 0.18);
  const byGroup = [
    buildGroup(1, 'Металлоконструкции', weeks, -0.62, -0.15),
    buildGroup(2, 'Электромонтажные работы', weeks, 0.41, 0.74),
    buildGroup(3, 'Трубопроводы', weeks, -0.05, 0.22),
  ];

  const legend = [...overall.plan.segments, ...overall.fact.segments].map((seg) => ({
    normId: seg.normId,
    normName: seg.normName,
    groupId: seg.normGroupId,
    groupName: seg.normGroupName,
    color: seg.color,
    segmentKey: seg.segmentKey,
    effectiveFrom: seg.effectiveFrom,
    effectiveTo: seg.effectiveTo,
  }));

  return {
    overall,
    byGroup,
    legend,
    warnings: ['Демонстрационные данные — реальный backend в этой среде не запущен.'],
  };
}

export default function TrendAnalysisMockup() {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const original = apiClient.post.bind(apiClient);
    const mockResponse = buildMockZTrendResponse();

    // @ts-expect-error — временная подмена только для этого дев-мокапа, тип не важен.
    apiClient.post = async (url: string) => {
      if (typeof url === 'string' && url.includes('/analytics/trends/z-score')) {
        await new Promise((resolve) => setTimeout(resolve, 350));
        return { data: mockResponse };
      }
      throw new Error(`Мокап не поддерживает запрос: ${url}`);
    };

    setReady(true);

    return () => {
      apiClient.post = original;
    };
  }, []);

  if (!ready) return null;

  return (
    <Box sx={{ minHeight: '100vh', bgcolor: 'background.default', py: 4 }}>
      <Container maxWidth="lg">
        <Stack spacing={2}>
          <Box>
            <Typography variant="h5" component="h1">
              Анализ тренда оценки факта
            </Typography>
            <Typography variant="body2" color="text.secondary">
              Динамика средневзвешенного Z по основным версиям плана и факта относительно нормы на
              дату версии.
            </Typography>
          </Box>
          <TrendAnalysisPanel
            dateFrom={isoWeeksAgo(10)}
            dateTo={isoWeeksAgo(0)}
            weightMode="basisHours"
            showPlan
            showFact
          />
        </Stack>
      </Container>
    </Box>
  );
}
