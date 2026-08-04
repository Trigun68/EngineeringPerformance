import {
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
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
}

export function AnalyticsPeriodFilters({
  preset,
  dateFrom,
  dateTo,
  onPresetChange,
  onDateFromChange,
  onDateToChange,
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
      <ToggleButtonGroup
        exclusive
        size="small"
        value={preset}
        onChange={(_, next: AnalyticsPeriodPreset | null) => {
          if (next) onPresetChange(next);
        }}
        sx={{
          flexWrap: 'wrap',
          gap: 0.5,
          '& .MuiToggleButtonGroup-grouped': {
            border: '1px solid #AEB6BF !important',
            borderRadius: '4px !important',
            mx: 0,
            textTransform: 'none',
            px: 1.5,
          },
          display: { xs: 'none', lg: 'inline-flex' },
        }}
        data-testid="analytics-period-toggles"
      >
        {ANALYTICS_PERIOD_PRESETS.map((p) => (
          <ToggleButton key={p} value={p} data-testid={`analytics-period-${p}`}>
            {ANALYTICS_PERIOD_PRESET_LABELS[p]}
          </ToggleButton>
        ))}
      </ToggleButtonGroup>

      <FormControl
        size="small"
        sx={{ minWidth: 220, display: { xs: 'flex', lg: 'none' } }}
        data-testid="analytics-period-select"
      >
        <InputLabel id="analytics-period-label">Период</InputLabel>
        <Select
          labelId="analytics-period-label"
          label="Период"
          value={preset}
          onChange={(e) => onPresetChange(e.target.value as AnalyticsPeriodPreset)}
        >
          {ANALYTICS_PERIOD_PRESETS.map((p) => (
            <MenuItem key={p} value={p}>
              {ANALYTICS_PERIOD_PRESET_LABELS[p]}
            </MenuItem>
          ))}
        </Select>
      </FormControl>

      {isCustom && (
        <Stack direction="row" spacing={1.5} alignItems="center" flexWrap="wrap" useFlexGap>
          <TextField
            size="small"
            label="Дата с"
            type="date"
            value={dateFrom}
            onChange={(e) => onDateFromChange(e.target.value)}
            InputLabelProps={{ shrink: true }}
            inputProps={{ 'data-testid': 'analytics-date-from' }}
          />
          <TextField
            size="small"
            label="Дата по"
            type="date"
            value={dateTo}
            onChange={(e) => onDateToChange(e.target.value)}
            InputLabelProps={{ shrink: true }}
            inputProps={{ 'data-testid': 'analytics-date-to' }}
          />
        </Stack>
      )}
    </Stack>
  );
}
