import { useState } from 'react';
import {
  Box,
  IconButton,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableFooter,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import ChevronRightIcon from '@mui/icons-material/ChevronRight';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import type { ForecastIicVolumeRow } from '../../../../api/analyticsApi';
import { fmt, formatReportVolume, INTERPRETATION_COLOR } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

interface ForecastTableProps {
  rows: ForecastIicVolumeRow[];
}

interface ForecastTreeRow extends ForecastIicVolumeRow {
  children: ForecastTreeRow[];
  level: number;
}

function buildForecastTree(rows: ForecastIicVolumeRow[]): ForecastTreeRow[] {
  const byId = new Map<number, ForecastTreeRow>();
  rows.forEach((r) => byId.set(r.workDirectionId, { ...r, children: [], level: 0 }));

  const roots: ForecastTreeRow[] = [];
  byId.forEach((node) => {
    const parent = node.parentId != null ? byId.get(node.parentId) : undefined;
    if (parent) parent.children.push(node);
    else roots.push(node);
  });

  const assignLevel = (nodes: ForecastTreeRow[], level: number) => {
    for (const node of nodes) {
      node.level = level;
      assignLevel(node.children, level + 1);
    }
  };
  assignLevel(roots, 0);
  return roots;
}

export function ForecastTable({ rows }: ForecastTableProps) {
  const [collapsed, setCollapsed] = useState<number[]>([]);
  const tree = buildForecastTree(rows);
  const rootsOnly = rows.filter((r) => r.parentId == null);
  const sum = (key: 'optimisticHours' | 'baseHours' | 'pessimisticHours') =>
    rootsOnly.reduce((s, r) => s + (r[key] ?? 0), 0);

  const toggle = (id: number) =>
    setCollapsed((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));

  const visible: ForecastTreeRow[] = [];
  const walk = (list: ForecastTreeRow[], hidden: boolean) => {
    for (const row of list) {
      if (!hidden) visible.push(row);
      walk(row.children, hidden || collapsed.includes(row.workDirectionId));
    }
  };
  walk(tree, false);

  if (rows.length === 0) {
    return (
      <Typography variant="body2" color="text.secondary">
        Плановые объёмы по направлениям не заданы — сценарии прогноза недоступны.
      </Typography>
    );
  }

  return (
    <TableContainer>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell sx={{ minWidth: 220 }}>Направление работ</TableCell>
            <TableCell align="right">Физобъём</TableCell>
            <TableCell align="right">Сгруппированный физобъём</TableCell>
            <TableCell align="right">Оптимистичный, ч</TableCell>
            <TableCell align="right">Базовый, ч</TableCell>
            <TableCell align="right">Пессимистичный, ч</TableCell>
            <TableCell align="right">Разброс, ч</TableCell>
          </TableRow>
        </TableHead>
        <TableBody>
          {visible.map((row) => {
            const hasChildren = row.children.length > 0;
            const isCollapsed = collapsed.includes(row.workDirectionId);
            return (
              <TableRow key={row.workDirectionId} sx={row.level === 0 ? { bgcolor: wpColors.pageBg } : undefined}>
                <TableCell>
                  <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.5, pl: row.level * 2.5 }}>
                    {hasChildren ? (
                      <IconButton
                        size="small"
                        onClick={() => toggle(row.workDirectionId)}
                        aria-label={isCollapsed ? `Развернуть ${row.name}` : `Свернуть ${row.name}`}
                        sx={{ p: 0.25 }}
                      >
                        {isCollapsed ? <ChevronRightIcon fontSize="small" /> : <ExpandMoreIcon fontSize="small" />}
                      </IconButton>
                    ) : (
                      <Box sx={{ width: 26, flexShrink: 0 }} />
                    )}
                    <Typography variant="body2" sx={{ fontWeight: row.level === 0 ? 600 : 400 }}>
                      {row.name}
                    </Typography>
                  </Box>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
                    {formatReportVolume(row.ownPlannedVolume, row.unitName)}
                  </Typography>
                </TableCell>
                <TableCell align="right">
                  <Typography variant="caption" sx={{ fontFamily: 'monospace' }}>
                    {formatReportVolume(row.groupedPhysicalVolume, row.unitName)}
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
            );
          })}
        </TableBody>
        <TableFooter>
          <TableRow>
            <TableCell sx={{ fontWeight: 600 }}>Итого по проекту</TableCell>
            <TableCell />
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
  );
}
