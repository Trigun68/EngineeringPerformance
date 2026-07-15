import type { SxProps, Theme } from '@mui/material';

/** Стили подвкладок (Итого / По объектам / Детально) — паритет с ForecastPanel. */
export const projectsPageSubTabsSx: SxProps<Theme> = {
  minHeight: 40,
  flex: '1 1 auto',
  '& .MuiTabs-indicator': { display: 'none' },
  '& .MuiTab-root': {
    textTransform: 'none',
    border: '1px solid #AEB6BF',
    borderRadius: 1,
    mx: 0.5,
    minHeight: 36,
    bgcolor: '#F4F6F7',
    color: '#2C3E50',
    fontWeight: 500,
  },
  '& .MuiTab-root.Mui-selected': {
    bgcolor: '#D6EAF8',
    borderColor: '#5D6D7E',
    color: '#2C3E50 !important',
    fontWeight: 600,
    boxShadow: 'inset 0 0 0 1px #5D6D7E',
  },
};

/** Панель из 6 кнопок импорта/удаления в одну строку без переноса. */
export const importToolbarStackSx: SxProps<Theme> = {
  flexWrap: 'nowrap',
  justifyContent: 'flex-end',
  overflowX: 'auto',
  pb: 0.5,
  '& .MuiButton-root': { flexShrink: 0, whiteSpace: 'nowrap' },
};
