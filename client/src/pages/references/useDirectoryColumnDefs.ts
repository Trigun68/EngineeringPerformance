import { useCallback, useMemo } from 'react';
import type { ColDef, ValueGetterParams } from 'ag-grid-community';

import type { DirectoryEntry, DirectoryTreeRow } from '../../api/directoriesApi';
import type { DictionaryItem, DictionaryKey } from '../../types/dictionary';
import { formatAsIisNorm } from '../../utils/formatIisNorm';
import { formatPropertyValue, type DirectoryFieldDef } from './directoryFieldConfig';

function boolArchive(value: boolean): string {
  return value ? 'Да' : 'Нет';
}

function propertyValue<T extends DirectoryEntry>(row: T, field: DirectoryFieldDef): unknown {
  const key = field.propertyKey ?? field.key;
  return row.properties[key] ?? row.properties[field.key];
}

export interface DirectoryColumnDefsInput {
  resource: string;
  isPhysicalVolumeCatalog: boolean;
  fieldDefs: DirectoryFieldDef[];
  optionCounts: Record<number, number>;
  getName: (key: DictionaryKey, id: number | null | undefined) => string;
  getById: (key: DictionaryKey, id: number) => DictionaryItem | undefined;
  getUnitName: (unitId: number | null | undefined) => string;
  resolveIicUnitName: (directionId: number) => string;
}

export interface DirectoryColumnDefs {
  flatColumnDefs: ColDef<DirectoryEntry>[];
  treeColumnDefs: ColDef<DirectoryTreeRow>[];
  autoGroupColumnDef: ColDef<DirectoryTreeRow>;
}

/**
 * Колонки грида справочника: плоский и древовидный варианты + autoGroup-колонка.
 * Вынесено из DirectoryPage, чтобы страница держала только состояние и действия.
 */
export function useDirectoryColumnDefs(input: DirectoryColumnDefsInput): DirectoryColumnDefs {
  const {
    resource,
    isPhysicalVolumeCatalog,
    fieldDefs,
    optionCounts,
    getName,
    getById,
    getUnitName,
    resolveIicUnitName,
  } = input;

  const buildPropertyColumns = useCallback(
    <T extends DirectoryEntry>(): ColDef<T>[] =>
      fieldDefs
        .filter((f) => f.showInGrid)
        .map((field) => {
          const col: ColDef<T> = {
            colId: field.key,
            headerName: field.label,
            minWidth: field.gridWidth ?? 120,
            valueGetter: (params: ValueGetterParams<T>) => {
              const row = params.data;
              if (!row) return '';
              const value = propertyValue(row, field);
              return formatPropertyValue(field, value, getName, getById, getUnitName, resolveIicUnitName);
            },
          };

          // Lookup: фильтр по стабильному id, в чеклисте — отформатированная подпись.
          // Иначе Excel-set сравнивает длинные «код — имя (ед.)» и ломается при смене формата/подгрузке словаря.
          if (field.type === 'lookup') {
            col.filterValueGetter = (params: ValueGetterParams<T>) => {
              const row = params.data;
              if (!row) return '';
              const value = propertyValue(row, field);
              if (value == null || value === '') return '';
              return String(value);
            };
          }

          return col;
        }),
    [fieldDefs, getById, getName, getUnitName, resolveIicUnitName],
  );

  const archiveColDef = useMemo<ColDef>(
    () => ({
      colId: 'isArchived',
      headerName: 'Архив',
      maxWidth: 80,
      // valueGetter (не valueFormatter): Excel-set фильтр читает getter/field, не formatter.
      valueGetter: (p) => boolArchive(Boolean(p.data?.isArchived)),
      filterValueGetter: (p) => (p.data?.isArchived ? '1' : '0'),
    }),
    [],
  );

  const propertyColumns = useMemo(() => buildPropertyColumns<DirectoryEntry>(), [buildPropertyColumns]);
  const treePropertyColumns = useMemo(() => buildPropertyColumns<DirectoryTreeRow>(), [buildPropertyColumns]);

  const flatColumnDefs = useMemo<ColDef<DirectoryEntry>[]>(() => {
    if (resource === 'project-characteristics') {
      return [
        { colId: 'name', field: 'name', headerName: 'Наименование характеристики', minWidth: 280, flex: 1 },
        {
          colId: 'optionCount',
          headerName: 'Кол-во вариантов',
          maxWidth: 140,
          valueGetter: (p) => (p.data ? (optionCounts[p.data.id] ?? 0) : 0),
        },
        archiveColDef,
      ];
    }
    if (isPhysicalVolumeCatalog) {
      return [
        {
          colId: 'code',
          headerName: 'Код',
          minWidth: 120,
          valueGetter: (p) => {
            const code = p.data?.properties?.code;
            return code != null ? String(code) : '';
          },
        },
        { colId: 'name', field: 'name', headerName: 'Наименование', minWidth: 240, flex: 1 },
        ...propertyColumns,
        archiveColDef,
      ];
    }
    return [
      { colId: 'name', field: 'name', headerName: 'Наименование', minWidth: 240, flex: 1 },
      ...propertyColumns,
      archiveColDef,
    ];
  }, [archiveColDef, resource, isPhysicalVolumeCatalog, optionCounts, propertyColumns]);

  const nameColumnDef = useMemo<ColDef<DirectoryTreeRow>>(
    () => ({ colId: 'name', field: 'name', headerName: 'Наименование', minWidth: 240, flex: 1 }),
    [],
  );

  const treeColumnDefs = useMemo<ColDef<DirectoryTreeRow>[]>(
    () => (isPhysicalVolumeCatalog
      ? [nameColumnDef, ...treePropertyColumns, archiveColDef as ColDef<DirectoryTreeRow>]
      : [...treePropertyColumns, archiveColDef as ColDef<DirectoryTreeRow>]),
    [archiveColDef, isPhysicalVolumeCatalog, nameColumnDef, treePropertyColumns],
  );

  const autoGroupColumnDef = useMemo<ColDef<DirectoryTreeRow>>(() => {
    if (isPhysicalVolumeCatalog) {
      return {
        colId: 'ag-Grid-AutoColumn',
        headerName: 'Код',
        minWidth: 280,
        valueGetter: (params) => {
          const row = params.data;
          if (!row) return '';
          const code = row.properties.code;
          return code != null ? String(code) : '';
        },
        cellRendererParams: { suppressCount: true },
      };
    }

    const base: ColDef<DirectoryTreeRow> = {
      colId: 'ag-Grid-AutoColumn',
      headerName: 'Наименование',
      minWidth: 280,
      cellRendererParams: { suppressCount: true },
      // Стабильный ключ фильтра — id строки; подпись — имя / formatAsIisNorm.
      filterValueGetter: (params) => (params.data ? String(params.data.id) : ''),
    };

    // Оборудование — не IIC-группа: формат «код — имя (ед.)» запрещён правилом,
    // ед. изм. показывается отдельной колонкой.
    if (resource === 'equipment') {
      return {
        ...base,
        valueGetter: (params) => params.data?.name ?? '',
      };
    }

    if (resource === 'iic-work-directions') {
      return {
        ...base,
        valueGetter: (params) => {
          const row = params.data;
          if (!row) return '';
          const code = row.properties.code;
          const unitId = row.properties.unitId;
          return formatAsIisNorm(
            typeof code === 'string' ? code : null,
            row.name,
            getUnitName(typeof unitId === 'number' ? unitId : null),
          );
        },
      };
    }

    return {
      ...base,
      valueGetter: (params) => params.data?.name ?? '',
    };
  }, [resource, getUnitName, isPhysicalVolumeCatalog]);

  return { flatColumnDefs, treeColumnDefs, autoGroupColumnDef };
}
