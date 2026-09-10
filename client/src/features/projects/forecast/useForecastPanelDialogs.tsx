import { useState, type ReactNode } from 'react';
import type { ProjectVersion } from '../../../api/projectVersionsApi';
import type { ContextMenuItem } from '../../../components/DataTable/DataTable';
import { useProjectDetailContextMenu } from '../shared/useProjectDetailContextMenu';
import type { ProjectsPageDetailRow, ProjectsPageForecastSummaryRow } from '../types';
import {
  ForecastPanelDialogs,
  type ForecastImportDialogType,
} from './ForecastPanelDialogs';

export interface UseForecastPanelDialogsParams {
  projectId: number;
  projectName: string;
  projectCode: string;
  workspaceVersions: ProjectVersion[];
  planVersionId: number | null;
  planVersionLabel: string;
  summaryRows: ProjectsPageForecastSummaryRow[];
  onVersionSelect: (version: ProjectVersion) => void;
  onReload: () => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export interface UseForecastPanelDialogsResult {
  contextMenuItems: ContextMenuItem<ProjectsPageDetailRow>[];
  dialogs: ReactNode;
  onOpenVersionPicker: () => void;
  onOpenPlanning: () => void;
  onOpenReport: () => void;
  onImportPhysicalVolumes: () => void;
  onDeletePhysicalVolumes: () => void;
  onImportEquipment: () => void;
  onDeleteEquipment: () => void;
}

/** Состояние диалогов прогноза и пункты контекстного меню детальной гриды. */
export function useForecastPanelDialogs({
  projectId,
  projectName,
  projectCode,
  workspaceVersions,
  planVersionId,
  planVersionLabel,
  summaryRows,
  onVersionSelect,
  onReload,
  setLoading,
  setError,
}: UseForecastPanelDialogsParams): UseForecastPanelDialogsResult {
  const [versionPickerOpen, setVersionPickerOpen] = useState(false);
  const [reportOpen, setReportOpen] = useState(false);
  const [planningOpen, setPlanningOpen] = useState(false);
  const [importDialogType, setImportDialogType] = useState<ForecastImportDialogType>(null);
  const [deleteDialogType, setDeleteDialogType] = useState<ForecastImportDialogType>(null);

  const { contextMenuItems, contextDialogs, ConfirmDialog } = useProjectDetailContextMenu({
    projectId,
    versionId: planVersionId,
    scope: 'forecast',
    setImportDialogType,
    setLoading,
    setError,
  });

  return {
    contextMenuItems,
    dialogs: (
      <ForecastPanelDialogs
        projectId={projectId}
        projectName={projectName}
        projectCode={projectCode}
        workspaceVersions={workspaceVersions}
        planVersionId={planVersionId}
        planVersionLabel={planVersionLabel}
        summaryRows={summaryRows}
        versionPickerOpen={versionPickerOpen}
        reportOpen={reportOpen}
        planningOpen={planningOpen}
        importDialogType={importDialogType}
        deleteDialogType={deleteDialogType}
        onVersionSelect={onVersionSelect}
        onCloseVersionPicker={() => setVersionPickerOpen(false)}
        onCloseReport={() => setReportOpen(false)}
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
    onOpenReport: () => setReportOpen(true),
    onImportPhysicalVolumes: () => setImportDialogType('physical-volumes'),
    onDeletePhysicalVolumes: () => setDeleteDialogType('physical-volumes'),
    onImportEquipment: () => setImportDialogType('equipment'),
    onDeleteEquipment: () => setDeleteDialogType('equipment'),
  };
}
