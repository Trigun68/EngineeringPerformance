import { Box, CircularProgress } from '@mui/material';

/** Индикатор загрузки lazy-страницы внутри карточки контента — оболочка не снимается. */
export function RouteContentFallback() {
  return (
    <Box
      sx={{ display: 'flex', justifyContent: 'center', alignItems: 'center', py: 4, flex: 1 }}
      data-testid="route-content-fallback"
    >
      <CircularProgress size={28} />
    </Box>
  );
}
