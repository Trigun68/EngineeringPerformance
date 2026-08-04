import {
  Box,
  Checkbox,
  FormControl,
  FormControlLabel,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from '@mui/material';
import type { NormRegistryFiltersMeta } from '../../../api/normRegistryApi';

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

      <FormControl size="small" sx={{ minWidth: 220 }}>
        <InputLabel id="norm-groups-label">Группы ИИС</InputLabel>
        <Select
          labelId="norm-groups-label"
          multiple
          label="Группы ИИС"
          value={value.groupIds}
          onChange={(e) =>
            onChange({
              ...value,
              groupIds: (e.target.value as number[]).map(Number),
            })
          }
          renderValue={(selected) =>
            selected.length === 0
              ? 'Все'
              : meta?.groups
                  .filter((g) => selected.includes(g.id))
                  .map((g) => g.name)
                  .join(', ') ?? `${selected.length}`
          }
          data-testid="norm-analysis-groups"
        >
          {(meta?.groups ?? []).map((g) => (
            <MenuItem key={g.id} value={g.id}>
              {g.code ? `${g.code} — ${g.name}` : g.name}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

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
