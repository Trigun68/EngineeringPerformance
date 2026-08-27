import { useCallback, useEffect, useMemo, useState } from 'react';
import { Box, Button, Checkbox, FormControlLabel, Stack, TextField } from '@mui/material';
import type { ColDef, IDoesFilterPassParams, ValueGetterParams } from 'ag-grid-community';
import { useGridFilter, type CustomFilterProps } from 'ag-grid-react';

interface ExcelSetColumnFilterModel {
  values: string[];
}

interface FilterEntry {
  /** Стабильный ключ для matching / persistence (id lookup или display). */
  key: string;
  /** Подпись в чеклисте (отформатированный текст). */
  label: string;
}

const EMPTY_KEY = '';
const EMPTY_LABEL = '(Пусто)';

function toFilterKey(value: unknown): string {
  if (value == null || value === '') return EMPTY_KEY;
  return String(value);
}

function readGetter(
  data: Record<string, unknown>,
  colDef: ColDef | undefined,
  kind: 'filter' | 'display',
): unknown {
  if (!colDef || !data) return null;

  const filterGetter = colDef.filterValueGetter;
  const valueGetter = colDef.valueGetter;

  if (kind === 'filter' && typeof filterGetter === 'function') {
    return filterGetter({ data, colDef } as ValueGetterParams);
  }

  if (typeof valueGetter === 'function') {
    return valueGetter({ data, colDef } as ValueGetterParams);
  }

  if (kind === 'display' && typeof filterGetter === 'function') {
    return filterGetter({ data, colDef } as ValueGetterParams);
  }

  if (colDef.field) {
    return data[colDef.field];
  }

  return null;
}

function readEntry(data: Record<string, unknown>, colDef: ColDef | undefined): FilterEntry {
  const hasFilterGetter = typeof colDef?.filterValueGetter === 'function';
  const rawKey = hasFilterGetter
    ? readGetter(data, colDef, 'filter')
    : readGetter(data, colDef, 'display');
  const rawLabel = hasFilterGetter
    ? readGetter(data, colDef, 'display')
    : rawKey;

  const key = toFilterKey(rawKey);
  const label =
    key === EMPTY_KEY
      ? EMPTY_LABEL
      : rawLabel == null || rawLabel === ''
        ? key
        : String(rawLabel);

  return { key, label };
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
  const [valuesEpoch, setValuesEpoch] = useState(0);

  // Пересчитываем уникальные значения при изменении модели строк (lookup-подписи подгружаются async).
  useEffect(() => {
    const bump = () => setValuesEpoch((n) => n + 1);
    api.addEventListener('modelUpdated', bump);
    api.addEventListener('rowDataUpdated', bump);
    return () => {
      api.removeEventListener('modelUpdated', bump);
      api.removeEventListener('rowDataUpdated', bump);
    };
  }, [api]);

  const uniqueEntries = useMemo(() => {
    const byKey = new Map<string, string>();
    api.forEachNode((node) => {
      if (!node.data) return;
      const entry = readEntry(node.data as Record<string, unknown>, colDef);
      if (!byKey.has(entry.key)) {
        byKey.set(entry.key, entry.label);
      }
    });
    return [...byKey.entries()]
      .map(([key, label]) => ({ key, label }))
      .sort((a, b) => a.label.localeCompare(b.label, 'ru'));
  }, [api, colDef, valuesEpoch]);

  const allKeys = useMemo(() => uniqueEntries.map((e) => e.key), [uniqueEntries]);

  useEffect(() => {
    if (model?.values) {
      setDraftSelected(new Set(model.values));
      return;
    }
    setDraftSelected(null);
  }, [model]);

  const filteredEntries = useMemo(() => {
    const q = search.trim().toLowerCase();
    if (!q) return uniqueEntries;
    return uniqueEntries.filter((e) => e.label.toLowerCase().includes(q) || e.key.toLowerCase().includes(q));
  }, [search, uniqueEntries]);

  const doesFilterPass = useCallback(
    (params: IDoesFilterPassParams) => {
      if (!model?.values) return true;
      if (model.values.length === 0) return false;
      const selected = new Set(model.values);
      const entry = readEntry(params.data as Record<string, unknown>, colDef);
      if (selected.has(entry.key)) return true;

      // Совместимость со старыми сохранёнными фильтрами: раньше в model попадали display-строки.
      if (selected.has(entry.label)) return true;

      return false;
    },
    [colDef, model],
  );

  useGridFilter({ doesFilterPass });

  const isChecked = (key: string): boolean =>
    draftSelected == null || draftSelected.has(key);

  const toggleValue = (key: string) => {
    const base = draftSelected ?? new Set(allKeys);
    const next = new Set(base);
    if (next.has(key)) next.delete(key);
    else next.add(key);
    if (next.size === allKeys.length) {
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
        {filteredEntries.length === 0 && (
          <Box sx={{ color: 'text.secondary', fontSize: 13, py: 1 }}>Нет совпадений</Box>
        )}
        {filteredEntries.map((entry) => (
          <FormControlLabel
            key={entry.key || '__empty__'}
            sx={{ ml: 0 }}
            control={
              <Checkbox
                size="small"
                checked={isChecked(entry.key)}
                onChange={() => toggleValue(entry.key)}
              />
            }
            label={entry.label}
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
