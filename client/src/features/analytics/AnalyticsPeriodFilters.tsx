import {
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
} from '@mui/material';
import {
  ANALYTICS_PERIOD_PRESET_LABELS,
  ANALYTICS_PERIOD_PRESETS,
  type AnalyticsPeriodPreset,
} from './analyticsPeriod';

export interface AnalyticsPeriodFiltersProps {
  preset: AnalyticsPeriodPreset;
  dateFrom: string;
  dateTo: string;
  onPresetChange: (preset: AnalyticsPeriodPreset) => void;
  onDateFromChange: (value: string) => void;
  onDateToChange: (value: string) => void;
  /** Доп. контролы в том же ряду (вес, План/Факт, проекты…). */
  extras?: React.ReactNode;
}

export function AnalyticsPeriodFilters({
  preset,
  dateFrom,
  dateTo,
  onPresetChange,
  onDateFromChange,
  onDateToChange,
  extras,
}: AnalyticsPeriodFiltersProps) {
  const isCustom = preset === 'custom';

  return (
    <Stack
      direction={{ xs: 'column', md: 'row' }}
      spacing={1.5}
      alignItems={{ xs: 'stretch', md: 'center' }}
      flexWrap="wrap"
      useFlexGap
      data-testid="analytics-period-filters"
    >
      <FormControl size="small" sx={{ minWidth: 220 }} data-testid="analytics-period-select">
        <InputLabel id="analytics-period-label">Отчёт за</InputLabel>
        <Select
          labelId="analytics-period-label"
          label="Отчёт за"
          value={preset}
          onChange={(e) => onPresetChange(e.target.value as AnalyticsPeriodPreset)}
        >
          {ANALYTICS_PERIOD_PRESETS.map((p) => (
            <MenuItem key={p} value={p} data-testid={`analytics-period-${p}`}>
              {ANALYTICS_PERIOD_PRESET_LABELS[p]}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      {isCustom ? (
        <Stack direction="row" spacing={1.5} alignItems="center" flexWrap="wrap" useFlexGap>
          <TextField
            size="small"
            label="Дата начала"
            type="date"
            value={dateFrom}
            onChange={(e) => onDateFromChange(e.target.value)}
            InputLabelProps={{ shrink: true }}
            inputProps={{ 'data-testid': 'analytics-date-from' }}
          />
          <TextField
            size="small"
            label="Дата конца"
            type="date"
            value={dateTo}
            onChange={(e) => onDateToChange(e.target.value)}
            InputLabelProps={{ shrink: true }}
            inputProps={{ 'data-testid': 'analytics-date-to' }}
          />
        </Stack>
      ) : null}

      {extras}
    </Stack>
  );
}
