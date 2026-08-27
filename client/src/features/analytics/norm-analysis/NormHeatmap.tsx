import { Box, Paper, Stack, Tooltip, Typography } from '@mui/material';
import type { NormHealthIssue, NormListItem } from '../../../api/normRegistryApi';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import {
  CONFIDENCE_COLOR,
  CONFIDENCE_LABEL,
  SEVERITY_BG,
  SEVERITY_COLOR,
  SEVERITY_LABEL,
  coefficientOfVariation,
  confidenceOf,
  heatmapCategory,
  iicNameWithoutCode,
  severityForNorm,
  type NormGroupMeta,
} from './normAnalysisMetrics';

interface Props {
  rows: NormListItem[];
  issues: NormHealthIssue[];
  groups: NormGroupMeta[];
  selectedId: number | null;
  onSelect: (row: NormListItem) => void;
}

/** Тепловая карта групп IIC: ячейки 132×84, цвет = серьёзность, две точки = серьёзность и достоверность. */
export function NormHeatmap({ rows, issues, groups, selectedId, onSelect }: Props) {
  if (rows.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary" data-testid="norm-analysis-heatmap-empty">
        Нет строк для тепловой карты.
      </Typography>
    );
  }

  const grouped = new Map<string, NormListItem[]>();
  for (const row of rows) {
    const cat = heatmapCategory(row, groups);
    const list = grouped.get(cat) ?? [];
    list.push(row);
    grouped.set(cat, list);
  }

  return (
    <Stack spacing={2} data-testid="norm-analysis-heatmap">
      {[...grouped.entries()].map(([category, norms]) => (
        <Paper key={category} variant="outlined" sx={{ p: 2 }}>
          <Typography variant="subtitle2" sx={{ mb: 1.25 }}>
            {category}
          </Typography>
          <Box sx={{ display: 'flex', gap: 1.25, flexWrap: 'wrap' }}>
            {norms.map((n) => {
              const sevKey = severityForNorm(issues, n);
              const confidence = confidenceOf(n);
              const isSelected = n.id === selectedId;
              const label = formatIisNorm({ code: n.groupCode, name: n.groupName });
              const cv = coefficientOfVariation(n.meanPerformance, n.stdDeviation);
              return (
                <Tooltip
                  key={n.id}
                  title={`${label} · ${SEVERITY_LABEL[sevKey]} · достоверность: ${CONFIDENCE_LABEL[confidence]}`}
                >
                  <Paper
                    component="button"
                    type="button"
                    onClick={() => onSelect(n)}
                    data-testid={`norm-heatmap-cell-${n.id}`}
                    sx={{
                      width: 132,
                      height: 84,
                      p: 1,
                      display: 'flex',
                      flexDirection: 'column',
                      justifyContent: 'space-between',
                      textAlign: 'left',
                      cursor: 'pointer',
                      font: 'inherit',
                      bgcolor: SEVERITY_BG[sevKey],
                      border: isSelected ? '2px solid' : '1px solid transparent',
                      borderColor: isSelected ? 'text.primary' : 'transparent',
                      '&:hover': { filter: 'brightness(0.97)' },
                    }}
                  >
                    <Typography variant="caption" sx={{ fontWeight: 600, lineHeight: 1.2 }} noWrap>
                      {iicNameWithoutCode(label)}
                    </Typography>
                    <Stack direction="row" justifyContent="space-between" alignItems="center">
                      <Box
                        sx={{
                          width: 8,
                          height: 8,
                          borderRadius: '50%',
                          bgcolor: SEVERITY_COLOR[sevKey],
                        }}
                      />
                      <Box
                        sx={{
                          width: 8,
                          height: 8,
                          borderRadius: '50%',
                          bgcolor: CONFIDENCE_COLOR[confidence],
                        }}
                      />
                      <Typography variant="caption" color="text.secondary">
                        CV {cv == null ? '—' : cv.toFixed(2)}
                      </Typography>
                    </Stack>
                  </Paper>
                </Tooltip>
              );
            })}
          </Box>
        </Paper>
      ))}
      <Typography variant="caption" color="text.secondary" sx={{ px: 1 }}>
        Цвет ячейки — серьёзность отклонения. Левая точка — серьёзность, правая — достоверность нормы.
      </Typography>
    </Stack>
  );
}
