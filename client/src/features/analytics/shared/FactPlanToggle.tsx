import { ToggleButton, ToggleButtonGroup } from '@mui/material';

export type FactPlanMode = 'fact' | 'plan' | 'both';

interface Props {
  value: FactPlanMode;
  onChange: (mode: FactPlanMode) => void;
  testId?: string;
}

/**
 * Единый переключатель «какие основные версии показывать» — заменяет пару
 * отдельных чекбоксов «План»/«Факт» одним сегментированным контролом.
 */
export function FactPlanToggle({ value, onChange, testId = 'fact-plan-toggle' }: Props) {
  return (
    <ToggleButtonGroup
      value={value}
      exclusive
      size="small"
      onChange={(_, next: FactPlanMode | null) => {
        if (next) onChange(next);
      }}
      data-testid={testId}
      aria-label="Основные версии для отображения"
    >
      <ToggleButton value="fact" data-testid={`${testId}-fact`}>
        Факт
      </ToggleButton>
      <ToggleButton value="plan" data-testid={`${testId}-plan`}>
        План
      </ToggleButton>
      <ToggleButton value="both" data-testid={`${testId}-both`}>
        Факт и план
      </ToggleButton>
    </ToggleButtonGroup>
  );
}
