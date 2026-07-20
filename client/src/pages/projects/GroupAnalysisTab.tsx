import { useEffect, useMemo, useState } from 'react';
import { Link as RouterLink } from 'react-router-dom';
import {
  Alert,
  Box,
  Chip,
  Stack,
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';
import type { ColDef } from 'ag-grid-community';
import { fetchGroupAnalysis, type GroupAnalysisResult } from '../../api/analyticsApi';
import { DataTable } from '../../components/DataTable/DataTable';
import { usePermission } from '../../hooks/usePermission';

interface GroupAnalysisTabProps {
  projectId: number;
}

function groupCriterionLabel(criterion: string, value: string | null): string {
  if (criterion === 'Program' && value) return `Программа: ${value}`;
  if (criterion === 'Customer' && value) return `Заказчик: ${value}`;
  return 'Только текущий проект (не заданы программа и заказчик)';
}

export function GroupAnalysisTab({ projectId }: GroupAnalysisTabProps) {
  const { canRead } = usePermission('GroupAnalysis');
  const [data, setData] = useState<GroupAnalysisResult | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (!canRead) return;
    setLoading(true);
    setError(null);
    void fetchGroupAnalysis(projectId)
      .then(setData)
      .catch(() => setError('Не удалось загрузить групповой анализ.'))
      .finally(() => setLoading(false));
  }, [projectId, canRead]);

  const columnDefs = useMemo<ColDef<GroupAnalysisResult['projects'][number]>[]>(
    () => [
      { field: 'projectCode', headerName: 'Код', minWidth: 100 },
      { field: 'projectName', headerName: 'Проект', minWidth: 180 },
      { field: 'versionName', headerName: 'Версия факта', minWidth: 140 },
      { field: 'totalFactLaborHours', headerName: 'Часы факта', width: 120 },
      { field: 'weightedZScore', headerName: 'Z', width: 90 },
      { field: 'weightedZInterpretation', headerName: 'Интерпретация', minWidth: 140 },
    ],
    [],
  );

  if (!canRead) {
    return <Alert severity="warning">Нет прав на групповой анализ.</Alert>;
  }

  if (error) return <Alert severity="error">{error}</Alert>;

  return (
    <Box>
      <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 2 }}>
        <Typography variant="h6">Групповой анализ</Typography>
        {data && (
          <Chip
            size="small"
            label={groupCriterionLabel(data.groupCriterion, data.groupValue)}
            color={data.groupCriterion === 'Single' ? 'default' : 'primary'}
            variant="outlined"
          />
        )}
      </Stack>

      {data && (data.aggregateWeightedZScore != null || data.aggregateWeightedZInterpretation) && (
        <Table size="small" sx={{ mb: 2, maxWidth: 480 }}>
          <TableHead>
            <TableRow>
              <TableCell>Средневзвешенный Z по группе</TableCell>
              <TableCell>Интерпретация</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            <TableRow>
              <TableCell>{data.aggregateWeightedZScore?.toFixed(2) ?? '—'}</TableCell>
              <TableCell>{data.aggregateWeightedZInterpretation ?? '—'}</TableCell>
            </TableRow>
          </TableBody>
        </Table>
      )}

      <DataTable
        rowData={data?.projects ?? []}
        columnDefs={columnDefs}
        loading={loading}
        objectName="GroupAnalysis"
        gridId={`project-${projectId}-group-analysis`}
        contextMenuItems={[
          {
            name: 'Открыть проект',
            action: (row) => {
              if (row) window.location.assign(`/projects/${row.projectId}`);
            },
            requiredLevel: 1,
          },
        ]}
      />

      {data && data.projects.length === 0 && !loading && (
        <Alert severity="info" sx={{ mt: 2 }}>
          Нет доступных проектов в группе.
        </Alert>
      )}

      <Typography variant="body2" color="text.secondary" sx={{ mt: 2 }}>
        Сравнение основано на последней факт-версии каждого проекта.{' '}
        <RouterLink to={`/projects/${projectId}`}>Вернуться к карточке проекта</RouterLink>
      </Typography>
    </Box>
  );
}
