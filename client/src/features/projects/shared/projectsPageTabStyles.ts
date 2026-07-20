import type { SxProps, Theme } from '@mui/material';

/** Стили подвкладок (Итого / По объектам / Детально) — паритет с ForecastPanel. */
export const projectsPageSubTabsSx: SxProps<Theme> = {
  minHeight: 40,
  flex: '1 1 auto',
  width: '100%',
  maxWidth: '100%',
  '& .MuiTabs-indicator': { display: 'none' },
  '& .MuiTab-root': {
    textTransform: 'none',
    border: '1px solid #AEB6BF',
    borderRadius: 1,
    mx: 0.5,
    minHeight: 36,
    minWidth: { xs: 72, md: 120 },
    bgcolor: '#F4F6F7',
    color: '#2C3E50',
    fontWeight: 500,
    px: { xs: 1, md: 2 },
  },
  '& .MuiTab-root.Mui-selected': {
    bgcolor: '#D6EAF8',
    borderColor: '#5D6D7E',
    color: '#2C3E50 !important',
    fontWeight: 600,
    boxShadow: 'inset 0 0 0 1px #5D6D7E',
  },
};

/** Панель импорта: одна строка с переносом при нехватке места. */
export const importToolbarStackSx: SxProps<Theme> = {
  flexWrap: 'wrap',
  justifyContent: 'flex-start',
  gap: 1,
  pb: 0.5,
  '& .MuiButton-root': {
    flexShrink: 0,
    whiteSpace: 'nowrap',
    minHeight: 40,
  },
};

/** Первый ряд тулбара панели: версия + параметры + импорт/удаление. */
export const panelPrimaryToolbarSx: SxProps<Theme> = {
  flexWrap: 'wrap',
  alignItems: 'center',
  gap: 1,
  mb: 1.5,
  '& .MuiButton-root': {
    flexShrink: 0,
    whiteSpace: 'nowrap',
    minHeight: 40,
  },
};
