import type { ReactNode } from 'react';
import type { ImportDataType } from '../../../api/importApi';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import {
  ANALYSIS_FACT,
  ProjectVersionPickerDialog,
} from '../../../components/ProjectVersionPickerDialog/ProjectVersionPickerDialog';
import { DataImportDialog } from '../forecast/DataImportDialog';
import { IterationDeleteDialog } from '../forecast/IterationDeleteDialog';
import { PlanningParametersDialog } from '../forecast/PlanningParametersDialog';
import { FactAnalysisDialog } from './FactAnalysisDialog';

export type FactImportDialogType = ImportDataType | null;

export interface FactPanelDialogsProps {
  projectId: number;
  projectName: string;
  projectCode: string;
  workspaceVersions: ProjectVersion[];
  factVersionId: number | null;
  factVersionLabel: string;
  versionPickerOpen: boolean;
  analysisOpen: boolean;
  planningOpen: boolean;
  importDialogType: FactImportDialogType;
  deleteDialogType: FactImportDialogType;
  onVersionSelect: (version: ProjectVersion) => void;
  onCloseVersionPicker: () => void;
  onCloseAnalysis: () => void;
  onClosePlanning: () => void;
  onCloseImport: () => void;
  onCloseDelete: () => void;
  onReload: () => void;
  contextDialogs: ReactNode;
  confirmDialog: ReactNode;
}

/** Диалоги панели факта: версия, анализ, параметры, импорт/удаление, контекстное меню. */
export function FactPanelDialogs({
  projectId,
  projectName,
  projectCode,
  workspaceVersions,
  factVersionId,
  factVersionLabel,
  versionPickerOpen,
  analysisOpen,
  planningOpen,
  importDialogType,
  deleteDialogType,
  onVersionSelect,
  onCloseVersionPicker,
  onCloseAnalysis,
  onClosePlanning,
  onCloseImport,
  onCloseDelete,
  onReload,
  contextDialogs,
  confirmDialog,
}: FactPanelDialogsProps) {
  return (
    <>
      <ProjectVersionPickerDialog
        open={versionPickerOpen}
        title="Выбор версии (факт)"
        projectId={projectId}
        versions={workspaceVersions}
        directionFilter={ANALYSIS_FACT}
        selectedVersionId={factVersionId}
        onSelect={onVersionSelect}
        onClose={onCloseVersionPicker}
      />

      <FactAnalysisDialog
        open={analysisOpen}
        projectId={projectId}
        versionId={factVersionId}
        projectName={projectName}
        projectCode={projectCode}
        versionLabel={factVersionLabel}
        onClose={onCloseAnalysis}
      />

      {contextDialogs}
      {confirmDialog}

      <PlanningParametersDialog
        open={planningOpen}
        projectId={projectId}
        versionId={factVersionId}
        showFactColumn
        onClose={onClosePlanning}
        onSaved={onReload}
      />

      {importDialogType && factVersionId && (
        <DataImportDialog
          open
          dataType={importDialogType}
          projectId={projectId}
          versionId={factVersionId}
          onClose={onCloseImport}
          onSuccess={onReload}
        />
      )}

      {deleteDialogType && factVersionId && (
        <IterationDeleteDialog
          open
          dataType={deleteDialogType}
          projectId={projectId}
          versionId={factVersionId}
          onClose={onCloseDelete}
          onSuccess={onReload}
        />
      )}
    </>
  );
}
