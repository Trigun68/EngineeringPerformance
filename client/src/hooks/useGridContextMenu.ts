import { useCallback, useMemo } from 'react';

export interface ContextMenuItem<T> {
  name: string;
  action: (row: T) => void;
  requiredLevel?: 1 | 2;
  condition?: (row: T) => boolean;
}

export interface UseGridContextMenuOptions<T> {
  items: ContextMenuItem<T>[];
  canEdit?: boolean;
}

/**
 * Хук для централизованного управления контекстным меню AG Grid.
 * Унифицирует проверку прав (requiredLevel) и условий видимости (condition).
 * 
 * @example
 * ```tsx
 * const contextMenuItems = useGridContextMenu({
 *   items: [
 *     { name: 'Изменить', action: handleEdit, requiredLevel: 2 },
 *     { name: 'Удалить', action: handleDelete, requiredLevel: 2, condition: (row) => !row.isSystem },
 *   ],
 *   canEdit: userCanEdit,
 * });
 * ```
 */
export function useGridContextMenu<T>({ items, canEdit = false }: UseGridContextMenuOptions<T>) {
  // Фильтруем items по правам и условиям
  const filteredItems = useMemo(() => {
    return items
      .filter((item) => {
        // Проверка уровня доступа
        if (item.requiredLevel && item.requiredLevel > 1 && !canEdit) {
          return false;
        }
        return true;
      })
      .map((item) => ({
        name: item.name,
        action: (row: T) => {
          // Проверка condition перед выполнением
          if (item.condition && !item.condition(row)) {
            return;
          }
          item.action(row);
        },
      }));
  }, [items, canEdit]);

  // Callback для getContextMenuItems AG Grid
  const getContextMenuItems = useCallback(
    (params: { node?: { data?: T } | null }) => {
      if (!params.node?.data) return [];

      const rowData = params.node.data;

      return filteredItems.map((item) => ({
        name: item.name,
        action: () => item.action(rowData),
      }));
    },
    [filteredItems]
  );

  return {
    contextMenuItems: filteredItems,
    getContextMenuItems,
  };
}

/**
 * Упрощённая версия для прямого использования в DataTable.
 * Принимает массив items и возвращает готовый для передачи в DataTable массив.
 */
export function useDataTableContextMenu<T>(
  items: ContextMenuItem<T>[],
  canEdit = false
): Array<{ name: string; action: (row: T) => void }> {
  return useMemo(() => {
    return items
      .filter((item) => {
        if (item.requiredLevel && item.requiredLevel > 1 && !canEdit) {
          return false;
        }
        return true;
      })
      .map((item) => ({
        name: item.name,
        action: (row: T) => {
          if (item.condition && !item.condition(row)) return;
          item.action(row);
        },
      }));
  }, [items, canEdit]);
}
