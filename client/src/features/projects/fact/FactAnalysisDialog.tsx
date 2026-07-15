import {
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
} from '@mui/material';
import CloseIcon from '@mui/icons-material/Close';
import { AssessmentTab } from '../../../pages/projects/AssessmentTab';

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
    <Dialog open={open} onClose={onClose} maxWidth="lg" fullWidth data-testid="fact-analysis-dialog">
      <DialogTitle sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', pr: 1 }}>
        Анализ версии
        <IconButton aria-label="Закрыть" onClick={onClose} size="small">
          <CloseIcon />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers sx={{ minHeight: 420 }}>
        <AssessmentTab projectId={projectId} versionId={versionId} />
      </DialogContent>
    </Dialog>
  );
}
