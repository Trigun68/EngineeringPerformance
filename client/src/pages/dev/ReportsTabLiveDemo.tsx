import { useEffect, useState } from 'react';
import { Box, Typography } from '@mui/material';
import type { AxiosAdapter, AxiosResponse } from 'axios';
import apiClient from '../../services/apiClient';
import { ReportsTab } from '../projects/reports/ReportsTab';
import type { Project } from '../../types/project';
import type { ProjectVersion } from '../../api/projectVersionsApi';
import type { PerformanceRow, ProjectReportSummary, ProjectVersionAnalysis } from '../../api/analyticsApi';

// ---------------------------------------------------------------------------
// Изолированная dev-демонстрация РЕАЛЬНОЙ вкладки «Отчёт» (ReportsTab) на
// примерных данных. Backend в текущей среде недоступен (нет SQL Server), поэтому
// здесь временно подменяется адаптер общего axios-инстанса apiClient: запросы к
// эндпоинтам отчёта перехватываются и возвращают мок-данные того же формата, что
// и настоящий backend DTO. Сам компонент ReportsTab и все его дочерние
// компоненты — production-код без изменений.
// ---------------------------------------------------------------------------

const MOCK_PROJECT: Project = {
  id: 1,
  code: 'ГПП-110/10-2026',
  name: 'Реконструкция ГПП-110/10 кВ подстанции №4',
  description: 'Демо-проект для предпросмотра вкладки «Отчёт»',
  workNumber: '2026-014',
  contractNumber: 'К-2026/014',
  calendarPlanNumber: null,
  type: 'Реконструкция',
  customer: 'ПАО «Энергосеть»',
  contractor: 'ООО «ИнжПроект»',
  gip: 'Смирнов А. В.',
  workStatus: 'В работе',
  workStart: '2026-02-01',
  program: null,
  versionCount: 3,
  isArchived: false,
};

const PLAN_VERSION: ProjectVersion = {
  id: 10,
  projectId: 1,
  name: 'plan-v3',
  versionName: 'План v3 — Основная',
  description: null,
  direction: 1,
  physicalVolumeTypeId: null,
  sortOrder: 1,
  isArchived: false,
  createdAt: '2026-04-18T00:00:00Z',
  isMain: true,
};

const FACT_VERSION: ProjectVersion = {
  id: 11,
  projectId: 1,
  name: 'fact-v5',
  versionName: 'Факт v5 — Основная',
  description: null,
  direction: 2,
  physicalVolumeTypeId: null,
  sortOrder: 2,
  isArchived: false,
  createdAt: '2026-07-21T00:00:00Z',
  isMain: true,
};

const OLD_PLAN_VERSION: ProjectVersion = {
  id: 9,
  projectId: 1,
  name: 'plan-v1',
  versionName: 'План v1',
  description: null,
  direction: 1,
  physicalVolumeTypeId: null,
  sortOrder: 0,
  isArchived: false,
  createdAt: '2026-02-10T00:00:00Z',
  isMain: false,
};

const MOCK_VERSIONS: ProjectVersion[] = [OLD_PLAN_VERSION, PLAN_VERSION, FACT_VERSION];

// Плоский список направлений работ (дерево строится компонентом buildTree из parentId).
const PERFORMANCE_ROWS: PerformanceRow[] = [
  {
    workDirectionId: 1,
    workDirectionName: 'Электротехнические работы',
    workDirectionCode: '01',
    parentId: null,
    level: 0,
    normUnitName: null,
    factPhysicalVolume: null,
    factLaborHours: 9600,
    factProductivity: null,
    factEstimate: null,
    normProductivity: null,
    normEstimate: null,
    normStdDev: null,
    zScore: -0.42,
    interpretation: 'Good',
    subtreeHours: 9600,
  },
  {
    workDirectionId: 11,
    workDirectionName: 'Монтаж КРУЭ 110 кВ',
    workDirectionCode: '01.01',
    parentId: 1,
    level: 1,
    normUnitName: 'яч.',
    factPhysicalVolume: 12,
    factLaborHours: 5200,
    factProductivity: 433.3,
    factEstimate: 6.07,
    normProductivity: 410,
    normEstimate: 6.02,
    normStdDev: 0.18,
    zScore: -0.28,
    interpretation: 'Good',
    subtreeHours: 5200,
  },
  {
    workDirectionId: 12,
    workDirectionName: 'Прокладка кабельных линий 10 кВ',
    workDirectionCode: '01.02',
    parentId: 1,
    level: 1,
    normUnitName: 'км',
    factPhysicalVolume: 8.4,
    factLaborHours: 4400,
    factProductivity: 523.8,
    factEstimate: 6.26,
    normProductivity: 610,
    normEstimate: 6.41,
    normStdDev: 0.22,
    zScore: -0.68,
    interpretation: 'Good',
    subtreeHours: 4400,
  },
  {
    workDirectionId: 2,
    workDirectionName: 'Строительные работы',
    workDirectionCode: '02',
    parentId: null,
    level: 0,
    normUnitName: null,
    factPhysicalVolume: null,
    factLaborHours: 5400,
    factProductivity: null,
    factEstimate: null,
    normProductivity: null,
    normEstimate: null,
    normStdDev: null,
    zScore: 0.35,
    interpretation: 'Satisfactory',
    subtreeHours: 5400,
  },
  {
    workDirectionId: 21,
    workDirectionName: 'Устройство фундаментов ОРУ',
    workDirectionCode: '02.01',
    parentId: 2,
    level: 1,
    normUnitName: 'м³',
    factPhysicalVolume: 340,
    factLaborHours: 3100,
    factProductivity: 9.12,
    factEstimate: 2.21,
    normProductivity: 8.4,
    normEstimate: 2.13,
    normStdDev: 0.14,
    zScore: 0.57,
    interpretation: 'NeedsAttention',
    subtreeHours: 3100,
  },
  {
    workDirectionId: 22,
    workDirectionName: 'Ограждение и благоустройство',
    workDirectionCode: '02.02',
    parentId: 2,
    level: 1,
    normUnitName: 'м²',
    factPhysicalVolume: 1200,
    factLaborHours: 2300,
    factProductivity: 1.92,
    factEstimate: 0.65,
    normProductivity: 2.1,
    normEstimate: 0.74,
    normStdDev: 0.16,
    zScore: -0.56,
    interpretation: 'Good',
    subtreeHours: 2300,
  },
  {
    workDirectionId: 3,
    workDirectionName: 'Пусконаладочные работы',
    workDirectionCode: '03',
    parentId: null,
    level: 0,
    normUnitName: null,
    factPhysicalVolume: null,
    factLaborHours: 1980,
    factProductivity: null,
    factEstimate: null,
    normProductivity: null,
    normEstimate: null,
    normStdDev: null,
    zScore: 1.24,
    interpretation: 'Critical',
    subtreeHours: 1980,
  },
  {
    workDirectionId: 31,
    workDirectionName: 'ПНР систем РЗА',
    workDirectionCode: '03.01',
    parentId: 3,
    level: 1,
    normUnitName: 'комплект',
    factPhysicalVolume: 18,
    factLaborHours: 1980,
    factProductivity: 110,
    factEstimate: 4.7,
    normProductivity: 78,
    normEstimate: 4.36,
    normStdDev: 0.27,
    zScore: 1.24,
    interpretation: 'Critical',
    subtreeHours: 1980,
  },
];

const TOTAL_HOURS = PERFORMANCE_ROWS.filter((r) => r.parentId === null).reduce(
  (s, r) => s + (r.subtreeHours ?? 0),
  0,
);

function buildAnalysis(weightedZScore: number, totalHours: number): ProjectVersionAnalysis {
  return {
    projectName: MOCK_PROJECT.name,
    projectCode: MOCK_PROJECT.code,
    versionName: FACT_VERSION.versionName,
    directions: [],
    laborIntensities: [],
    summary: {
      totalFactLaborHours: totalHours,
      weightedZScore,
      weightedZScorePercentile: `${Math.round(((Math.min(1, Math.max(-1, weightedZScore)) + 1) / 2) * 100)}-й`,
      weightedZScoreInterpretation: null,
      physicalVolumeTotalsByUnit: [
        { unitName: 'яч.', totalVolume: 12 },
        { unitName: 'км', totalVolume: 8.4 },
        { unitName: 'м³', totalVolume: 340 },
        { unitName: 'м²', totalVolume: 1200 },
        { unitName: 'комплект', totalVolume: 18 },
      ],
      unallocatedLaborHours: 120,
    },
  };
}

const REPORT_SUMMARY: ProjectReportSummary = {
  planVersionId: PLAN_VERSION.id,
  planVersionName: PLAN_VERSION.versionName,
  planHours: 18240,
  planWeightedZScore: -0.18,
  planPercentile: '41-й',
  planInterpretation: 'Good',
  factVersionId: FACT_VERSION.id,
  factVersionName: FACT_VERSION.versionName,
  factHours: TOTAL_HOURS,
  factWeightedZScore: -0.34,
  factPercentile: '33-й',
  factInterpretation: 'Good',
};

function mockResponse<T>(data: T, config: Parameters<AxiosAdapter>[0]): AxiosResponse<T> {
  return {
    data,
    status: 200,
    statusText: 'OK',
    headers: {},
    config,
  } as AxiosResponse<T>;
}

function installMockAdapter(): AxiosAdapter {
  return async (config) => {
    const url = config.url ?? '';

    if (/\/versions\/\d+\/performance$/.test(url)) {
      return mockResponse(PERFORMANCE_ROWS, config);
    }
    if (/\/versions\/11\/analysis$/.test(url) || /\/versions\/10\/analysis$/.test(url)) {
      const isFact = /\/versions\/11\//.test(url);
      return mockResponse(buildAnalysis(isFact ? -0.34 : -0.18, isFact ? TOTAL_HOURS : 18240), config);
    }
    if (/\/versions\/9\/analysis$/.test(url)) {
      return mockResponse(buildAnalysis(0.42, 15800), config);
    }
    if (/\/versions\/\d+\/analysis$/.test(url)) {
      return mockResponse(buildAnalysis(-0.2, 16000), config);
    }
    if (/\/versions\/\d+\/forecast$/.test(url)) {
      return mockResponse(
        {
          projectName: MOCK_PROJECT.name,
          projectCode: MOCK_PROJECT.code ?? '',
          versionName: FACT_VERSION.versionName,
          projectCharacteristics: {},
          plannedVolumes: [],
          scenarios: [],
          top3Disciplines: [],
          interpretations: [],
        },
        config,
      );
    }
    if (/\/characteristics$/.test(url)) {
      return mockResponse(
        [
          {
            characteristicId: 1,
            characteristicName: 'Класс напряжения',
            selectedOptionId: 2,
            selectedOptionName: '110 кВ',
            selectedValuePercent: 92,
            options: [],
          },
          {
            characteristicId: 2,
            characteristicName: 'Тип объекта',
            selectedOptionId: 5,
            selectedOptionName: 'Подстанция',
            selectedValuePercent: 88,
            options: [],
          },
          {
            characteristicId: 3,
            characteristicName: 'Регион строительства',
            selectedOptionId: 9,
            selectedOptionName: 'Северо-Запад',
            selectedValuePercent: 76,
            options: [],
          },
        ],
        config,
      );
    }
    if (/\/report-summary$/.test(url)) {
      return mockResponse(REPORT_SUMMARY, config);
    }

    return mockResponse(null, config);
  };
}

export default function ReportsTabLiveDemo() {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const previousAdapter = apiClient.defaults.adapter;
    apiClient.defaults.adapter = installMockAdapter();
    setReady(true);
    return () => {
      apiClient.defaults.adapter = previousAdapter;
    };
  }, []);

  if (!ready) return null;

  return (
    <Box sx={{ position: 'fixed', inset: 0, overflowY: 'auto', overflowX: 'hidden', bgcolor: '#F5F6F8', py: 3 }}>
      <Box sx={{ maxWidth: 1400, mx: 'auto', px: { xs: 2, md: 3 } }}>
        <Typography variant="caption" sx={{ display: 'block', color: 'text.secondary', mb: 2 }}>
          Демо-режим: данные подставлены локально (backend недоступен в этой среде). Компонент ReportsTab — без
          изменений, из продакшен-кода.
        </Typography>
        <ReportsTab projectId={1} versionId={FACT_VERSION.id} project={MOCK_PROJECT} versions={MOCK_VERSIONS} />
      </Box>
    </Box>
  );
}
