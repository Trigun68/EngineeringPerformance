import { useCallback, useMemo, useState } from 'react';
import {
  createEquipment,
  createPhysicalVolume,
  deleteEquipment,
  deletePhysicalVolume,
  fetchEquipmentById,
  fetchPhysicalVolumeById,
} from '../../../api/projectDataApi';
import type { ImportDataType } from '../../../api/importApi';
import type { ContextMenuItem } from '../../../components/DataTable/DataTable';
import { usePermission } from '../../../hooks/usePermission';
import { useConfirmation } from '../../../hooks/useConfirmation';
import { getApiErrorMessage } from '../../../services/apiError';
import type { ProjectsPageDetailRow } from '../types';
import { ProjectEquipmentQuickEditDialog } from '../forecast/ProjectEquipmentQuickEditDialog';
import { ProjectPhysicalVolumeQuickEditDialog } from '../forecast/ProjectPhysicalVolumeQuickEditDialog';
import { ForecastDetailCatalogDialog } from '../forecast/ForecastDetailCatalogDialog';
import { ForecastDetailNormDialog } from '../forecast/ForecastDetailNormDialog';

interface QuickEditState {
  rowId: number;
  nomenclature: string;
  quantity: number;
}

function detailNomenclature(row: ProjectsPageDetailRow): string {
  return (
    (row.isEquipment
      ? row.itemName || row.catalogName || row.pvName
      : row.pvName || row.catalogName || row.itemName) ?? ''
  ).trim() || '—';
}

export interface UseFactDetailContextMenuParams {
  projectId: number;
  factVersionId: number | null;
  onReload: () => Promise<void>;
  setImportDialogType: (type: ImportDataType | null) => void;
  setLoading: (loading: boolean) => void;
  setError: (error: string | null) => void;
}

export function useFactDetailContextMenu({
  projectId,
  factVersionId,
  onReload,
  setImportDialogType,
  setLoading,
  setError,
}: UseFactDetailContextMenuParams) {
  const { hasAccess: canImport } = usePermission('ProjectDataUpload');
  const { hasAccess: canEditPpv } = usePermission('ProjectPhysicalVolumes');
  const { hasAccess: canEditPeq } = usePermission('ProjectEquipments');
  const { hasAccess: canEditPvCatalog } = usePermission('PhysicalVolumes');
  const { hasAccess: canEditEquipmentCatalog } = usePermission('Equipment');
  const { hasAccess: canEditNorms } = usePermission('IicWorkDirectionNorms');
  const { confirm, Dialog: ConfirmDialog } = useConfirmation();

  const [ppvEdit, setPpvEdit] = useState<QuickEditState | null>(null);
  const [peqEdit, setPeqEdit] = useState<QuickEditState | null>(null);
  const [catalogEdit, setCatalogEdit] = useState<{
    resource: string;
    entryId: number;
    title: string;
  } | null>(null);
  const [normEdit, setNormEdit] = useState<{
    normId: number;
    workDirectionId: number;
    groupLabel: string;
    unitLabel: string;
  } | null>(null);

  const handleDeleteDetailRow = useCallback(async (row: ProjectsPageDetailRow) => {
    if (!factVersionId || !row.rowId) return;
    const label = detailNomenclature(row);
    if (!(await confirm({
      title: 'Удалить строку?',
      description: `Удалить строку «${label}»?`,
    }))) return;

    setLoading(true);
    setError(null);
    try {
      if (row.isEquipment) {
        if (!canEditPeq) return;
        await deleteEquipment(projectId, factVersionId, row.rowId);
      } else {
        if (!canEditPpv) return;
        await deletePhysicalVolume(projectId, factVersionId, row.rowId);
      }
      await onReload();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось удалить строку.'));
    } finally {
      setLoading(false);
    }
  }, [canEditPeq, canEditPpv, factVersionId, onReload, projectId, setError, setLoading]);

  const handleCopyDetailRow = useCallback(async (row: ProjectsPageDetailRow) => {
    if (!row.rowId || !factVersionId) return;
    setLoading(true);
    setError(null);
    try {
      if (row.isEquipment) {
        if (!canEditPeq) return;
        const src = await fetchEquipmentById(projectId, factVersionId, row.rowId);
        if (!src) throw new Error('Строка оборудования не найдена.');
        await createEquipment(projectId, factVersionId, {
          equipmentId: src.equipmentId,
          quantity: src.quantity,
          quantityInWork: src.quantityInWork,
        });
      } else {
        if (!canEditPpv) return;
        const src = await fetchPhysicalVolumeById(projectId, factVersionId, row.rowId);
        if (!src) throw new Error('Строка физ. объёма не найдена.');
        await createPhysicalVolume(projectId, factVersionId, {
          physicalVolumeId: src.physicalVolumeId,
          quantity: src.quantity,
          facilityCode: src.facilityCode,
          facilityName: src.facilityName,
          workName: src.workName,
          laborHoursPerUnit: src.laborHoursPerUnit,
        });
      }
      await onReload();
    } catch (err) {
      setError(getApiErrorMessage(err, 'Не удалось скопировать строку.'));
    } finally {
      setLoading(false);
    }
  }, [canEditPeq, canEditPpv, factVersionId, onReload, projectId, setError, setLoading]);

  const contextMenuItems = useMemo((): ContextMenuItem<ProjectsPageDetailRow>[] => {
    const items: ContextMenuItem<ProjectsPageDetailRow>[] = [];

    if (canEditPpv || canEditPeq) {
      items.push({
        name: 'Редактировать физ. объём',
        disabled: (row) => !row?.rowId || row.isEquipment,
        action: (row) => {
          if (!row?.rowId || row.isEquipment || !canEditPpv) return;
          setPpvEdit({
            rowId: row.rowId,
            nomenclature: detailNomenclature(row),
            quantity: row.quantity,
          });
        },
      });
      items.push({
        name: 'Редактировать оборудование',
        disabled: (row) => !row?.rowId || !row.isEquipment,
        action: (row) => {
          if (!row?.rowId || !row.isEquipment || !canEditPeq) return;
          setPeqEdit({
            rowId: row.rowId,
            nomenclature: detailNomenclature(row),
            quantity: row.quantity,
          });
        },
      });
      items.push({
        name: 'Редактировать номенклатуру физ. объёма',
        disabled: (row) => !canEditPvCatalog || !row?.catalogVolumeId || !row.catalogResource || row.isEquipment,
        action: (row) => {
          if (!canEditPvCatalog || !row?.catalogVolumeId || !row.catalogResource || row.isEquipment) return;
          setCatalogEdit({
            resource: row.catalogResource,
            entryId: row.catalogVolumeId,
            title: 'Редактировать номенклатуру физ. объёма',
          });
        },
      });
      items.push({
        name: 'Редактировать номенклатуру оборудования',
        disabled: (row) => !canEditEquipmentCatalog || !row?.catalogEquipmentId || !row.isEquipment,
        action: (row) => {
          if (!canEditEquipmentCatalog || !row?.catalogEquipmentId || !row.isEquipment) return;
          setCatalogEdit({
            resource: 'equipment',
            entryId: row.catalogEquipmentId,
            title: 'Редактировать номенклатуру оборудования',
          });
        },
      });
      items.push({
        name: 'Редактировать норму IIC',
        disabled: (row) => !canEditNorms || !row?.normId || !row.classifierId,
        action: (row) => {
          if (!canEditNorms || !row?.normId || !row.classifierId) return;
          setNormEdit({
            normId: row.normId,
            workDirectionId: row.classifierId,
            groupLabel: row.iicGroupName,
            unitLabel: row.iicUnitName,
          });
        },
      });
      items.push({
        name: 'Удалить выбранную строку',
        disabled: (row) => !row?.rowId,
        action: (row) => {
          if (row) void handleDeleteDetailRow(row);
        },
      });
      items.push({
        name: 'Добавить строку копированием выбранной',
        disabled: (row) => !row?.rowId,
        action: (row) => {
          if (row) void handleCopyDetailRow(row);
        },
      });
      items.push({
        name: 'Добавить строку',
        action: (row) => {
          setImportDialogType(row?.isEquipment ? 'equipment' : 'physical-volumes');
        },
      });
    } else if (canImport) {
      items.push({
        name: 'Добавить (импорт)',
        action: (row) => {
          setImportDialogType(row?.isEquipment ? 'equipment' : 'physical-volumes');
        },
      });
    }

    return items;
  }, [
    canEditEquipmentCatalog,
    canEditNorms,
    canEditPeq,
    canEditPpv,
    canEditPvCatalog,
    canImport,
    handleCopyDetailRow,
    handleDeleteDetailRow,
    setImportDialogType,
  ]);

  const contextDialogs = factVersionId ? (
    <>
      {ppvEdit && (
        <ProjectPhysicalVolumeQuickEditDialog
          open
          projectId={projectId}
          versionId={factVersionId}
          rowId={ppvEdit.rowId}
          nomenclature={ppvEdit.nomenclature}
          quantity={ppvEdit.quantity}
          onClose={() => setPpvEdit(null)}
          onSaved={() => void onReload()}
        />
      )}
      {peqEdit && (
        <ProjectEquipmentQuickEditDialog
          open
          projectId={projectId}
          versionId={factVersionId}
          rowId={peqEdit.rowId}
          nomenclature={peqEdit.nomenclature}
          quantity={peqEdit.quantity}
          onClose={() => setPeqEdit(null)}
          onSaved={() => void onReload()}
        />
      )}
      <ForecastDetailCatalogDialog
        open={catalogEdit != null}
        resource={catalogEdit?.resource ?? ''}
        entryId={catalogEdit?.entryId ?? null}
        title={catalogEdit?.title ?? ''}
        onClose={() => setCatalogEdit(null)}
        onSaved={() => void onReload()}
      />
      <ForecastDetailNormDialog
        open={normEdit != null}
        normId={normEdit?.normId ?? null}
        workDirectionId={normEdit?.workDirectionId ?? null}
        groupLabel={normEdit?.groupLabel ?? ''}
        unitLabel={normEdit?.unitLabel ?? ''}
        onClose={() => setNormEdit(null)}
        onSaved={() => void onReload()}
      />
    </>
  ) : null;

  return { contextMenuItems, contextDialogs, ConfirmDialog };
}
