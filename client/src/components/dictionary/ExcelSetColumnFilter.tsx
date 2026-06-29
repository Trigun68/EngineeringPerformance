import { useCallback, useEffect, useMemo, useState } from 'react';
import { Box, Button, Checkbox, FormControlLabel, Stack, TextField } from '@mui/material';
import type { ColDef, IDoesFilterPassParams, ValueGetterParams } from 'ag-grid-community';
import { useGridFilter, type CustomFilterProps } from 'ag-grid-react';

interface ExcelSetColumnFilterModel {
  values: string[];
}

function cellDisplayValue(value: unknown): string {
  if (value == null || value === '') return '(Пусто)';
  return String(value);
}

function readCellValue(data: Record<string, unknown>, colDef: ColDef | undefined): unknown {
  if (!colDef || !data) return null;

  if (typeof colDef.filterValueGetter === 'function') {
    return colDef.filterValueGetter({ data, colDef } as ValueGetterParams);
  }

  if (typeof colDef.valueGetter === 'function') {
    return colDef.valueGetter({ data, colDef } as ValueGetterParams);
  }

  if (colDef.field) {
    return data[colDef.field];
  }

  return null;
}

/** Excel-подобный фильтр (community AG Grid): уникальные значения, поиск, apply/reset. */
export function ExcelSetColumnFilter({
  model,
  onModelChange,
  colDef,
  api,
}: CustomFilterProps<unknown, unknown, ExcelSetColumnFilterModel>) {
  const [search, setSearch] = useState('');
  const [draftSelected, setDraftSelected] = useState<Set<string> | null>(null);

  const uniqueValues = useMemo(() => {
    const values = new Set<string>();
    api.forEachNode((node) => {
      if (!node.data) return;
      const raw = readCellValue(node.data as Record<string, unknown>, colDef);
      values.add(cellDisplayValue(raw));
    });
    return [...values].sort((a, b) => a.localeCompare(b, 'ru'));
  }, [api, colDef]);

  useEffect(() => {
    if (model?.values) {
      setDraftSelected(new Set(model.values));
      return;
    }
    setDraftSelected(null);
  }, [model]);

  const filteredValues = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return uniqueValues;
    return uniqueValues.filter((v) => v.toLowerCase().includes(q));
  }, [search, uniqueValues]);

  const doesFilterPass = useCallback(
    (params: IDoesFilterPassParams) => {
      if (!model?.values) return true;
      if (model.values.length === 0) return false;
      const selected = new Set(model.values);
      const raw = readCellValue(params.data as Record<string, unknown>, colDef);
      return selected.has(cellDisplayValue(raw));
    },
    [colDef, model],
  );

  useGridFilter({ doesFilterPass });

  const isChecked = (value: string): boolean =>
    draftSelected == null || draftSelected.has(value);

  const toggleValue = (value: string) => {
    const base = draftSelected ?? new Set(uniqueValues);
    const next = new Set(base);
    if (next.has(value)) next.delete(value);
    else next.add(value);
    if (next.size === uniqueValues.length) {
      setDraftSelected(null);
      return;
    }
    setDraftSelected(next);
  };

  const selectAll = () => setDraftSelected(null);

  const deselectAll = () => setDraftSelected(new Set());

  const closeFilterPopup = () => {
    const popupApi = api as typeof api & { hidePopup?: () => void };
    popupApi.hidePopup?.();
  };

  const applyFilter = () => {
    if (draftSelected == null) {
      onModelChange(null);
    } else {
      onModelChange({ values: [...draftSelected] });
    }
    closeFilterPopup();
  };

  const resetFilter = () => {
    setSearch('');
    setDraftSelected(null);
    onModelChange(null);
    closeFilterPopup();
  };

  return (
    <Box sx={{ p: 1.5, minWidth: 240, maxWidth: 320 }}>
      <TextField
        size="small"
        placeholder="Поиск..."
        value={search}
        onChange={(e) => setSearch(e.target.value)}
        fullWidth
        sx={{ mb: 1 }}
      />
      <Stack direction="row" spacing={1} sx={{ mb: 1 }}>
        <Button size="small" variant="text" onClick={selectAll} sx={{ textTransform: 'none', minWidth: 0, px: 0.5 }}>
          Выбрать все
        </Button>
        <Button size="small" variant="text" onClick={deselectAll} sx={{ textTransform: 'none', minWidth: 0, px: 0.5 }}>
          Снять все
        </Button>
      </Stack>
      <Stack sx={{ maxHeight: 220, overflow: 'auto', mb: 1 }}>
        {filteredValues.length === 0 && (
          <Box sx={{ color: 'text.secondary', fontSize: 13, py: 1 }}>Нет совпадений</Box>
        )}
        {filteredValues.map((value) => (
          <FormControlLabel
            key={value}
            sx={{ ml: 0 }}
            control={
              <Checkbox
                size="small"
                checked={isChecked(value)}
                onChange={() => toggleValue(value)}
              />
            }
            label={value}
          />
        ))}
      </Stack>
      <Stack direction="row" spacing={1} justifyContent="flex-end">
        <Button size="small" variant="outlined" onClick={resetFilter}>
          Сбросить
        </Button>
        <Button size="small" variant="contained" onClick={applyFilter}>
          Применить
        </Button>
      </Stack>
    </Box>
  );
}
