import { Paper, Table, TableBody, TableCell, TableContainer, TableHead, TableRow, Typography } from '@mui/material';
import type { WorkCostPeriod } from '../../api/registersApi';
import { formatDate } from '../../utils/formatDateTime';

export function periodDateKey(iso: string): string {
  return iso.slice(0, 10);
}

export interface WorkCostsPeriodsPanelProps {
  periods: WorkCostPeriod[];
  selectedFrom: string | null;
  onSelect: (effectiveFrom: string) => void;
  onEdit: (effectiveFrom: string) => void;
}

export function WorkCostsPeriodsPanel({
  periods,
  selectedFrom,
  onSelect,
  onEdit,
}: WorkCostsPeriodsPanelProps) {
  return (
    <Paper
      variant="outlined"
      data-testid="work-costs-periods"
      sx={{
        width: 300,
        flexShrink: 0,
        display: 'flex',
        flexDirection: 'column',
        minHeight: 0,
        overflow: 'hidden',
      }}
    >
      <Typography variant="subtitle2" sx={{ px: 1.5, py: 1, flexShrink: 0 }}>
        Периоды
      </Typography>
      {periods.length === 0 ? (
        <Typography variant="body2" color="text.secondary" sx={{ px: 1.5, pb: 1.5 }}>
          Нет установленных расценок
        </Typography>
      ) : (
        <TableContainer sx={{ flex: 1, minHeight: 0 }}>
          <Table size="small" stickyHeader>
            <TableHead>
              <TableRow>
                <TableCell>Действует с</TableCell>
                <TableCell>По</TableCell>
                <TableCell align="right">Групп</TableCell>
              </TableRow>
            </TableHead>
            <TableBody>
              {periods.map((period) => {
                const from = periodDateKey(period.effectiveFrom);
                return (
                  <TableRow
                    key={from}
                    hover
                    selected={selectedFrom === from}
                    onClick={() => onSelect(from)}
                    onDoubleClick={() => onEdit(from)}
                    data-testid={`work-cost-period-${from}`}
                    sx={{ cursor: 'pointer' }}
                  >
                    <TableCell>{formatDate(period.effectiveFrom)}</TableCell>
                    <TableCell>
                      {period.effectiveToVaries
                        ? 'разн.'
                        : period.effectiveTo
                          ? formatDate(period.effectiveTo)
                          : '—'}
                    </TableCell>
                    <TableCell align="right">{period.directionCount}</TableCell>
                  </TableRow>
                );
              })}
            </TableBody>
          </Table>
        </TableContainer>
      )}
    </Paper>
  );
}
