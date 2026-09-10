import { useState, type ReactNode } from 'react';
import type { ImportDataType } from '../../../api/importApi';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import type { ContextMenuItem } from '../../../components/DataTable/DataTable';
import { useProjectDetailContextMenu } from '../shared/useProjectDetailContextMenu';
import type { ProjectsPageDetailRow } from '../types';
import { FactPanelDialogs, type FactImportDialogType } from './FactPanelDialogs';

export interface UseFactPanelDialogsParams {
  projectId: number;
  projectName: string;
  projectCode: string;
  workspaceVersions: ProjectVersion[];
  factVersionId: number | null;
  factVersionLabel: string;
  onVersionSelect: (version: ProjectVersion) => void;
  onReload: () => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export interface UseFactPanelDialogsResult {
  contextMenuItems: ContextMenuItem<ProjectsPageDetailRow>[];
  dialogs: ReactNode;
  onOpenVersionPicker: () => void;
  onOpenPlanning: () => void;
  onOpenAnalysis: () => void;
  onImport: (type: ImportDataType) => void;
  onDelete: (type: ImportDataType) => void;
}

/** Состояние диалогов факта и пункты контекстного меню детальной гриды. */
export function useFactPanelDialogs({
  projectId,
  projectName,
  projectCode,
  workspaceVersions,
  factVersionId,
  factVersionLabel,
  onVersionSelect,
  onReload,
  setLoading,
  setError,
}: UseFactPanelDialogsParams): UseFactPanelDialogsResult {
  const [versionPickerOpen, setVersionPickerOpen] = useState(false);
  const [analysisOpen, setAnalysisOpen] = useState(false);
  const [planningOpen, setPlanningOpen] = useState(false);
  const [importDialogType, setImportDialogType] = useState<FactImportDialogType>(null);
  const [deleteDialogType, setDeleteDialogType] = useState<FactImportDialogType>(null);

  const { contextMenuItems, contextDialogs, ConfirmDialog } = useProjectDetailContextMenu({
    projectId,
    versionId: factVersionId,
    scope: 'fact',
    setImportDialogType,
    setLoading,
    setError,
  });

  return {
    contextMenuItems,
    dialogs: (
      <FactPanelDialogs
        projectId={projectId}
        projectName={projectName}
        projectCode={projectCode}
        workspaceVersions={workspaceVersions}
        factVersionId={factVersionId}
        factVersionLabel={factVersionLabel}
        versionPickerOpen={versionPickerOpen}
        analysisOpen={analysisOpen}
        planningOpen={planningOpen}
        importDialogType={importDialogType}
        deleteDialogType={deleteDialogType}
        onVersionSelect={onVersionSelect}
        onCloseVersionPicker={() => setVersionPickerOpen(false)}
        onCloseAnalysis={() => setAnalysisOpen(false)}
        onClosePlanning={() => setPlanningOpen(false)}
        onCloseImport={() => setImportDialogType(null)}
        onCloseDelete={() => setDeleteDialogType(null)}
        onReload={onReload}
        contextDialogs={contextDialogs}
        confirmDialog={ConfirmDialog}
      />
    ),
    onOpenVersionPicker: () => setVersionPickerOpen(true),
    onOpenPlanning: () => setPlanningOpen(true),
    onOpenAnalysis: () => setAnalysisOpen(true),
    onImport: setImportDialogType,
    onDelete: setDeleteDialogType,
  };
}
