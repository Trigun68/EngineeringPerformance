import { useMemo } from 'react';
import { Box, Chip } from '@mui/material';
import type { ColDef, ValueFormatterParams } from 'ag-grid-community';
import { DataTable } from '../../../components/DataTable/DataTable';
import { OBJECT_NAME } from '../../../constants/accessControlConstants';
import type { ProductivityTrendTransition } from '../../../api/productivityTrendApi';
import {
  formulaLabel,
  formatNum,
  pair,
  VERDICT_COLOR,
  VERDICT_LABEL,
} from './productivityTrendLabels';

export interface ProductivityTrendTransitionsGridProps {
  rowData: ProductivityTrendTransition[];
}

export function ProductivityTrendTransitionsGrid({ rowData }: ProductivityTrendTransitionsGridProps) {
  const columnDefs = useMemo<ColDef<ProductivityTrendTransition>[]>(
    () => [
      { field: 'groupName', headerName: 'Группа IIC', flex: 1, minWidth: 220 },
      {
        field: 'effectiveFrom',
        headerName: 'Дата смены',
        width: 120,
        valueFormatter: (p: ValueFormatterParams<ProductivityTrendTransition>) => {
          const raw = p.value as string | undefined;
          if (!raw) return '—';
          const d = new Date(raw);
          return Number.isNaN(d.getTime()) ? raw : d.toLocaleDateString('ru-RU');
        },
      },
      {
        headerName: 'Формула',
        width: 220,
        valueGetter: (p) =>
          p.data ? `${formulaLabel(p.data.oldFormulaType)} → ${formulaLabel(p.data.newFormulaType)}` : '',
      },
      {
        headerName: 'Часы P10',
        width: 140,
        valueGetter: (p) => (p.data ? pair(p.data.hoursOldP10, p.data.hoursNewP10) : ''),
      },
      {
        headerName: 'Часы медиана',
        width: 150,
        valueGetter: (p) => (p.data ? pair(p.data.hoursOldMedian, p.data.hoursNewMedian) : ''),
      },
      {
        headerName: 'Часы P90',
        width: 140,
        valueGetter: (p) => (p.data ? pair(p.data.hoursOldP90, p.data.hoursNewP90) : ''),
      },
      {
        field: 'standardEffectHours',
        headerName: 'Эффект нормы',
        width: 130,
        valueFormatter: (p) => formatNum(p.value as number | null),
      },
      {
        field: 'mixEffectHours',
        headerName: 'Эффект объёма',
        width: 130,
        valueFormatter: (p) => formatNum(p.value as number | null),
      },
      {
        field: 'verdict',
        headerName: 'Вердикт',
        width: 150,
        cellRenderer: (p: { value?: string }) => {
          const v = String(p.value ?? '');
          return (
            <Chip
              size="small"
              color={VERDICT_COLOR[v] ?? 'default'}
              label={VERDICT_LABEL[v] ?? (v || '—')}
              aria-label={VERDICT_LABEL[v] ?? v}
            />
          );
        },
      },
      {
        field: 'curvesCross',
        headerName: 'Пересечение',
        width: 120,
        valueFormatter: (p) => (p.value ? 'Да' : 'Нет'),
      },
      {
        field: 'logVolumeLtOne',
        headerName: 'ln V<1',
        width: 100,
        valueFormatter: (p) => (p.value ? 'Да' : 'Нет'),
      },
    ],
    [],
  );

  return (
    <Box sx={{ height: 360 }} data-testid="prod-trend-transitions">
      <DataTable
        rowData={rowData}
        columnDefs={columnDefs}
        objectName={OBJECT_NAME.GroupTrend}
        gridId="analytics-productivity-trend-transitions"
        getRowIdValue={(row) => `${row.groupId}-${row.oldNormId}-${row.newNormId}`}
        gridTestId="prod-trend-transitions-grid"
        height="100%"
      />
    </Box>
  );
}
