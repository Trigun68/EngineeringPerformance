import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
} from '@mui/material';
import type { NormListItem } from '../../../api/normRegistryApi';

interface Props {
  open: boolean;
  norm: NormListItem | null;
  busy?: boolean;
  onClose: () => void;
  onConfirm: () => void;
}

export function ConfirmArchiveDialog({ open, norm, busy, onClose, onConfirm }: Props) {
  return (
    <Dialog open={open} onClose={busy ? undefined : onClose} data-testid="norm-archive-dialog">
      <DialogTitle>Закрыть период нормы?</DialogTitle>
      <DialogContent>
        <DialogContentText>
          Норма #{norm?.id} ({norm?.groupName}) будет закрыта: EffectiveTo = сегодня (UTC).
          Статус станет «Историческая». Действие необратимо без ручного редактирования.
        </DialogContentText>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose} disabled={busy}>
          Отмена
        </Button>
        <Button
          color="warning"
          variant="contained"
          onClick={onConfirm}
          disabled={busy}
          data-testid="norm-archive-confirm"
        >
          Закрыть период
        </Button>
      </DialogActions>
    </Dialog>
  );
}
