import { Dialog, DialogContent, DialogTitle, IconButton, Typography } from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import { FactAnalysisReportView } from './FactAnalysisReportView';

export interface FactAnalysisDialogProps {
  open: boolean;
  projectId: number;
  versionId: number | null;
  onClose: () => void;
}

export function FactAnalysisDialog({
  open,
  projectId,
  versionId,
  onClose,
}: FactAnalysisDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="xl"
      fullWidth
      data-testid="fact-analysis-dialog"
      PaperProps={{ sx: { height: '90vh' } }}
    >
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pr: 1 }}>
        <Typography variant="h6" component="span">
          Анализ версии
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
