import { useCallback, useState, type RefObject } from 'react';
import type { GridApi } from 'ag-grid-community';
import { exportFactFacilityPdf, exportFactSummaryPdf } from '../../../api/visualReportsApi';
import {
  exportFactDetailExcel,
  exportFactDetailExcelVisible,
  exportFactDetailSummaryTable,
  exportFactFacilityExcel,
  exportFactSummaryExcel,
} from '../../../api/projectsPageApi';
import { getApiErrorMessage } from '../../../services/apiError';
import { collectGridExportData } from '../../../utils/directoryExport';

export interface UseFactExportParams {
  projectId: number;
  factVersionId: number | null;
  detailGridApiRef: RefObject<GridApi | null>;
  setError: (message: string | null) => void;
}

export interface UseFactExportResult {
  exporting: boolean;
  handleExportSummary: (type: 'excel' | 'pdf') => Promise<void>;
  handleExportFacility: (type: 'excel' | 'pdf') => Promise<void>;
  handleExportDetail: () => Promise<void>;
  handleExportDetailVisibleExcel: () => Promise<void>;
  handleExportDetailSummaryTable: () => Promise<void>;
}

/** Выгрузки факта: состояние overlay и обработчики без UI. */
export function useFactExport({
  projectId,
  factVersionId,
  detailGridApiRef,
  setError,
}: UseFactExportParams): UseFactExportResult {
  const [exporting, setExporting] = useState(false);

  const runExport = useCallback(
    async (action: (versionId: number) => Promise<void>, fallback: string) => {
      if (!factVersionId) return;
      setExporting(true);
      setError(null);
      try {
        await action(factVersionId);
      } catch (err) {
        setError(getApiErrorMessage(err, fallback));
      } finally {
        setExporting(false);
      }
    },
    [factVersionId, setError],
  );

  const handleExportSummary = useCallback(
    async (type: 'excel' | 'pdf') => {
      await runExport(
        (versionId) =>
          type === 'excel'
            ? exportFactSummaryExcel(projectId, versionId)
            : exportFactSummaryPdf(projectId, versionId),
        'Не удалось выполнить экспорт.',
      );
    },
    [projectId, runExport],
  );

  const handleExportFacility = useCallback(
    async (type: 'excel' | 'pdf') => {
      await runExport(
        (versionId) =>
          type === 'excel'
            ? exportFactFacilityExcel(projectId, versionId)
            : exportFactFacilityPdf(projectId, versionId),
        'Не удалось выполнить экспорт.',
      );
    },
    [projectId, runExport],
  );

  const handleExportDetail = useCallback(async () => {
    await runExport(
      (versionId) => exportFactDetailExcel(projectId, versionId),
      'Не удалось выполнить экспорт.',
    );
  }, [projectId, runExport]);

  const handleExportDetailVisibleExcel = useCallback(async () => {
    if (!factVersionId) return;
    const api = detailGridApiRef.current;
    if (!api) {
      setError('Таблица ещё не загружена.');
      return;
    }
    const { headers, rows: exportRows } = collectGridExportData(api, { includeGroups: true });
    if (headers.length === 0) {
      setError('Нет видимых столбцов для экспорта.');
      return;
    }
    if (exportRows.length === 0) {
      setError('Нет строк для экспорта (проверьте фильтры таблицы).');
      return;
    }
    await runExport(
      (versionId) =>
        exportFactDetailExcelVisible(
          projectId,
          versionId,
          'Факт_Детально_выделенные',
          headers,
          exportRows,
        ),
      'Не удалось выполнить экспорт выделенных данных.',
    );
  }, [detailGridApiRef, factVersionId, projectId, runExport, setError]);

  const handleExportDetailSummaryTable = useCallback(async () => {
    await runExport(
      (versionId) => exportFactDetailSummaryTable(projectId, versionId),
      'Не удалось выгрузить сводную таблицу.',
    );
  }, [projectId, runExport]);

  return {
    exporting,
    handleExportSummary,
    handleExportFacility,
    handleExportDetail,
    handleExportDetailVisibleExcel,
    handleExportDetailSummaryTable,
  };
}
