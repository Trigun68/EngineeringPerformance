import { render, screen, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import type { ProjectVersion } from '../../../../api/projectVersionsApi';
import type { VersionCompareDetailRow, VersionCompareResult } from '../../../../api/analyticsApi';
import { ComparePanel } from '../ComparePanel';

const fetchVersionCompare = vi.fn();
const fetchVersionCompareDetail = vi.fn();

vi.mock('../../../../api/analyticsApi', () => ({
  fetchVersionCompare: (...args: unknown[]) => fetchVersionCompare(...args),
  fetchVersionCompareDetail: (...args: unknown[]) => fetchVersionCompareDetail(...args),
  exportCompareDetailExcel: vi.fn(),
  exportCompareDetailSummaryTable: vi.fn(),
  exportCompareExcelVisible: vi.fn(),
}));

vi.mock('../../../../api/visualReportsApi', () => ({
  exportComparePdf: vi.fn(),
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

function emptyCompare(): VersionCompareResult {
  return {
    summary: [
      {
        workDirectionId: 1,
        parentId: null,
        name: 'Направление А',
        factZScoreA: 1,
        factZScoreB: 1.1,
        factZDelta: 0.1,
        interpretationA: '',
        interpretationB: '',
      },
    ],
    facility: [],
    detail: [],
  };
}

const detailRow: VersionCompareDetailRow = {
  facilityCode: 'F1',
  facilityName: 'Объект 1',
  documentSet: 'DS',
  pvCode: '01',
  pvName: 'Работа',
  workId: 'W1',
  workName: 'Монтаж',
  quantityA: 1,
  quantityB: 1,
  quantityDelta: 0,
  plannedLaborHoursA: 10,
  plannedLaborHoursB: 12,
  plannedLaborHoursOptimisticA: 8,
  plannedLaborHoursOptimisticB: 9,
  plannedLaborHoursPessimisticA: 12,
  plannedLaborHoursPessimisticB: 14,
  factLaborHoursA: 10,
  factLaborHoursB: 11,
  hoursDelta: 1,
  plannedProductivityA: 1,
  plannedProductivityB: 1,
  plannedProductivityOptimisticA: 1,
  plannedProductivityOptimisticB: 1,
  plannedProductivityPessimisticA: 1,
  plannedProductivityPessimisticB: 1,
  factProductivityA: 1,
  factProductivityB: 1,
  productivityDelta: 0,
  zScoreA: 0,
  zScoreB: 0,
  zDelta: 0,
  iicGroupName: 'A',
  iicUnitName: 'шт',
  presence: 'A+B',
  bindingType: '',
  catalogId: '',
  tag: '',
  itemName: 'Работа',
  workComment: '',
  estimationBasis: '',
  isAssessment: '',
  coefficient: 1,
  normProductivity: 1,
  normStdDev: 0,
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
  {
    id: 3013,
    projectId: 1,
    name: '30.06.26 (факт)',
    versionName: '30.06.26 (факт)',
    direction: 2,
    sortOrder: 2,
    isArchived: false,
    isPrimary: true,
  },
];

describe('ComparePanel detail load', () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getUserPreferences.mockImplementation(async () => {
      await delay(20);
      return {
        'projects.1.compareSubTab': 'compare-detail',
        'projects.1.compareVersionAId': '2',
        'projects.1.compareVersionBId': '3013',
      };
    });
    fetchVersionCompare.mockImplementation(async () => {
      await delay(40);
      return emptyCompare();
    });
    fetchVersionCompareDetail.mockImplementation(async () => {
      await delay(80);
      return [detailRow];
    });
  });

  it('fills detail grid when prefs switch versions while Детально is open', async () => {
    render(
      <ComparePanel
        projectId={1}
        workspaceVersions={versions}
        workspaceCompareVersionAId={7176}
        workspaceCompareVersionBId={3013}
      />,
    );

    await waitFor(
      () => {
        expect(screen.getByTestId('compare-detail')).toHaveAttribute('data-rows', '1');
      },
      { timeout: 4000 },
    );

    expect(fetchVersionCompareDetail).toHaveBeenCalledWith(1, 2, 3013, expect.any(AbortSignal));
  });

  it('aborts in-flight detail fetch when version A or B changes', async () => {
    getUserPreferences.mockResolvedValue({
      'projects.1.compareSubTab': 'compare-detail',
    });
    fetchVersionCompare.mockResolvedValue(emptyCompare());
    const signals: AbortSignal[] = [];
    fetchVersionCompareDetail.mockImplementation(
      (_projectId: number, _a: number, _b: number, signal?: AbortSignal) => {
        if (signal) signals.push(signal);
        return delay(400).then(() => [detailRow]);
      },
    );

    const { rerender } = render(
      <ComparePanel
        projectId={1}
        workspaceVersions={versions}
        workspaceCompareVersionAId={2}
        workspaceCompareVersionBId={3013}
      />,
    );

    await waitFor(() => {
      expect(fetchVersionCompareDetail).toHaveBeenCalled();
    });

    rerender(
      <ComparePanel
        projectId={1}
        workspaceVersions={versions}
        workspaceCompareVersionAId={7176}
        workspaceCompareVersionBId={3013}
      />,
    );

    await waitFor(() => {
      expect(signals.some((s) => s.aborted)).toBe(true);
    });
  });

  it('does not abort same-pair in-flight detail fetch', async () => {
    getUserPreferences.mockResolvedValue({
      'projects.1.compareSubTab': 'compare-detail',
    });
    fetchVersionCompare.mockResolvedValue(emptyCompare());
    const signals: AbortSignal[] = [];
    fetchVersionCompareDetail.mockImplementation(
      (_projectId: number, _a: number, _b: number, signal?: AbortSignal) => {
        if (signal) signals.push(signal);
        return delay(250).then(() => [detailRow]);
      },
    );

    const { rerender } = render(
      <ComparePanel
        projectId={1}
        workspaceVersions={versions}
        workspaceCompareVersionAId={7176}
        workspaceCompareVersionBId={3013}
      />,
    );

    await waitFor(() => {
      expect(fetchVersionCompareDetail).toHaveBeenCalled();
    });
    const firstSignal = signals[0];
    expect(firstSignal).toBeDefined();

    rerender(
      <ComparePanel
        projectId={1}
        workspaceVersions={[...versions]}
        workspaceCompareVersionAId={7176}
        workspaceCompareVersionBId={3013}
      />,
    );

    await delay(30);
    expect(firstSignal!.aborted).toBe(false);

    await waitFor(
      () => {
        expect(screen.getByTestId('compare-detail')).toHaveAttribute('data-rows', '1');
      },
      { timeout: 4000 },
    );
  });

  it('hides planned-hours chip on Детально until detail rows arrive', async () => {
    let releaseDetail: (rows: VersionCompareDetailRow[]) => void = () => undefined;
    fetchVersionCompareDetail.mockImplementation(
      () => new Promise<VersionCompareDetailRow[]>((resolve) => {
        releaseDetail = resolve;
      }),
    );

    render(
      <ComparePanel
        projectId={1}
        workspaceVersions={versions}
        workspaceCompareVersionAId={7176}
        workspaceCompareVersionBId={3013}
      />,
    );

    await waitFor(() => {
      expect(fetchVersionCompareDetail).toHaveBeenCalled();
    });
    expect(screen.queryByTestId('compare-version-differences')).not.toBeInTheDocument();

    releaseDetail([detailRow]);

    await waitFor(() => {
      expect(screen.getByTestId('compare-version-differences')).toHaveTextContent(
        'Различия версий: 2,00 ч',
      );
    });
  });
});
