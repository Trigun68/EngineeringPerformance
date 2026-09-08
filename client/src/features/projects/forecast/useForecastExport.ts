import { useCallback, useEffect, useState, type MutableRefObject } from 'react';
import type { GridApi } from 'ag-grid-community';
import {
  exportForecastDetailExcel,
  exportForecastDetailExcelVisible,
  exportForecastDetailSummaryTable,
  exportForecastFacilityExcel,
  exportForecastFacilityPdf,
  exportForecastSummaryExcel,
  exportForecastSummaryPdf,
} from '../../../api/projectsPageApi';
import { getApiErrorMessage } from '../../../services/apiError';
import { collectGridExportData } from '../../../utils/directoryExport';

export interface UseForecastExportParams {
  projectId: number;
  planVersionId: number | null;
  detailGridApiRef: MutableRefObject<GridApi | null>;
}

export interface UseForecastExportResult {
  exporting: boolean;
  exportError: string | null;
  handleExportSummary: (type: 'excel' | 'pdf') => Promise<void>;
  handleExportFacility: (type: 'excel' | 'pdf') => Promise<void>;
  handleExportDetail: () => Promise<void>;
  handleExportDetailVisibleExcel: () => Promise<void>;
  handleExportDetailSummaryTable: () => Promise<void>;
}

/**
 * Обработчики выгрузки прогноза и состояние overlay/ошибки, которым они владеют.
 */
export function useForecastExport({
  projectId,
  planVersionId,
  detailGridApiRef,
}: UseForecastExportParams): UseForecastExportResult {
  const [exporting, setExporting] = useState(false);
  const [exportError, setExportError] = useState<string | null>(null);

  useEffect(() => {
    setExportError(null);
  }, [projectId, planVersionId]);

  const runExport = useCallback(
    async (task: () => Promise<void>, fallback = 'Не удалось выполнить экспорт.') => {
      if (!planVersionId) return;
      setExporting(true);
      setExportError(null);
      try {
        await task();
      } catch (err) {
        setExportError(getApiErrorMessage(err, fallback));
      } finally {
        setExporting(false);
      }
    },
    [planVersionId],
  );

  const handleExportSummary = useCallback(
    async (type: 'excel' | 'pdf') => {
      if (!planVersionId) return;
      await runExport(() =>
        type === 'excel'
          ? exportForecastSummaryExcel(projectId, planVersionId)
          : exportForecastSummaryPdf(projectId, planVersionId),
      );
    },
    [planVersionId, projectId, runExport],
  );

  const handleExportFacility = useCallback(
    async (type: 'excel' | 'pdf') => {
      if (!planVersionId) return;
      await runExport(() =>
        type === 'excel'
          ? exportForecastFacilityExcel(projectId, planVersionId)
          : exportForecastFacilityPdf(projectId, planVersionId),
      );
    },
    [planVersionId, projectId, runExport],
  );

  const handleExportDetail = useCallback(async () => {
    if (!planVersionId) return;
    await runExport(() => exportForecastDetailExcel(projectId, planVersionId));
  }, [planVersionId, projectId, runExport]);

  const handleExportDetailVisibleExcel = useCallback(async () => {
    if (!planVersionId) return;
    const api = detailGridApiRef.current;
    if (!api) {
      setExportError('Таблица ещё не загружена.');
      return;
    }
    const { headers, rows: exportRows } = collectGridExportData(api, { includeGroups: true });
    if (headers.length === 0) {
      setExportError('Нет видимых столбцов для экспорта.');
      return;
    }
    if (exportRows.length === 0) {
      setExportError('Нет строк для экспорта (проверьте фильтры таблицы).');
      return;
    }
    await runExport(
      () =>
        exportForecastDetailExcelVisible(
          projectId,
          planVersionId,
          'Прогноз_Детально_выделенные',
          headers,
          exportRows,
        ),
      'Не удалось выполнить экспорт выделенных данных.',
    );
  }, [detailGridApiRef, planVersionId, projectId, runExport]);

  const handleExportDetailSummaryTable = useCallback(async () => {
    if (!planVersionId) return;
    await runExport(
      () => exportForecastDetailSummaryTable(projectId, planVersionId),
      'Не удалось выполнить экспорт сводной таблицы.',
    );
  }, [planVersionId, projectId, runExport]);

  return {
    exporting,
    exportError,
    handleExportSummary,
    handleExportFacility,
    handleExportDetail,
    handleExportDetailVisibleExcel,
    handleExportDetailSummaryTable,
  };
}
