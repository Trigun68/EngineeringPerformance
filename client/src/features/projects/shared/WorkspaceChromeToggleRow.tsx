import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { Box, IconButton, Tooltip } from '@mui/material';
import { useWorkspaceChrome } from './workspaceChromeContext';

/** Строка с кнопкой сворачивания шапки — сразу перед таблицей. */
export function WorkspaceChromeToggleRow() {
  const { chromeExpanded, setChromeExpanded } = useWorkspaceChrome();

  return (
    <Box
      data-testid="workspace-chrome-toggle-row"
      sx={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        flexShrink: 0,
        py: 0.5,
        mb: 0.5,
      }}
    >
      <Tooltip title={chromeExpanded ? 'Свернуть шапку' : 'Развернуть шапку'}>
        <IconButton
          size="small"
          aria-label={chromeExpanded ? 'Свернуть шапку' : 'Развернуть шапку'}
          data-testid="project-header-toggle"
          onClick={() => setChromeExpanded(!chromeExpanded)}
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
              transform: chromeExpanded ? 'rotate(180deg)' : 'rotate(0deg)',
              transition: 'transform 0.2s ease',
              color: '#5D6D7E',
            }}
          />
        </IconButton>
      </Tooltip>
    </Box>
  );
}
