import { Button, type ButtonProps } from '@mui/material';
import CircularProgress from '@mui/material/CircularProgress';

interface ToolbarActionButtonProps extends ButtonProps {
  loading?: boolean;
}

/** Общий вид панельных кнопок: outlined, small, без uppercase. */
export function ToolbarActionButton({
  sx,
  loading = false,
  startIcon,
  disabled,
  ...props
}: ToolbarActionButtonProps) {
  return (
    <Button
      {...props}
      size="small"
      variant="outlined"
      disabled={disabled || loading}
      startIcon={loading ? <CircularProgress size={16} color="inherit" /> : startIcon}
      sx={{ textTransform: 'none', ...sx }}
    />
  );
}
