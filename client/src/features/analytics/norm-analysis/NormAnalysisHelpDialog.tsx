import CloseIcon from '@mui/icons-material/Close';
import {
  Dialog,
  DialogContent,
  DialogTitle,
  IconButton,
} from '@mui/material';
import helpMarkdown from './normAnalysisHelp.md?raw';
import { HelpMarkdown } from './HelpMarkdown';

interface Props {
  open: boolean;
  onClose: () => void;
}

export function NormAnalysisHelpDialog({ open, onClose }: Props) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="md"
      fullWidth
      scroll="paper"
      aria-labelledby="norm-analysis-help-title"
      PaperProps={{ sx: { height: '90vh', maxHeight: '90vh' } }}
    >
      <DialogTitle id="norm-analysis-help-title" sx={{ pr: 6, position: 'relative' }}>
        Справка: анализ норм
        <IconButton
          aria-label="Закрыть"
          onClick={onClose}
          size="small"
          sx={{ position: 'absolute', right: 8, top: 8 }}
          data-testid="norm-analysis-help-close"
        >
          <CloseIcon fontSize="small" />
        </IconButton>
      </DialogTitle>
      <DialogContent dividers data-testid="norm-analysis-help-body">
        <HelpMarkdown source={helpMarkdown} omitH1 />
      </DialogContent>
    </Dialog>
  );
}
