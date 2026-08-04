import { useMemo } from 'react';
import { Autocomplete, TextField } from '@mui/material';
import type { DictionaryItem } from '../../types/dictionary';
import { formatIisNorm } from '../../utils/formatIisNorm';

export interface HierarchicalSelectOption {
  id: number | null;
  label: string;
  depth: number;
}

export interface HierarchicalSelectProps {
  items: DictionaryItem[];
  value: number | null;
  onChange: (value: number | null) => void;
  formatLabel?: (item: DictionaryItem) => string;
  allowEmpty?: boolean;
  emptyLabel?: string;
  excludeIds?: number[];
  required?: boolean;
  disabled?: boolean;
}

function defaultLabel(item: DictionaryItem): string {
  return formatIisNorm({ code: item.code, name: item.name });
}

function buildFlatOptions(
  items: DictionaryItem[],
  formatLabel: (item: DictionaryItem) => string,
  excludeIds: Set<number>,
): HierarchicalSelectOption[] {
  const byParent = new Map<number | null, DictionaryItem[]>();
  for (const item of items) {
    if (excludeIds.has(item.id)) continue;
    const key = item.parentId ?? null;
    const bucket = byParent.get(key);
    if (bucket) bucket.push(item);
    else byParent.set(key, [item]);
  }

  for (const bucket of byParent.values()) {
    bucket.sort((a, b) => {
      const codeCmp = (a.code ?? '').localeCompare(b.code ?? '', 'ru');
      return codeCmp !== 0 ? codeCmp : a.name.localeCompare(b.name, 'ru');
    });
  }

  const result: HierarchicalSelectOption[] = [];
  const visit = (parentId: number | null, depth: number, visited: Set<number>) => {
    const children = byParent.get(parentId);
    if (!children) return;
    for (const child of children) {
      if (visited.has(child.id)) continue;
      visited.add(child.id);
      result.push({ id: child.id, label: formatLabel(child), depth });
      visit(child.id, depth + 1, visited);
    }
  };

  visit(null, 0, new Set());
  return result;
}

export function HierarchicalSelect({
  items,
  value,
  onChange,
  formatLabel = defaultLabel,
  allowEmpty = true,
  emptyLabel = 'Не выбрано',
  excludeIds = [],
  required,
  disabled,
}: HierarchicalSelectProps) {
  const excludeSet = useMemo(() => new Set(excludeIds), [excludeIds]);

  const options = useMemo(() => {
    const tree = buildFlatOptions(items, formatLabel, excludeSet);
    if (!allowEmpty) return tree;
    return [{ id: null, label: emptyLabel, depth: 0 }, ...tree];
  }, [allowEmpty, emptyLabel, excludeSet, formatLabel, items]);

  const selected =
    options.find((option) => option.id === value) ??
    (allowEmpty ? options[0] : null);

  return (
    <Autocomplete
      disabled={disabled}
      options={options}
      value={selected}
      onChange={(_, option) => onChange(option?.id ?? null)}
      getOptionLabel={(option) => option.label}
      isOptionEqualToValue={(a, b) => a.id === b.id}
      filterOptions={(opts, state) => {
        const query = state.inputValue.trim().toLowerCase();
        if (!query) return opts;
        return opts.filter((option) => option.label.toLowerCase().includes(query));
      }}
      renderOption={(props, option) => (
        <li {...props} key={`${option.id ?? 'empty'}-${option.label}`} style={{ paddingLeft: 8 + option.depth * 16 }}>
          {option.label}
        </li>
      )}
      renderInput={(params) => (
        <TextField {...params} size="small" fullWidth required={required} placeholder={emptyLabel} />
      )}
    />
  );
}
