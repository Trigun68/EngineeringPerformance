import { useMemo } from 'react';
import {
  Autocomplete,
  Box,
  Checkbox,
  Chip,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from '@mui/material';
import type { NormRegistryFiltersMeta } from '../../../api/normRegistryApi';
import { buildHierarchicalOptions } from '../../../components/dictionary/HierarchicalSelect';
import { formatIisNorm } from '../../../utils/formatIisNorm';

export interface NormFilterState {
  groupIds: number[];
  statuses: string[];
  formulaTypes: string[];
  onlyWithIssues: boolean;
  search: string;
  includeHistorical: boolean;
  includeFuture: boolean;
}

interface Props {
  meta: NormRegistryFiltersMeta | null;
  value: NormFilterState;
  onChange: (next: NormFilterState) => void;
}

const STATUS_LABEL: Record<string, string> = {
  Active: 'Активные',
  Historical: 'Исторические',
  Future: 'Будущие',
};

export function NormFilters({ meta, value, onChange }: Props) {
  const groupItems = useMemo(
    () =>
      (meta?.groups ?? []).map((g) => ({
        id: g.id,
        name: g.name,
        code: g.code,
        parentId: g.parentId ?? null,
        unitName: g.unitName,
      })),
    [meta],
  );

  const groupOptions = useMemo(
    () =>
      buildHierarchicalOptions(
        groupItems.map((g) => ({
          id: g.id,
          name: g.name,
          code: g.code,
          parentId: g.parentId,
        })),
        (item) => {
          const unit = groupItems.find((g) => g.id === item.id)?.unitName;
          return formatIisNorm({ code: item.code, name: item.name, unitName: unit });
        },
      ),
    [groupItems],
  );

  const selectedGroups = useMemo(
    () => groupOptions.filter((o) => o.id != null && value.groupIds.includes(o.id)),
    [groupOptions, value.groupIds],
  );

  return (
    <Stack
      direction={{ xs: 'column', md: 'row' }}
      spacing={1.5}
      flexWrap="wrap"
      useFlexGap
      alignItems={{ md: 'center' }}
      data-testid="norm-analysis-filters"
    >
      <TextField
        size="small"
        label="Поиск"
        value={value.search}
        onChange={(e) => onChange({ ...value, search: e.target.value })}
        sx={{ minWidth: 200 }}
        data-testid="norm-analysis-search"
      />

      <Autocomplete
        multiple
        size="small"
        sx={{ minWidth: 280, flex: 1 }}
        options={groupOptions}
        value={selectedGroups}
        onChange={(_, opts) =>
          onChange({
            ...value,
            groupIds: opts.map((o) => o.id).filter((id): id is number => id != null),
          })
        }
        getOptionLabel={(o) => o.label}
        isOptionEqualToValue={(a, b) => a.id === b.id}
        renderOption={(props, option) => (
          <li
            {...props}
            key={option.id ?? 'empty'}
            style={{ paddingLeft: 8 + option.depth * 16 }}
          >
            {option.label}
          </li>
        )}
        renderTags={(tagValue, getTagProps) =>
          tagValue.map((option, index) => (
            <Chip
              {...getTagProps({ index })}
              key={option.id}
              size="small"
              label={option.label}
            />
          ))
        }
        renderInput={(params) => (
          <TextField
            {...params}
            label="Группы IIC"
            placeholder={value.groupIds.length ? undefined : 'Все'}
            inputProps={{ ...params.inputProps, 'data-testid': 'norm-analysis-groups' }}
          />
        )}
        data-testid="norm-analysis-groups-autocomplete"
      />

      <FormControl size="small" sx={{ minWidth: 180 }}>
        <InputLabel id="norm-status-label">Статус</InputLabel>
        <Select
          labelId="norm-status-label"
          multiple
          label="Статус"
          value={value.statuses}
          onChange={(e) => onChange({ ...value, statuses: e.target.value as string[] })}
          renderValue={(selected) =>
            selected.length === 0
              ? 'Все'
              : selected.map((s) => STATUS_LABEL[s] ?? s).join(', ')
          }
          data-testid="norm-analysis-statuses"
        >
          {(meta?.statuses ?? ['Active', 'Historical', 'Future']).map((s) => (
            <MenuItem key={s} value={s}>
              {STATUS_LABEL[s] ?? s}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <FormControl size="small" sx={{ minWidth: 160 }}>
        <InputLabel id="norm-formula-label">Формула</InputLabel>
        <Select
          labelId="norm-formula-label"
          multiple
          label="Формула"
          value={value.formulaTypes}
          onChange={(e) => onChange({ ...value, formulaTypes: e.target.value as string[] })}
          renderValue={(selected) => (selected.length === 0 ? 'Все' : selected.join(', '))}
          data-testid="norm-analysis-formulas"
        >
          {(meta?.formulaTypes ?? []).map((f) => (
            <MenuItem key={f} value={f}>
              {f}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      <FormControlLabel
        control={
          <Checkbox
            checked={value.onlyWithIssues}
            onChange={(e) => onChange({ ...value, onlyWithIssues: e.target.checked })}
            data-testid="norm-analysis-only-issues"
          />
        }
        label="Только с проблемами"
      />

      <Box sx={{ display: 'flex', gap: 1 }}>
        <FormControlLabel
          control={
            <Checkbox
              checked={value.includeHistorical}
              onChange={(e) => onChange({ ...value, includeHistorical: e.target.checked })}
            />
          }
          label="История на таймлайне"
        />
        <FormControlLabel
          control={
            <Checkbox
              checked={value.includeFuture}
              onChange={(e) => onChange({ ...value, includeFuture: e.target.checked })}
            />
          }
          label="Будущие на таймлайне"
        />
      </Box>
    </Stack>
  );
}
