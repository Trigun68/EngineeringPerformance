import { Box, Paper, Stack, Typography } from '@mui/material';
import { useWorkspaceChrome } from '../../features/projects/shared/workspaceChromeContext';

export function WorkspaceCompactBar({
  projectName,
  tabLabel,
}: {
  projectName: string | null;
  tabLabel: string;
}) {
  const { versionSummary } = useWorkspaceChrome();
  return (
    <Paper
      elevation={0}
      data-testid="workspace-compact-bar"
      sx={{
        mb: 1,
        px: { xs: 1.5, md: 2 },
        py: 1,
        border: '1px solid #D5DDE5',
        backgroundColor: '#F8F9FA',
      }}
    >
      <Stack direction="row" spacing={1.5} alignItems="center" flexWrap="wrap" useFlexGap>
        {projectName && (
          <Typography
            data-testid="workspace-compact-project"
            sx={{
              color: '#2C3E50',
              fontWeight: 600,
              fontSize: { xs: 14, md: 16 },
              maxWidth: { xs: '100%', md: 360 },
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {projectName}
          </Typography>
        )}
        <Typography
          data-testid="workspace-compact-tab"
          sx={{ color: '#5D6D7E', fontWeight: 500, fontSize: { xs: 13, md: 14 } }}
        >
          {tabLabel}
        </Typography>
        {versionSummary && (
          <Box
            data-testid="workspace-compact-version"
            sx={{
              backgroundColor: '#EBF5FB',
              border: '1px solid #AED6F1',
              px: 1.5,
              py: 0.5,
              borderRadius: 1,
              maxWidth: { xs: '100%', sm: 420 },
              minWidth: 0,
            }}
          >
            <Typography variant="body2" sx={{ color: '#1A5276' }} noWrap>
              {versionSummary}
            </Typography>
          </Box>
        )}
      </Stack>
    </Paper>
  );
}
