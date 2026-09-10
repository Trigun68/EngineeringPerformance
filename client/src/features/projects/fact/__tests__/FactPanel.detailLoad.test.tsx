import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import type { ReactElement } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ProjectVersion } from '../../../../api/projectVersionsApi';
import type { ProjectsPageData, ProjectsPageDetailRow } from '../../types';
import { FactPanel } from '../FactPanel';

const fetchProjectsPageData = vi.fn();
const fetchProjectsPageDetails = vi.fn();

vi.mock('../../../../api/projectsPageApi', () => ({
  fetchProjectsPageData: (...args: unknown[]) => fetchProjectsPageData(...args),
  fetchProjectsPageDetails: (...args: unknown[]) => fetchProjectsPageDetails(...args),
  exportFactDetailExcel: vi.fn(),
  exportFactDetailExcelVisible: vi.fn(),
  exportFactDetailSummaryTable: vi.fn(),
  exportFactFacilityExcel: vi.fn(),
  exportFactSummaryExcel: vi.fn(),
}));

vi.mock('../../../../api/visualReportsApi', () => ({
  exportFactFacilityPdf: vi.fn(),
  exportFactSummaryPdf: vi.fn(),
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
      summary: [],
      facility: [],
      detail: [],
      grandTotals: { baseHours: 0, optimisticHours: 0, pessimisticHours: 0 },
    },
    fact: {
      performance: [],
      facility: [],
      detail: [],
    },
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
    direction: 2,
    sortOrder: 0,
    isArchived: false,
    isPrimary: true,
  },
  {
    id: 2,
    projectId: 1,
    name: '30.06.26',
    versionName: '30.06.26',
    direction: 2,
    sortOrder: 1,
    isArchived: false,
    isPrimary: false,
  },
];

function createTestQueryClient(): QueryClient {
  return new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0,
        staleTime: 0,
        refetchOnWindowFocus: false,
      },
    },
  });
}

function renderFact(ui: ReactElement, client = createTestQueryClient()) {
  return {
    client,
    ...render(<QueryClientProvider client={client}>{ui}</QueryClientProvider>),
  };
}

function factPanelProps(versionId: number, label: string) {
  return {
    projectId: 1,
    projectName: 'Проект',
    projectCode: '25-310',
    workspaceVersions: versions,
    workspaceFactVersionId: versionId,
    workspaceFactVersionLabel: label,
  };
}

describe('FactPanel detail load', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getUserPreferences.mockResolvedValue({});
    fetchProjectsPageData.mockResolvedValue(emptyPageData());
    fetchProjectsPageDetails.mockResolvedValue({
      forecastDetail: [],
      factDetail: [detailRow],
      compareDetail: null,
    });
  });

  it('does not fetch detail until the Детально tab is selected', async () => {
    renderFact(<FactPanel {...factPanelProps(2, '30.06.26')} />);

    await waitFor(() => {
      expect(fetchProjectsPageData).toHaveBeenCalledWith(1, 2, undefined, expect.any(AbortSignal));
    });

    expect(fetchProjectsPageDetails).not.toHaveBeenCalled();
    expect(screen.getByTestId('fact-performance')).toBeInTheDocument();

    await fireEvent.click(screen.getByRole('tab', { name: 'Детально' }));

    await waitFor(() => {
      expect(fetchProjectsPageDetails).toHaveBeenCalledWith(1, 2, undefined, expect.any(AbortSignal));
    });

    await waitFor(() => {
      expect(screen.getByTestId('fact-detail')).toHaveAttribute('data-rows', '1');
    });
  });

  it('fills detail grid when prefs open Детально and switch fact version', async () => {
    getUserPreferences.mockImplementation(async () => {
      await delay(20);
      return {
        'projects.1.factSubTab': 'fact-detail',
        'projects.1.factVersionId': '2',
      };
    });
    fetchProjectsPageData.mockImplementation(async (_projectId: number, versionId: number) => {
      await delay(versionId === 7176 ? 40 : 50);
      return emptyPageData();
    });
    fetchProjectsPageDetails.mockImplementation(async () => {
      await delay(80);
      return {
        forecastDetail: [],
        factDetail: [detailRow],
        compareDetail: null,
      };
    });

    renderFact(<FactPanel {...factPanelProps(7176, '27.07.26')} />);

    await waitFor(
      () => {
        expect(screen.getByTestId('fact-detail')).toHaveAttribute('data-rows', '1');
      },
      { timeout: 4000 },
    );

    expect(fetchProjectsPageDetails).toHaveBeenCalledWith(1, 2, undefined, expect.any(AbortSignal));
  });

  it('aborts in-flight detail fetch when fact version changes', async () => {
    getUserPreferences.mockResolvedValue({
      'projects.1.factSubTab': 'fact-detail',
    });
    const signals: AbortSignal[] = [];
    fetchProjectsPageDetails.mockImplementation(
      (_projectId: number, _versionId: number, _compare: unknown, signal?: AbortSignal) => {
        if (signal) signals.push(signal);
        return delay(400).then(() => ({
          forecastDetail: [],
          factDetail: [detailRow],
          compareDetail: null,
        }));
      },
    );

    const client = createTestQueryClient();
    const { rerender } = renderFact(<FactPanel {...factPanelProps(2, '30.06.26')} />, client);

    await waitFor(() => {
      expect(fetchProjectsPageDetails).toHaveBeenCalled();
    });

    rerender(
      <QueryClientProvider client={client}>
        <FactPanel {...factPanelProps(7176, '27.07.26')} />
      </QueryClientProvider>,
    );

    await waitFor(() => {
      expect(signals.some((s) => s.aborted)).toBe(true);
    });
  });

  it('shows page-data error via getApiErrorMessage fallback', async () => {
    fetchProjectsPageData.mockRejectedValue(new Error('network'));

    renderFact(<FactPanel {...factPanelProps(2, '30.06.26')} />);

    expect(
      await screen.findByText('Не удалось загрузить данные оценки факта.'),
    ).toBeInTheDocument();
  });
});
