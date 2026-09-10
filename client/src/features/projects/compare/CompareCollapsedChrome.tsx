import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { Box, IconButton, Tooltip } from '@mui/material';

export interface CompareCollapsedChromeProps {
  onExpand: () => void;
}

/** Компактная строка сравнения при свёрнутой шапке (не WorkspaceChromeToggleRow). */
export function CompareCollapsedChrome({ onExpand }: CompareCollapsedChromeProps) {
  return (
    <Box
      data-testid="workspace-chrome-toggle-row"
      sx={{
        display: 'flex',
        justifyContent: 'flex-start',
        alignItems: 'center',
        flexShrink: 0,
        py: 0.5,
        mb: 0.5,
      }}
    >
      <Tooltip title="Развернуть шапку">
        <IconButton
          size="small"
          aria-label="Развернуть шапку"
          data-testid="project-header-toggle"
          onClick={onExpand}
          sx={{
            border: '1px solid #D5D8DC',
            bgcolor: '#F8F9FA',
            borderRadius: 1,
            py: 0.25,
            px: 1.5,
            '&:hover': { bgcolor: '#EEF2F5' },
          }}
        >
          <ExpandMoreIcon
            sx={{
              transform: 'rotate(0deg)',
              transition: 'transform 0.2s ease',
              color: '#5D6D7E',
            }}
          />
        </IconButton>
      </Tooltip>
    </Box>
  );
}
