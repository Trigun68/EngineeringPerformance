import { Box, MenuItem, TextField } from '@mui/material';
import { PlanningScenarioSelector } from '../../norms/PlanningScenarioSelector';
import type { PlanningScenario } from '../../../types/norms';
import { Z_PARAMETER_LEVEL_LABELS, type ZParameterLevel } from './planningZLevels';

export interface PlanningParametersFormProps {
  scenario: PlanningScenario;
  onScenarioChange: (value: PlanningScenario) => void;
  zLevel: ZParameterLevel;
  onZLevelChange: (value: ZParameterLevel) => void;
  canEdit: boolean;
  saving: boolean;
}

export function PlanningParametersForm({
  scenario,
  onScenarioChange,
  zLevel,
  onZLevelChange,
  canEdit,
  saving,
}: PlanningParametersFormProps) {
  return (
    <Box sx={{ mb: 2, maxWidth: 420, display: 'flex', flexDirection: 'column', gap: 2 }}>
      <PlanningScenarioSelector
        value={scenario}
        onChange={onScenarioChange}
        disabled={!canEdit || saving}
        helperText="Группировка объёма включает физические объёмы и оборудование одной группы ИИС."
      />
      <TextField
        select
        size="small"
        fullWidth
        label="Уровень параметров Z-оценки"
        value={zLevel}
        disabled={!canEdit || saving}
        onChange={(e) => onZLevelChange(Number(e.target.value) as ZParameterLevel)}
        data-testid="z-parameter-level-selector"
      >
        {(Object.entries(Z_PARAMETER_LEVEL_LABELS) as Array<[string, string]>).map(([k, label]) => (
          <MenuItem key={k} value={Number(k)}>
            {label}
          </MenuItem>
        ))}
      </TextField>
    </Box>
  );
}
