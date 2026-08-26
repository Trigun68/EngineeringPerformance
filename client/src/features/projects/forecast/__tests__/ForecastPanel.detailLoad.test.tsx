import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ProjectVersion } from '../../../../api/projectVersionsApi';
import type { ProjectsPageData, ProjectsPageDetailRow } from '../../types';
import { ForecastPanel } from '../ForecastPanel';

const fetchProjectsPageData = vi.fn();
const fetchProjectsPageDetails = vi.fn();

vi.mock('../../../../api/projectsPageApi', () => ({
  fetchProjectsPageData: (...args: unknown[]) => fetchProjectsPageData(...args),
  fetchProjectsPageDetails: (...args: unknown[]) => fetchProjectsPageDetails(...args),
  exportForecastDetailExcel: vi.fn(),
  exportForecastDetailExcelVisible: vi.fn(),
  exportForecastDetailPdf: vi.fn(),
  exportForecastDetailSummaryTable: vi.fn(),
  exportForecastFacilityExcel: vi.fn(),
  exportForecastFacilityPdf: vi.fn(),
  exportForecastSummaryExcel: vi.fn(),
  exportForecastSummaryPdf: vi.fn(),
}));

vi.mock('../../../../api/projectDataApi', () => ({
  deleteEquipment: vi.fn(),
  deletePhysicalVolume: vi.fn(),
  createEquipment: vi.fn(),
  createPhysicalVolume: vi.fn(),
  fetchEquipmentById: vi.fn(),
  fetchPhysicalVolumeById: vi.fn(),
}));

const getUserPreferences = vi.fn();
vi.mock('../../../../api/userPreferencesApi', () => ({
  getUserPreferences: (...args: unknown[]) => getUserPreferences(...args),
  putUserPreferences: vi.fn().mockResolvedValue(undefined),
}));

vi.mock('../../../../hooks/usePermission', () => ({
  usePermission: () => ({
    canRead: true,
    hasAccess: true,
    hasLevel: () => true,
    level: 2,
    canEdit: true,
    isAdmin: true,
  }),
}));

vi.mock('../../../../store/useDictionaryStore', () => ({
  useDictionaryStore: (selector: (s: { lists: { physicalVolumeTypes: unknown[] } }) => unknown) =>
    selector({ lists: { physicalVolumeTypes: [] } }),
}));

vi.mock('../../../../components/DataTable/DataTable', () => ({
  DataTable: ({
    rowData,
    loading,
    gridId,
  }: {
    rowData: unknown[];
    loading?: boolean;
    gridId?: string;
  }) => (
    <div
      data-testid={gridId ?? 'data-table'}
      data-loading={loading ? 'true' : 'false'}
      data-rows={rowData.length}
    />
  ),
}));

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => {
    setTimeout(resolve, ms);
  });
}

function emptyPageData(): ProjectsPageData {
  return {
    forecast: {
      summary: [
        {
          workDirectionId: 1,
          displayName: 'Направление А',
          normUnitName: 'шт',
          factPhysicalVolume: 1,
          normProductivity: 1,
          baseHours: 10,
          optimisticHours: 8,
          pessimisticHours: 12,
          isTotal: false,
          level: 0,
          code: 'A',
        },
      ],
      facility: [],
      detail: [],
      grandTotals: { baseHours: 10, optimisticHours: 8, pessimisticHours: 12 },
    },
    fact: { performance: [], facility: [], detail: [] },
    compare: null,
  };
}

const detailRow: ProjectsPageDetailRow = {
  rowId: 1,
  facilityCode: 'F1',
  facilityName: 'Объект 1',
  documentSet: 'DS',
  catalogCode: '01',
  catalogName: 'Работа',
  workId: 'W1',
  workName: 'Монтаж',
  iicGroupName: 'A',
  iicUnitName: 'шт',
  quantity: 1,
  baseHours: 10,
  optimisticHours: 8,
  pessimisticHours: 12,
  factLaborHours: null,
  factProductivity: null,
  zScore: null,
  interpretation: '',
  isEquipment: false,
};

const versions: ProjectVersion[] = [
  {
    id: 7176,
    projectId: 1,
    name: '27.07.26',
    versionName: '27.07.26',
    direction: 1,
    sortOrder: 0,
    isArchived: false,
    isPrimary: true,
  },
  {
    id: 2,
    projectId: 1,
    name: '30.06.26',
    versionName: '30.06.26',
    direction: 1,
    sortOrder: 1,
    isArchived: false,
    isPrimary: false,
  },
];

describe('ForecastPanel detail load', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getUserPreferences.mockImplementation(async () => {
      await delay(20);
      return {
        'projects.1.forecastSubTab': 'forecast-detail',
        'projects.1.planVersionId': '2',
      };
    });
    fetchProjectsPageData.mockImplementation(async (_projectId: number, versionId: number) => {
      await delay(versionId === 7176 ? 40 : 50);
      return emptyPageData();
    });
    fetchProjectsPageDetails.mockImplementation(async () => {
      await delay(80);
      return {
        forecastDetail: [detailRow],
        factDetail: [],
        compareDetail: null,
      };
    });
  });

  it('fills detail grid when prefs switch plan version while Детально is open', async () => {
    render(
      <ForecastPanel
        projectId={1}
        projectName="Проект"
        projectCode="25-310"
        workspaceVersions={versions}
        workspacePlanVersionId={7176}
        workspacePlanVersionLabel="27.07.26"
      />,
    );

    await waitFor(
      () => {
        expect(screen.getByTestId('forecast_details_columns')).toHaveAttribute('data-rows', '1');
      },
      { timeout: 4000 },
    );

    expect(fetchProjectsPageDetails).toHaveBeenCalledWith(1, 2, undefined, expect.any(AbortSignal));
  });

  it('aborts in-flight detail fetch when plan version changes', async () => {
    getUserPreferences.mockResolvedValue({
      'projects.1.forecastSubTab': 'forecast-detail',
    });
    fetchProjectsPageData.mockResolvedValue(emptyPageData());
    const signals: AbortSignal[] = [];
    fetchProjectsPageDetails.mockImplementation(
      (_projectId: number, _versionId: number, _compare: unknown, signal?: AbortSignal) => {
        if (signal) signals.push(signal);
        return delay(400).then(() => ({
          forecastDetail: [detailRow],
          factDetail: [],
          compareDetail: null,
        }));
      },
    );

    const { rerender } = render(
      <ForecastPanel
        projectId={1}
        projectName="Проект"
        projectCode="25-310"
        workspaceVersions={versions}
        workspacePlanVersionId={2}
        workspacePlanVersionLabel="30.06.26"
      />,
    );

    await waitFor(() => {
      expect(fetchProjectsPageDetails).toHaveBeenCalled();
    });

    rerender(
      <ForecastPanel
        projectId={1}
        projectName="Проект"
        projectCode="25-310"
        workspaceVersions={versions}
        workspacePlanVersionId={7176}
        workspacePlanVersionLabel="27.07.26"
      />,
    );

    await waitFor(() => {
      expect(signals.some((s) => s.aborted)).toBe(true);
    });
  });
});
