import { useMemo, useState } from 'react';
import { Box, Chip, InputAdornment, Stack, TextField, Typography } from '@mui/material';
import SearchIcon from '@mui/icons-material/Search';
import type { ColDef, ValueFormatterParams } from 'ag-grid-community';
import { DataTable } from '../../../../components/DataTable/DataTable';
import type { PerformanceTreeRow } from '../reportCalc';
import {
  getInterpretation,
  INTERPRETATION_COLOR,
  INTERPRETATION_SHORT_RU,
  MUTED_COLOR,
  resolveSubtreePhysicalVolume,
  type Interpretation,
} from '../reportCalc';

interface PhysicalVolumeGroupsRow {
  id: number;
  name: string;
  physicalVolume: number | null;
  laborHours: number | null;
  productivity: number | null;
  normProductivity: number | null;
  zScore: number | null;
  interpretation: Interpretation | null;
}

interface PhysicalVolumeGroupsTableProps {
  rows: PerformanceTreeRow[];
}

const INTERPRETATION_FILTERS: Interpretation[] = ['Excellent', 'Good', 'Satisfactory', 'NeedsAttention', 'Critical'];

function numberFormatter(digits: number) {
  return (params: ValueFormatterParams<PhysicalVolumeGroupsRow>) => {
    const value = params.value as number | null;
    if (value === null || value === undefined || Number.isNaN(value)) return '—';
    return value.toLocaleString('ru-RU', { minimumFractionDigits: digits, maximumFractionDigits: digits });
  };
}

/**
 * Плоская таблица групп ИИС с собственными факт-часами — дополняет
 * иерархическое дерево PerformanceTree, но допускает сортировку и фильтрацию по любому
 * столбцу и быстрый поиск по коду/наименованию, что в дереве неудобно на крупных проектах.
 */
export function PhysicalVolumeGroupsTable({ rows }: PhysicalVolumeGroupsTableProps) {
  const [search, setSearch] = useState('');
  const [activeFilter, setActiveFilter] = useState<Interpretation | null>(null);

  const tableRows = useMemo<PhysicalVolumeGroupsRow[]>(
    () =>
      rows.map((row) => ({
        id: row.workDirectionId,
        name: row.workDirectionName,
        physicalVolume: resolveSubtreePhysicalVolume(row),
        laborHours: row.factLaborHours ?? row.subtreeHours,
        productivity: row.factProductivity,
        normProductivity: row.normProductivity,
        zScore: row.zScore,
        interpretation: getInterpretation(row.zScore),
      })),
    [rows],
  );

  const filtered = useMemo(() => {
    const term = search.trim().toLowerCase();
    return tableRows.filter((row) => {
      if (activeFilter && row.interpretation !== activeFilter) return false;
      if (!term) return true;
      return row.name.toLowerCase().includes(term);
    });
  }, [tableRows, search, activeFilter]);

  const columnDefs = useMemo<ColDef<PhysicalVolumeGroupsRow>[]>(
    () => [
      { field: 'name', headerName: 'Направление IIC', flex: 1, minWidth: 280 },
      {
        field: 'physicalVolume',
        headerName: 'Факт. объём',
        width: 130,
        valueFormatter: numberFormatter(1),
        type: 'rightAligned',
      },
      {
        field: 'laborHours',
        headerName: 'Часы',
        width: 110,
        valueFormatter: numberFormatter(1),
        type: 'rightAligned',
      },
      {
        field: 'productivity',
        headerName: 'Произв. (факт)',
        width: 130,
        valueFormatter: numberFormatter(2),
        type: 'rightAligned',
      },
      {
        field: 'normProductivity',
        headerName: 'Норма IIC',
        width: 120,
        valueFormatter: numberFormatter(2),
        type: 'rightAligned',
      },
      {
        field: 'zScore',
        headerName: 'Оценка Z',
        width: 90,
        type: 'rightAligned',
        valueFormatter: numberFormatter(2),
        cellStyle: (params) => ({
          fontWeight: 700,
          color: params.data?.interpretation ? INTERPRETATION_COLOR[params.data.interpretation] : MUTED_COLOR,
        }),
      },
      {
        field: 'interpretation',
        headerName: 'Оценка',
        width: 150,
        valueFormatter: (params: ValueFormatterParams<PhysicalVolumeGroupsRow>) =>
          params.value ? INTERPRETATION_SHORT_RU[params.value as Interpretation] : '—',
        cellStyle: (params) => ({
          fontWeight: 600,
          color: params.data?.interpretation ? INTERPRETATION_COLOR[params.data.interpretation] : MUTED_COLOR,
        }),
      },
    ],
    [],
  );

  return (
    <Stack spacing={1.5} sx={{ height: 520 }} data-testid="report-physical-volume-groups">
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ sm: 'center' }}>
        <TextField
          size="small"
          placeholder="Поиск по наименованию"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          sx={{ width: { xs: '100%', sm: 320 } }}
          InputProps={{
            startAdornment: (
              <InputAdornment position="start">
                <SearchIcon fontSize="small" />
              </InputAdornment>
            ),
          }}
        />
        <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
          {INTERPRETATION_FILTERS.map((interp) => (
            <Chip
              key={interp}
              label={INTERPRETATION_SHORT_RU[interp]}
              size="small"
              onClick={() => setActiveFilter((prev) => (prev === interp ? null : interp))}
              sx={{
                fontWeight: 600,
                color: activeFilter === interp ? '#FFFFFF' : INTERPRETATION_COLOR[interp],
                bgcolor: activeFilter === interp ? INTERPRETATION_COLOR[interp] : 'transparent',
                border: `1px solid ${INTERPRETATION_COLOR[interp]}`,
              }}
            />
          ))}
        </Stack>
        <Typography variant="caption" color="text.secondary" sx={{ ml: { sm: 'auto' } }}>
          Показано {filtered.length} из {tableRows.length} групп
        </Typography>
      </Stack>

      <Box sx={{ flex: 1, minHeight: 0 }}>
        <DataTable<PhysicalVolumeGroupsRow>
          rowData={filtered}
          columnDefs={columnDefs}
          objectName="Assessment"
          gridId="report-physical-volume-groups"
          pagination
          paginationPageSize={25}
        />
      </Box>
    </Stack>
  );
}
