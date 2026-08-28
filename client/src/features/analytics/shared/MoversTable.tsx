import {
  Paper,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import type { MoverRowDto } from '../../../api/projectTrendApi';

interface Props {
  rows: MoverRowDto[];
  onProjectClick?: (projectId: number) => void;
  testId?: string;
}

function fmt(n: number | null, digits = 2): string {
  if (n == null) return '—';
  return n.toLocaleString('ru-RU', { minimumFractionDigits: digits, maximumFractionDigits: digits });
}

/** Таблица проектов последнего факт-среза, сортировка по |вклад| уже на API. */
export function MoversTable({ rows, onProjectClick, testId }: Props) {
  if (rows.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary" data-testid={testId}>
        Нет вкладов проектов в последний срез факта.
      </Typography>
    );
  }

  return (
    <Paper variant="outlined" data-testid={testId} sx={{ overflow: 'auto' }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Код</TableCell>
            <TableCell>Проект</TableCell>
            <TableCell align="right">Z</TableCell>
            <TableCell align="right">Вес, ч</TableCell>
            <TableCell align="right">Вклад</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row) => (
            <TableRow
              key={row.projectId}
              hover={Boolean(onProjectClick)}
              sx={{ cursor: onProjectClick ? 'pointer' : 'default' }}
              onClick={() => onProjectClick?.(row.projectId)}
              data-testid={`trend-mover-${row.projectId}`}
            >
              <TableCell>{row.projectCode}</TableCell>
              <TableCell>{row.projectName}</TableCell>
              <TableCell align="right">{fmt(row.zScore)}</TableCell>
              <TableCell align="right">{fmt(row.weight, 0)}</TableCell>
              <TableCell align="right">{fmt(row.contribution, 4)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Paper>
  );
}
