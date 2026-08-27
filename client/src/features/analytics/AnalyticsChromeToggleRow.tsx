import ExpandMoreIcon from '@mui/icons-material/ExpandMore';
import { Box, IconButton, Tooltip } from '@mui/material';
import { useAnalyticsChrome } from './analyticsChromeContext';

/** Кнопка сворачивания фильтров аналитики — перед контентом вкладки. */
export function AnalyticsChromeToggleRow() {
  const { chromeExpanded, setChromeExpanded } = useAnalyticsChrome();

  return (
    <Box
      data-testid="analytics-chrome-toggle-row"
      sx={{
        display: 'flex',
        justifyContent: 'center',
        alignItems: 'center',
        flexShrink: 0,
        py: 0.5,
      }}
    >
      <Tooltip title={chromeExpanded ? 'Свернуть фильтры' : 'Развернуть фильтры'}>
        <IconButton
          size="small"
          aria-label={chromeExpanded ? 'Свернуть фильтры' : 'Развернуть фильтры'}
          data-testid="analytics-chrome-toggle"
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
