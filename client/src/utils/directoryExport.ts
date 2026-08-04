import type { Column, GridApi, IRowNode } from 'ag-grid-community';

function formatCellValue(value: unknown): string {
  if (value == null) return '';
  if (typeof value === 'boolean') return value ? 'Да' : 'Нет';
  if (value instanceof Date) {
    const d = value.getDate().toString().padStart(2, '0');
    const m = (value.getMonth() + 1).toString().padStart(2, '0');
    return `${d}.${m}.${value.getFullYear()}`;
  }
  return String(value);
}

export type CollectGridExportOptions = {
  /**
   * Включать заголовки групп, подитоги (footer) и общий итог.
   * Без активной группировки колонок поведение как у leaf-only.
   */
  includeGroups?: boolean;
};

function getExportColumns(api: GridApi): Column[] {
  return (
    api.getAllDisplayedColumns()?.filter((col) => {
      const def = col.getColDef();
      return !def.checkboxSelection && col.getColId() !== 'ag-Grid-SelectionColumn';
    }) ?? []
  );
}

/** Подпись для footer / grand total, если ячейка группы пустая. */
function groupFooterLabel(node: IRowNode): string {
  const key = node.key;
  const hasKey = key != null && String(key).trim() !== '';
  // Корневой grand total обычно level < 0 или без ключа
  if (!hasKey || (node.level != null && node.level < 0)) {
    return 'Итого';
  }
  return `Итого: ${key}`;
}

function buildRowCells(
  api: GridApi,
  columns: Column[],
  node: IRowNode,
): string[] {
  const cells = columns.map((col) => formatCellValue(api.getValue(col, node)));

  if (!node.group) {
    return cells;
  }

  // Заголовок группы: ключ обычно уже в колонке группировки через getValue.
  // Footer / grand total: подставляем «Итого…», если первая ячейка пустая.
  if (node.footer) {
    const label = groupFooterLabel(node);
    const firstFilled = cells.findIndex((c) => c.trim() !== '');
    if (cells[0]?.trim() === '') {
      cells[0] = label;
    } else if (firstFilled < 0) {
      cells[0] = label;
    }
    return cells;
  }

  // Group header без значения в первой колонке — подставить ключ.
  if (cells[0]?.trim() === '' && node.key != null && String(node.key).trim() !== '') {
    cells[0] = String(node.key);
  }

  return cells;
}

/**
 * Собирает видимые колонки и отфильтрованные строки AG Grid для экспорта в Excel.
 * По умолчанию — только листовые строки (справочники).
 * С `includeGroups: true` — структура группировки, подитоги и общий итог как в гриде.
 */
export function collectGridExportData(
  api: GridApi,
  options?: CollectGridExportOptions,
): { headers: string[]; rows: string[][] } {
  const columns = getExportColumns(api);

  const headers = columns.map((col) => {
    const def = col.getColDef();
    return def.headerName ?? def.field ?? col.getColId();
  });

  const hasRowGroups = (api.getRowGroupColumns()?.length ?? 0) > 0;
  const includeGroups = Boolean(options?.includeGroups) && hasRowGroups;

  const rows: string[][] = [];
  api.forEachNodeAfterFilterAndSort((node) => {
    if (includeGroups) {
      if (node.group) {
        rows.push(buildRowCells(api, columns, node));
        return;
      }
      if (!node.data) return;
      rows.push(buildRowCells(api, columns, node));
      return;
    }

    if (node.group || !node.data) return;
    rows.push(buildRowCells(api, columns, node));
  });

  return { headers, rows };
}

export function buildExportFileName(tabName: string): string {
  const now = new Date();
  const pad = (n: number) => n.toString().padStart(2, '0');
  const stamp = `${now.getFullYear()}${pad(now.getMonth() + 1)}${pad(now.getDate())}_${pad(now.getHours())}${pad(now.getMinutes())}${pad(now.getSeconds())}`;
  const safe = tabName.replace(/[/\\?%*:|"<>]/g, '_').trim() || 'Справочник';
  const cleaned = safe.replace(/^_+$/, 'Справочник');
  return `${cleaned}_${stamp}.xlsx`;
}
