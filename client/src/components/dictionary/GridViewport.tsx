import type { ReactNode } from 'react';
import { Box } from '@mui/material';

interface GridViewportProps {
  children: ReactNode;
  loading?: boolean;
  className?: string;
}

/** Контейнер AG Grid с фиксированной высотой и прокруткой внутри таблицы. */
export function GridViewport({ children, loading = false, className = 'ag-theme-alpine' }: GridViewportProps) {
  return (
    <Box
      className={className}
      sx={{
        flex: 1,
        minHeight: 0,
        width: '100%',
        display: 'flex',
        flexDirection: 'column',
        overflow: 'hidden',
        opacity: loading ? 0.6 : 1,
      }}
    >
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          width: '100%',
          height: '100%',
          overflow: 'hidden',
          '& .ag-root-wrapper': { height: '100%' },
          '& .ag-root-wrapper-body': { height: '100%' },
          '& .ag-root': { height: '100%' },
          '& .ag-body-viewport': { overflow: 'auto' },
        }}
      >
        {children}
      </Box>
    </Box>
  );
}
