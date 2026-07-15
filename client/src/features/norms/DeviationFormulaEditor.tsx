import { Stack, TextField, Typography } from '@mui/material';
import type { DeviationFormulaDto } from '../../types/norms';

export interface DeviationFormulaEditorProps {
  value: DeviationFormulaDto;
  onChange: (value: DeviationFormulaDto) => void;
}

/** Процент отклонения от нормы N(V) — задаёт ширину коридора вокруг кривой. */
export function DeviationFormulaEditor({ value, onChange }: DeviationFormulaEditorProps) {
  const sigma = value.coefficients.sigma ?? 0;
  const percent = sigma * 100;

  return (
    <Stack spacing={1.5}>
      <TextField
        size="small"
        type="number"
        label="Процент отклонения от нормы, %"
        value={percent === 0 ? '' : percent}
        inputProps={{ min: 0, step: 0.1 }}
        onChange={(e) => {
          const nextPercent = e.target.value === '' ? 0 : Number(e.target.value);
          onChange({
            modelType: 1,
            coefficients: { sigma: nextPercent / 100 },
          });
        }}
      />
      <Typography variant="caption" color="text.secondary">
        Задаёт симметричный коридор ±% от значения производительности N(V) вокруг базовой кривой.
      </Typography>
    </Stack>
  );
}
