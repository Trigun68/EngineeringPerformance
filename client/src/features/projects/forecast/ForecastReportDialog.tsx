import { Dialog, DialogContent, DialogTitle, IconButton, Typography } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import { formatProjectModalTitle } from '../utils/projectModalTitle';
import { ForecastReportView } from './ForecastReportView';
import type { ProjectsPageForecastSummaryRow } from '../types';

export interface ForecastReportDialogProps {
  open: boolean;
  projectId: number;
  versionId: number | null;
  projectName: string;
  projectCode: string;
  versionLabel: string;
  summaryRows: ProjectsPageForecastSummaryRow[];
  onClose: () => void;
}

export function ForecastReportDialog({
  open,
  projectId,
  versionId,
  projectName,
  projectCode,
  versionLabel,
  summaryRows,
  onClose,
}: ForecastReportDialogProps) {
  const title = formatProjectModalTitle('Прогноз трудозатрат', {
    code: projectCode,
    name: projectName,
    version: versionLabel,
  });

  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="xl"
      fullWidth
      data-testid="forecast-report-dialog"
      PaperProps={{ sx: { height: '90vh' } }}
    >
      <DialogTitle sx={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', pr: 1 }}>
        <Typography variant="h6" component="span" sx={{ flex: 1, pr: 1, whiteSpace: 'normal' }}>
          {title}
        </Typography>
        <IconButton aria-label="Закрыть" onClick={onClose} size="small">
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers sx={{ p: 2, display: 'flex', flexDirection: 'column', minHeight: 0 }}>
        {versionId != null && (
          <ForecastReportView
            projectId={projectId}
            versionId={versionId}
            summaryRows={summaryRows}
          />
        )}
      </DialogContent>
    </Dialog>
  );
}
