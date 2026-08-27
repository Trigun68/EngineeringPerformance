import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableFooter,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import type { ForecastIicVolumeRow } from '../../../../api/analyticsApi';
import { fmt, INTERPRETATION_COLOR } from '../reportCalc';

interface ForecastTableProps {
  rows: ForecastIicVolumeRow[];
}

export function ForecastTable({ rows }: ForecastTableProps) {
  const sum = (key: 'optimisticHours' | 'baseHours' | 'pessimisticHours') =>
    rows.reduce((s, r) => s + (r[key] ?? 0), 0);

  if (rows.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        Плановые объёмы по направлениям не заданы — сценарии прогноза недоступны.
      </Typography>
    );
  }

  return (
    <>
      <Typography variant="body2" color="text.secondary" sx={{ mb: 2 }}>
        Сценарии рассчитаны по плановому объёму и нормам снимка версии. Отклонения применяются в лог-пространстве:
        оптимистичный = exp(ln(норма) − 0.5σ), базовый = exp(ln(норма)), пессимистичный = exp(ln(норма) + 0.5σ).
      </Typography>
      <TableContainer>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell sx={{ minWidth: 220 }}>Направление работ</TableCell>
              <TableCell align="right">Плановый объём</TableCell>
              <TableCell align="right">Оптимистичный, ч</TableCell>
              <TableCell align="right">Базовый, ч</TableCell>
              <TableCell align="right">Пессимистичный, ч</TableCell>
              <TableCell align="right">Разброс, ч</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {rows.map((row) => (
              <TableRow key={row.workDirectionId}>
                <TableCell sx={{ fontWeight: 500 }}>{row.name}</TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
                    {fmt(row.plannedVolume, 1)} {row.unitName ?? ''}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace', color: INTERPRETATION_COLOR.Excellent }}>
                    {fmt(row.optimisticHours, 1)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                    {fmt(row.baseHours, 1)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace', color: INTERPRETATION_COLOR.Critical }}>
                    {fmt(row.pessimisticHours, 1)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace', color: 'text.secondary' }}>
                    {fmt((row.pessimisticHours ?? 0) - (row.optimisticHours ?? 0), 1)}
                  </Typography>
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
          <TableFooter>
            <TableRow>
              <TableCell sx={{ fontWeight: 600 }}>Итого по проекту</TableCell>
              <TableCell />
              <TableCell align="right">
                <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                  {fmt(sum('optimisticHours'), 1)}
                </Typography>
              </TableCell>
              <TableCell align="right">
                <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                  {fmt(sum('baseHours'), 1)}
                </Typography>
              </TableCell>
              <TableCell align="right">
                <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                  {fmt(sum('pessimisticHours'), 1)}
                </Typography>
              </TableCell>
              <TableCell align="right">
                <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                  {fmt(sum('pessimisticHours') - sum('optimisticHours'), 1)}
                </Typography>
              </TableCell>
            </TableRow>
          </TableFooter>
        </Table>
      </TableContainer>
    </>
  );
}
