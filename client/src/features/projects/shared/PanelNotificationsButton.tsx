import { useMemo, useState } from 'react';
import {
  Badge,
  Box,
  Button,
  IconButton,
  Popover,
  Tooltip,
  Typography,
} from '@mui/material';
import WarningAmberOutlinedIcon from '@mui/icons-material/WarningAmberOutlined';

export interface PanelNotificationsButtonProps {
  /** Тексты предупреждений / несоответствий. */
  messages: string[];
  /** Подпись для aria / заголовка popover. */
  title?: string;
}

/**
 * Компактная кнопка оповещений рядом с подвкладками.
 * Tooltip — краткая суть; click — плавающее окно с полным текстом.
 */
export function PanelNotificationsButton({
  messages,
  title = 'Оповещения',
}: PanelNotificationsButtonProps) {
  const [anchor, setAnchor] = useState<HTMLElement | null>(null);
  const open = Boolean(anchor);

  const tooltipText = useMemo(() => {
    if (messages.length === 0) return '';
    if (messages.length === 1) return messages[0]!;
    const head = messages.slice(0, 3).join('\n');
    const rest = messages.length > 3 ? `\n… ещё ${messages.length - 3}` : '';
    return `${head}${rest}`;
  }, [messages]);

  if (messages.length === 0) {
    return null;
  }

  return (
    <>
      <Tooltip
        title={<Box sx={{ whiteSpace: 'pre-wrap', maxWidth: 360 }}>{tooltipText}</Box>}
        arrow
        enterDelay={300}
      >
        <Badge
          badgeContent={messages.length}
          color="warning"
          overlap="circular"
          sx={{ '& .MuiBadge-badge': { fontSize: 10, minWidth: 16, height: 16 } }}
        >
          <IconButton
            size="small"
            color="warning"
            aria-label={title}
            data-testid="panel-notifications-button"
            onClick={(e) => setAnchor(e.currentTarget)}
            sx={{
              border: '1px solid',
              borderColor: 'warning.light',
              bgcolor: 'rgba(237, 108, 2, 0.08)',
              borderRadius: 1,
              minWidth: 36,
              minHeight: 36,
            }}
          >
            <WarningAmberOutlinedIcon fontSize="small" />
          </IconButton>
        </Badge>
      </Tooltip>

      <Popover
        open={open}
        anchorEl={anchor}
        onClose={() => setAnchor(null)}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'left' }}
        transformOrigin={{ vertical: 'top', horizontal: 'left' }}
        slotProps={{
          paper: {
            sx: {
              p: 2,
              maxWidth: 480,
              maxHeight: 360,
              overflow: 'auto',
            },
          },
        }}
      >
        <Typography variant="subtitle2" sx={{ mb: 1, fontWeight: 600 }}>
          {title}
        </Typography>
        <Box component="ul" sx={{ m: 0, pl: 2.5 }}>
          {messages.map((msg) => (
            <Typography key={msg} component="li" variant="body2" sx={{ mb: 0.75 }}>
              {msg}
            </Typography>
          ))}
        </Box>
        <Box sx={{ mt: 1, display: 'flex', justifyContent: 'flex-end' }}>
          <Button size="small" onClick={() => setAnchor(null)}>
            Закрыть
          </Button>
        </Box>
      </Popover>
    </>
  );
}
