import { Dialog, DialogContent, DialogTitle, IconButton, Typography } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import { formatProjectModalTitle } from '../utils/projectModalTitle';
import { FactAnalysisReportView } from './FactAnalysisReportView';

export interface FactAnalysisDialogProps {
  open: boolean;
  projectId: number;
  versionId: number | null;
  projectName?: string;
  projectCode?: string;
  versionLabel?: string;
  onClose: () => void;
}

export function FactAnalysisDialog({
  open,
  projectId,
  versionId,
  projectName = '',
  projectCode = '',
  versionLabel = '',
  onClose,
}: FactAnalysisDialogProps) {
  const title = formatProjectModalTitle('Анализ версии', {
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
      data-testid="fact-analysis-dialog"
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
          <FactAnalysisReportView projectId={projectId} versionId={versionId} />
        )}
      </DialogContent>
    </Dialog>
  );
}
