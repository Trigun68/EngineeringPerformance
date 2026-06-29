import type { ContextMenuItem } from '../../components/DataTable/DataTable';

export interface ProjectsPageMenuHandlers {
  onEdit?: () => void;
  onCopyVersion?: () => void;
  onImport?: () => void;
  onExportExcel?: () => void;
  onExportPdf?: () => void;
  onDelete?: () => void;
}

export interface ProjectsPageMenuPermissions {
  canEditData: boolean;
  canCopyVersion: boolean;
  canImport: boolean;
  canExport: boolean;
  canDelete: boolean;
}

/** Пункты контекстного меню: скрыты без прав (legacy AddIfAccessible). */
export function buildProjectsPageContextMenu(
  permissions: ProjectsPageMenuPermissions,
  handlers: ProjectsPageMenuHandlers,
): ContextMenuItem[] {
  const items: ContextMenuItem[] = [];

  if (permissions.canEditData && handlers.onEdit) {
    items.push({ name: 'Редактировать', action: () => handlers.onEdit?.(), requiredLevel: 2 });
  }
  if (permissions.canCopyVersion && handlers.onCopyVersion) {
    items.push({ name: 'Копировать версию', action: () => handlers.onCopyVersion?.(), requiredLevel: 2 });
  }
  if (permissions.canImport && handlers.onImport) {
    items.push({ name: 'Импорт Excel', action: () => handlers.onImport?.(), requiredLevel: 2 });
  }
  if (permissions.canExport && handlers.onExportExcel) {
    items.push({ name: 'Экспорт Excel', action: () => handlers.onExportExcel?.(), requiredLevel: 1 });
  }
  if (permissions.canExport && handlers.onExportPdf) {
    items.push({ name: 'Экспорт PDF', action: () => handlers.onExportPdf?.(), requiredLevel: 1 });
  }
  if (permissions.canDelete && handlers.onDelete) {
    items.push({ name: 'Удалить', action: () => handlers.onDelete?.(), requiredLevel: 2 });
  }

  return items;
}
