import { LinearProgress, Stack, Typography } from '@mui/material';
import type { ProjectCharacteristicSelection } from '../../../../api/projectCharacteristicsApi';
import { fmt } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

interface AccuracyPanelProps {
  characteristics: ProjectCharacteristicSelection[];
  score: number | null;
}

export function AccuracyPanel({ characteristics, score }: AccuracyPanelProps) {
  const withValue = characteristics.filter((c) => c.selectedValuePercent !== null && c.selectedValuePercent !== undefined);

  return (
    <Stack spacing={3}>
      <Typography variant="body2" color="text.secondary">
        «Точность сравнения» — средняя PD% по выбранным характеристикам проекта. Она показывает, насколько
        выборка ИИЦ, из которой взяты нормы, сопоставима с текущим объектом. Чем ниже показатель, тем осторожнее
        следует трактовать Z-оценку.
      </Typography>

      {withValue.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          Для проекта не выбраны характеристики с оценкой сопоставимости.
        </Typography>
      ) : (
        <Stack spacing={2.5}>
          {withValue.map((item) => (
            <Stack spacing={0.5} key={item.characteristicId}>
              <Stack direction="row" justifyContent="space-between" alignItems="baseline">
                <Typography variant="body2" sx={{ fontWeight: 500 }}>
                  {item.characteristicName}
                </Typography>
                <Typography variant="body2" sx={{ fontFamily: 'monospace' }}>
                  {fmt(item.selectedValuePercent, 0)}%
                </Typography>
              </Stack>
              <LinearProgress
                variant="determinate"
                value={Math.min(item.selectedValuePercent ?? 0, 100)}
                sx={{ height: 8, borderRadius: 1, bgcolor: wpColors.pageBg }}
              />
              <Typography variant="caption" color="text.secondary">
                {item.selectedOptionName ?? '—'}
              </Typography>
            </Stack>
          ))}
        </Stack>
      )}

      <Stack
        direction="row"
        justifyContent="space-between"
        alignItems="baseline"
        sx={{ borderTop: 1, borderColor: 'divider', pt: 2 }}
      >
        <Typography variant="body2" sx={{ fontWeight: 600 }}>
          Итоговая точность сравнения
        </Typography>
        <Typography variant="subtitle1" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
          {score === null ? 'Н/Д' : `${fmt(score, 1)}%`}
        </Typography>
      </Stack>
    </Stack>
  );
}
