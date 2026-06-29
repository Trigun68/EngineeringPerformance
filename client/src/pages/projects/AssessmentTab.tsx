import { useCallback, useEffect, useMemo, useState } from 'react';
import { Alert, Box, Button, Stack, Typography } from '@mui/material';
import TableViewIcon from '@mui/icons-material/TableView';
import PictureAsPdfIcon from '@mui/icons-material/PictureAsPdf';
import type { ColDef } from 'ag-grid-community';
import {
  exportAnalysisExcel,
  exportAnalysisPdf,
  fetchVersionAnalysis,
  type ProjectVersionDirection,
} from '../../api/analyticsApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { usePermission } from '../../hooks/usePermission';

interface AssessmentTabProps {
  projectId: number;
  versionId: number | null;
}

interface DirectionTreeRow extends ProjectVersionDirection {
  hierarchy: string[];
}

function flattenDirections(rows: ProjectVersionDirection[]): DirectionTreeRow[] {
  const byParent = rows.reduce<Map<number | null, ProjectVersionDirection[]>>((map, row) => {
    const key = row.parentId;
    const list = map.get(key) ?? [];
    list.push(row);
    map.set(key, list);
    return map;
  }, new Map());

  const result: DirectionTreeRow[] = [];
  const walk = (parentId: number | null, path: string[]) => {
    const children = byParent.get(parentId) ?? [];
    for (const child of children.sort((a, b) => a.code.localeCompare(b.code))) {
      const hierarchy = [...path, child.name];
      result.push({ ...child, hierarchy });
      walk(child.id, hierarchy);
    }
  };
  walk(null, []);
  return result;
}

export function AssessmentTab({ projectId, versionId }: AssessmentTabProps) {
  const { canRead } = usePermission('Assessment');
  const [rows, setRows] = useState<DirectionTreeRow[]>([]);
  const [summary, setSummary] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    if (!versionId || !canRead) return;
    setLoading(true);
    setError(null);
    try {
      const report = await fetchVersionAnalysis(projectId, versionId);
      setRows(flattenDirections(report.directions));
      const s = report.summary;
      setSummary(
        [
          s.weightedZScore != null ? `Z (взвеш.): ${s.weightedZScore}` : null,
          s.weightedZScorePercentile ? `Перцентиль: ${s.weightedZScorePercentile}` : null,
          s.totalFactLaborHours != null ? `Плановые часы: ${s.totalFactLaborHours}` : null,
        ]
          .filter(Boolean)
          .join(' · '),
      );
    } catch {
      setError('Не удалось загрузить отчёт оценки.');
    } finally {
      setLoading(false);
    }
  }, [projectId, versionId, canRead]);

  useEffect(() => {
    void load();
  }, [load]);

  const columnDefs = useMemo<ColDef<DirectionTreeRow>[]>(
    () => [
      { field: 'code', headerName: 'Код', maxWidth: 100 },
      { field: 'factPhysicalVolume', headerName: 'Факт. объём', maxWidth: 110 },
      { field: 'factLaborHours', headerName: 'Часы', maxWidth: 100 },
      { field: 'factProductivity', headerName: 'Произв.', maxWidth: 100 },
      { field: 'zScore', headerName: 'Z', maxWidth: 80 },
      { field: 'interpretation', headerName: 'Интерпретация', minWidth: 160 },
    ],
    [],
  );

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр оценки факта.</Alert>;
  }

  if (!versionId) {
    return <Alert severity="info">Выберите версию анализа.</Alert>;
  }

  return (
    <Box>
      {versionId && (
        <Stack direction="row" spacing={1} sx={{ mb: 2 }}>
          <Button
            size="small"
            variant="outlined"
            startIcon={<TableViewIcon />}
            onClick={() => void exportAnalysisExcel(projectId, versionId)}
          >
            Excel
          </Button>
          <Button
            size="small"
            variant="outlined"
            startIcon={<PictureAsPdfIcon />}
            onClick={() => void exportAnalysisPdf(projectId, versionId)}
          >
            PDF
          </Button>
        </Stack>
      )}
      {error && <Alert severity="error" sx={{ mb: 2 }}>{error}</Alert>}
      {summary && (
        <Typography variant="body2" color="text.secondary" sx={{ mb: 1 }}>
          {summary}
        </Typography>
      )}
      <Stack spacing={1}>
        <DataTable<DirectionTreeRow>
          rowData={rows}
          columnDefs={columnDefs}
          loading={loading}
          objectName="Assessment"
          gridId={`project-${projectId}-assessment`}
          treeData
          getDataPath={(row) => row.hierarchy}
          autoGroupColumnDef={{ headerName: 'Направление IIC', minWidth: 260, field: 'name' }}
        />
      </Stack>
    </Box>
  );
}
