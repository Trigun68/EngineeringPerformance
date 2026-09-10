import { useCallback, useEffect, useRef, useState } from 'react';
import type { GridApi } from 'ag-grid-community';
import {
  exportCompareDetailExcel,
  exportCompareDetailSummaryTable,
  exportCompareExcelVisible,
  exportCompareFacilityExcel,
  exportCompareSummaryExcel,
  type CompareExportSection,
} from '../../../api/analyticsApi';
import { exportComparePdf, type ComparePdfSection } from '../../../api/visualReportsApi';
import { getApiErrorMessage } from '../../../services/apiError';
import { collectGridExportData } from '../../../utils/directoryExport';

export type CompareSubTab = 'compare-total' | 'compare-facility' | 'compare-detail';

export interface UseCompareExportParams {
  projectId: number;
  versionAId: number | null;
  versionBId: number | null;
  subTab: CompareSubTab;
  showOnlyDifferences: boolean;
}

function subTabToExportSection(subTab: CompareSubTab): CompareExportSection {
  if (subTab === 'compare-facility') return 'facility';
  if (subTab === 'compare-detail') return 'detail';
  return 'summary';
}

/** Экспорт Excel/PDF сравнения: состояние выгрузки и обработчики без запросов страницы. */
export function useCompareExport({
  projectId,
  versionAId,
  versionBId,
  subTab,
  showOnlyDifferences,
}: UseCompareExportParams) {
  const [actionError, setError] = useState<string | null>(null);
  const [exportingPdf, setExportingPdf] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [activeGridReady, setActiveGridReady] = useState(false);
  const gridApiRef = useRef<GridApi | null>(null);

  useEffect(() => {
    gridApiRef.current = null;
    setActiveGridReady(false);
  }, [subTab]);

  useEffect(() => {
    setError(null);
  }, [projectId, versionAId, versionBId]);

  const handleGridApiReady = useCallback((api: GridApi) => {
    gridApiRef.current = api;
    setActiveGridReady(true);
  }, []);

  const handleExportPdf = useCallback(async () => {
    if (!versionAId || !versionBId || subTab === 'compare-detail') return;
    setExportingPdf(true);
    try {
      const section: ComparePdfSection = subTab === 'compare-facility' ? 'facility' : 'summary';
      await exportComparePdf(projectId, versionAId, versionBId, {
        section,
        showOnlyDifferences,
      });
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось экспортировать PDF.'));
    } finally {
      setExportingPdf(false);
    }
  }, [subTab, projectId, showOnlyDifferences, versionAId, versionBId]);

  const handleExportVisibleExcel = useCallback(async () => {
    if (!versionAId || !versionBId) return;
    const api = gridApiRef.current;
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
    const section = subTabToExportSection(subTab);
    const tabLabel =
      section === 'facility' ? 'Сравнение_По_объектам' : section === 'detail' ? 'Сравнение_Детально' : 'Сравнение_Итого';
    setExporting(true);
    setError(null);
    try {
      await exportCompareExcelVisible(
        projectId,
        section,
        `${tabLabel}_выделенные`,
        headers,
        exportRows,
        versionAId,
        versionBId,
        true,
      );
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт выделенных данных.'));
    } finally {
      setExporting(false);
    }
  }, [projectId, subTab, versionAId, versionBId]);

  const handleExportAllExcel = useCallback(async () => {
    if (!versionAId || !versionBId) return;
    setExporting(true);
    setError(null);
    try {
      if (subTab === 'compare-facility') {
        await exportCompareFacilityExcel(projectId, versionAId, versionBId);
      } else if (subTab === 'compare-detail') {
        await exportCompareDetailExcel(projectId, versionAId, versionBId);
      } else {
        await exportCompareSummaryExcel(projectId, versionAId, versionBId);
      }
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт Excel.'));
    } finally {
      setExporting(false);
    }
  }, [projectId, subTab, versionAId, versionBId]);

  const handleExportSummaryTable = useCallback(async () => {
    if (!versionAId || !versionBId) return;
    setExporting(true);
    setError(null);
    try {
      await exportCompareDetailSummaryTable(projectId, versionAId, versionBId);
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось выполнить экспорт сводной таблицы.'));
    } finally {
      setExporting(false);
    }
  }, [projectId, versionAId, versionBId]);

  return {
    actionError,
    setError,
    exporting,
    exportingPdf,
    activeGridReady,
    handleGridApiReady,
    handleExportPdf,
    handleExportVisibleExcel,
    handleExportAllExcel,
    handleExportSummaryTable,
  };
}
