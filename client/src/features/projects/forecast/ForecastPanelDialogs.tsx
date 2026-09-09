import type { ReactNode } from 'react';
import type { ImportDataType } from '../../../api/importApi';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import {
  ANALYSIS_PLAN,
  ProjectVersionPickerDialog,
} from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import type { ProjectsPageForecastSummaryRow } from '../types';
import { DataImportDialog } from './DataImportDialog';
import { ForecastReportDialog } from './ForecastReportDialog';
import { IterationDeleteDialog } from './IterationDeleteDialog';
import { PlanningParametersDialog } from './PlanningParametersDialog';

export type ForecastImportDialogType = Extract<ImportDataType, 'physical-volumes' | 'equipment'> | null;

export interface ForecastPanelDialogsProps {
  projectId: number;
  projectName: string;
  projectCode: string;
  workspaceVersions: ProjectVersion[];
  planVersionId: number | null;
  planVersionLabel: string;
  summaryRows: ProjectsPageForecastSummaryRow[];
  versionPickerOpen: boolean;
  reportOpen: boolean;
  planningOpen: boolean;
  importDialogType: ForecastImportDialogType;
  deleteDialogType: ForecastImportDialogType;
  onVersionSelect: (version: ProjectVersion) => void;
  onCloseVersionPicker: () => void;
  onCloseReport: () => void;
  onClosePlanning: () => void;
  onCloseImport: () => void;
  onCloseDelete: () => void;
  onReload: () => void;
  contextDialogs: ReactNode;
  confirmDialog: ReactNode;
}

/** Диалоги панели прогноза: версия, отчёт, параметры, импорт/удаление, контекстное меню. */
export function ForecastPanelDialogs({
  projectId,
  projectName,
  projectCode,
  workspaceVersions,
  planVersionId,
  planVersionLabel,
  summaryRows,
  versionPickerOpen,
  reportOpen,
  planningOpen,
  importDialogType,
  deleteDialogType,
  onVersionSelect,
  onCloseVersionPicker,
  onCloseReport,
  onClosePlanning,
  onCloseImport,
  onCloseDelete,
  onReload,
  contextDialogs,
  confirmDialog,
}: ForecastPanelDialogsProps) {
  return (
    <>
      <ProjectVersionPickerDialog
        open={versionPickerOpen}
        title="Выбор версии (план)"
        projectId={projectId}
        versions={workspaceVersions}
        directionFilter={ANALYSIS_PLAN}
        selectedVersionId={planVersionId}
        onSelect={onVersionSelect}
        onClose={onCloseVersionPicker}
      />

      <ForecastReportDialog
        open={reportOpen}
        projectId={projectId}
        versionId={planVersionId}
        projectName={projectName}
        projectCode={projectCode}
        versionLabel={planVersionLabel}
        summaryRows={summaryRows}
        onClose={onCloseReport}
      />

      <PlanningParametersDialog
        open={planningOpen}
        projectId={projectId}
        versionId={planVersionId}
        onClose={onClosePlanning}
        onSaved={onReload}
      />

      {importDialogType && planVersionId && (
        <DataImportDialog
          open
          dataType={importDialogType}
          projectId={projectId}
          versionId={planVersionId}
          onClose={onCloseImport}
          onSuccess={onReload}
        />
      )}

      {deleteDialogType && planVersionId && (
        <IterationDeleteDialog
          open
          dataType={deleteDialogType}
          projectId={projectId}
          versionId={planVersionId}
          onClose={onCloseDelete}
          onSuccess={onReload}
        />
      )}

      {contextDialogs}
      {confirmDialog}
    </>
  );
}
