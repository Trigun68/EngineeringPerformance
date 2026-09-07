import { useState } from 'react';
import {
  Box,
  Chip,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import type { PerformanceTreeRow } from '../reportCalc';
import { fmt, fmtSigned, getInterpretation, INTERPRETATION_SHORT_RU, interpretationColor, displayPhysicalVolume } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

interface PerformanceTreeProps {
  rows: PerformanceTreeRow[];
}

export function PerformanceTree({ rows }: PerformanceTreeProps) {
  const [collapsed, setCollapsed] = useState<number[]>([]);

  const toggle = (id: number) =>
    setCollapsed((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));

  const visible: PerformanceTreeRow[] = [];
  const walk = (list: PerformanceTreeRow[], hidden: boolean) => {
    for (const row of list) {
      if (!hidden) visible.push(row);
      walk(row.children, hidden || collapsed.includes(row.workDirectionId));
    }
  };
  walk(rows, false);

  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell sx={{ minWidth: 240 }}>Направление работ</TableCell>
            <TableCell align="right">Факт. объём</TableCell>
            <TableCell align="right">Часы</TableCell>
            <TableCell align="right">Часы поддерева</TableCell>
            <TableCell align="right">Факт. произв.</TableCell>
            <TableCell align="right">Норма</TableCell>
            <TableCell align="right">σ нормы</TableCell>
            <TableCell align="right">Оценка Z</TableCell>
            <TableCell>Интерпретация</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {visible.map((row) => {
            const hasChildren = row.children.length > 0;
            const isCollapsed = collapsed.includes(row.workDirectionId);
            const color = interpretationColor(row.zScore);
            const interpretation = getInterpretation(row.zScore);
            const volume = displayPhysicalVolume(row);
            return (
              <TableRow key={row.workDirectionId} sx={row.level === 0 ? { bgcolor: wpColors.pageBg } : undefined}>
                <TableCell>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, pl: row.level * 2.5 }}>
                    {hasChildren ? (
                      <IconButton
                        size="small"
                        onClick={() => toggle(row.workDirectionId)}
                        aria-label={isCollapsed ? `Развернуть ${row.workDirectionName}` : `Свернуть ${row.workDirectionName}`}
                        sx={{ p: 0.25 }}
                      >
                        {isCollapsed ? <ChevronRightIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
                      </IconButton>
                    ) : (
                      <Box sx={{ width: 26, flexShrink: 0 }} />
                    )}
                    <Typography variant="body2" sx={{ fontWeight: row.level === 0 ? 600 : 400 }}>
                      {row.workDirectionName}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
                    {volume === null
                      ? '—'
                      : `${fmt(volume, 1)} ${row.normUnitName ?? ''}`.trim()}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
                    {fmt(row.factLaborHours, 1)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace', color: wpColors.heading }}>
                    {fmt(row.subtreeHours, 1)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
                    {fmt(row.factProductivity, 2)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace', color: wpColors.heading }}>
                    {fmt(row.normProductivity, 2)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace', color: wpColors.heading }}>
                    {fmt(row.normStdDev, 2)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="body2" sx={{ fontFamily: 'monospace', fontWeight: 600, color }}>
                    {fmtSigned(row.zScore)}
                  </Typography>
                </TableCell>
                <TableCell>
                  {interpretation ? (
                    <Chip
                      size="small"
                      variant="outlined"
                      label={INTERPRETATION_SHORT_RU[interpretation]}
                      sx={{ color, borderColor: color, fontSize: 11 }}
                    />
                  ) : (
                    <Typography variant="caption" color="text.secondary">
                      Н/Д
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
