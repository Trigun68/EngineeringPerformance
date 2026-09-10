import { MenuItem, TextField } from '@mui/material';
import { PLANNING_SCENARIO_LABELS, type PlanningScenario } from '../../types/norms';

export interface PlanningScenarioSelectorProps {
  value: PlanningScenario;
  onChange: (value: PlanningScenario) => void;
  disabled?: boolean;
  helperText?: string;
}

export function PlanningScenarioSelector({ value, onChange, disabled, helperText }: PlanningScenarioSelectorProps) {
  return (
    <TextField
      select
      size="small"
      fullWidth
      label="Сценарий планирования"
      value={value}
      disabled={disabled}
      helperText={helperText}
      onChange={(e) => onChange(Number(e.target.value) as PlanningScenario)}
    >
      {(Object.entries(PLANNING_SCENARIO_LABELS) as Array<[string, string]>).map(([k, label]) => (
        <MenuItem key={k} value={Number(k)}>{label}</MenuItem>
      ))}
    </TextField>
  );
}
