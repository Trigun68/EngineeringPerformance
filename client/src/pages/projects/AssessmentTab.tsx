import { Alert, Box } from '@mui/material';
import { FactAnalysisReportView } from '../../features/projects/fact/FactAnalysisReportView';
import { usePermission } from '../../hooks/usePermission';

interface AssessmentTabProps {
  projectId: number;
  versionId: number | null;
}

/** Вкладка / встроенный отчёт анализа версии (карточки IIC + трудоёмкость). */
export function AssessmentTab({ projectId, versionId }: AssessmentTabProps) {
  const { canRead } = usePermission('Assessment');

  if (!canRead) {
    return <Alert severity="warning">Нет прав на просмотр оценки факта.</Alert>;
  }

  if (!versionId) {
    return <Alert severity="info">Выберите версию анализа.</Alert>;
  }

  return (
    <Box sx={{ height: '100%', minHeight: 420 }}>
      <FactAnalysisReportView projectId={projectId} versionId={versionId} />
    </Box>
  );
}
