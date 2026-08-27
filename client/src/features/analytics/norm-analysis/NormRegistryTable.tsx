import {
  Box,
  Chip,
  Paper,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Tooltip,
  Typography,
} from '@mui/material';
import ScheduleOutlinedIcon from '@mui/icons-material/ScheduleOutlined';
import type { NormHealthIssue, NormListItem } from '../../../api/normRegistryApi';
import { formatIisNorm } from '../../../utils/formatIisNorm';
import { NormSparkline } from './NormSparkline';
import {
  CONFIDENCE_COLOR,
  CONFIDENCE_LABEL,
  SEVERITY_BG,
  SEVERITY_COLOR,
  SEVERITY_LABEL,
  STATUS_LABEL,
  coefficientOfVariation,
  confidenceOf,
  cvHeatBackground,
  driftMeansForGroup,
  isStaleActiveReview,
  monthsSince,
  severityForNorm,
} from './normAnalysisMetrics';

interface Props {
  rows: NormListItem[];
  allRows: NormListItem[];
  loading: boolean;
  issues: NormHealthIssue[];
  medianCv: number | null;
  selectedId: number | null;
  onRowClick: (row: NormListItem) => void;
}

export function NormRegistryTable({
  rows,
  allRows,
  loading,
  issues,
  medianCv,
  selectedId,
  onRowClick,
}: Props) {
  if (!loading && rows.length === 0) {
    return (
      <Box
        sx={{ p: 3, textAlign: 'center', border: '1px dashed', borderColor: 'divider', borderRadius: 1 }}
        data-testid="norm-analysis-empty"
      >
        <Typography color="text.secondary">Нет норм по текущим фильтрам</Typography>
      </Box>
    );
  }

  return (
    <TableContainer
      component={Paper}
      variant="outlined"
      sx={{ maxHeight: 520 }}
      data-testid="norm-analysis-table"
    >
      <Table size="small" stickyHeader data-testid="norm-analysis-grid">
        <TableHead>
          <TableRow>
            <TableCell sx={{ minWidth: 210 }}>Группа IIC</TableCell>
            <TableCell sx={{ minWidth: 170 }}>Серьёзность</TableCell>
            <TableCell>Формула</TableCell>
            <TableCell align="right">μ</TableCell>
            <TableCell align="right">σ</TableCell>
            <TableCell align="right">
              <Tooltip title="Коэффициент вариации σ/μ — насколько велик разброс относительно среднего. Подсвечивается, если заметно выше медианы по всем нормам.">
                <span>CV</span>
              </Tooltip>
            </TableCell>
            <TableCell sx={{ minWidth: 120 }}>
              <Tooltip title="Достоверность нормы: чем больше версий факта (N), тем надёжнее оценка.">
                <span>Достоверность</span>
              </Tooltip>
            </TableCell>
            <TableCell sx={{ minWidth: 90 }}>
              <Tooltip title="Динамика μ по ревизиям той же группы IIC — направление дрейфа важнее единичного выхода за допуск.">
                <span>Дрейф</span>
              </Tooltip>
            </TableCell>
            <TableCell>Статус</TableCell>
            <TableCell align="right">Ревизия</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((n) => {
            const cv = coefficientOfVariation(n.meanPerformance, n.stdDeviation);
            const sevKey = severityForNorm(issues, n);
            const isSelected = n.id === selectedId;
            const months = monthsSince(n.effectiveFrom);
            const stale = isStaleActiveReview(n.status, months);
            const confidence = confidenceOf(n);
            const label = formatIisNorm({ code: n.groupCode, name: n.groupName });
            const drift = driftMeansForGroup(allRows, n.groupId);
            return (
              <TableRow
                key={n.id}
                hover
                selected={isSelected}
                onClick={() => onRowClick(n)}
                sx={{ cursor: 'pointer' }}
                data-testid={`norm-analysis-table-row-${n.id}`}
              >
                <TableCell sx={{ fontWeight: isSelected ? 600 : 400 }}>{label}</TableCell>
                <TableCell>
                  <Stack direction="row" spacing={0.75} alignItems="center">
                    <Box
                      sx={{
                        width: 9,
                        height: 9,
                        borderRadius: '50%',
                        flexShrink: 0,
                        bgcolor: SEVERITY_COLOR[sevKey],
                      }}
                    />
                    <Typography variant="body2" noWrap>
                      {SEVERITY_LABEL[sevKey]}
                    </Typography>
                  </Stack>
                </TableCell>
                <TableCell>{n.formulaType}</TableCell>
                <TableCell align="right">
                  {n.meanPerformance == null ? '—' : n.meanPerformance.toFixed(3)}
                </TableCell>
                <TableCell align="right">
                  {n.stdDeviation == null ? '—' : n.stdDeviation.toFixed(3)}
                </TableCell>
                <TableCell align="right" sx={{ backgroundColor: cvHeatBackground(cv, medianCv) }}>
                  {cv == null ? '—' : cv.toFixed(2)}
                </TableCell>
                <TableCell>
                  <Tooltip title={`N = ${n.totalVersions} версий`}>
                    <Stack direction="row" spacing={0.75} alignItems="center">
                      <Box
                        sx={{
                          width: 8,
                          height: 8,
                          borderRadius: '50%',
                          flexShrink: 0,
                          bgcolor: CONFIDENCE_COLOR[confidence],
                        }}
                      />
                      <Typography variant="body2" noWrap>
                        {CONFIDENCE_LABEL[confidence]}
                      </Typography>
                    </Stack>
                  </Tooltip>
                </TableCell>
                <TableCell>
                  {drift.length >= 2 ? (
                    <Box sx={{ color: SEVERITY_COLOR[sevKey], lineHeight: 0 }}>
                      <NormSparkline values={drift} color={SEVERITY_COLOR[sevKey]} />
                    </Box>
                  ) : (
                    <Typography variant="body2" color="text.secondary">
                      —
                    </Typography>
                  )}
                </TableCell>
                <TableCell>
                  <Chip
                    size="small"
                    label={STATUS_LABEL[n.status] ?? n.status}
                    color={n.status === 'Active' ? 'success' : n.status === 'Future' ? 'info' : 'default'}
                    variant={n.status === 'Historical' ? 'outlined' : 'filled'}
                  />
                </TableCell>
                <TableCell align="right">
                  {stale ? (
                    <Tooltip title="Норма активна и не пересматривалась 12+ месяцев">
                      <Chip
                        size="small"
                        icon={<ScheduleOutlinedIcon fontSize="small" />}
                        label={`${months} мес.`}
                        sx={{
                          bgcolor: SEVERITY_BG.Warning,
                          color: SEVERITY_COLOR.Warning,
                          fontWeight: 600,
                        }}
                      />
                    </Tooltip>
                  ) : (
                    <Typography variant="body2" color="text.secondary">
                      {months == null ? '—' : `${months} мес.`}
                    </Typography>
                  )}
                </TableCell>
              </TableRow>
            );
          })}
        </TableBody>
      </Table>
    </TableContainer>
  );
}
