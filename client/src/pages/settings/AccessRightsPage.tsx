import { PermissionMatrixGrid } from '../../components/permissions/PermissionMatrixGrid';
import { Box } from '@mui/material';

export function AccessRightsPage() {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        flex: 1,
        minHeight: 0,
        height: '100%',
        overflow: 'hidden',
      }}
    >
      <PermissionMatrixGrid />
    </Box>
  );
}
