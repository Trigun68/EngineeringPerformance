import type { ReactNode } from 'react';
import { Box, Typography } from '@mui/material';
import { MUTED_COLOR } from '../reportCalc';
import { wpColors } from '../../../../theme/wpColors';

/** Хром сводного отчёта вер 1 — цвета из темы страницы, без ограничения ширины 900px. */
export const PAGE_BG = wpColors.pageBg;
export const SECTION_BG = '#F4F6F8';
export const CARD_BG = wpColors.cardBg;
export const PRIMARY_TEXT = wpColors.heading;
export const BORDER = wpColors.cardBorder;

export function SectionLabel({ children }: { children: string }) {
  return (
    <Typography
      sx={{ fontSize: 11, fontWeight: 700, color: MUTED_COLOR, letterSpacing: 0.6, textTransform: 'uppercase' }}
    >
      {children}
    </Typography>
  );
}

export function ReportMockupCard({
  children,
  testId,
}: {
  children: ReactNode;
  testId?: string;
}) {
  return (
    <Box
      data-testid={testId}
      sx={{
        bgcolor: CARD_BG,
        border: `1px solid ${BORDER}`,
        borderRadius: '16px',
        p: { xs: 2, md: 3 },
      }}
    >
      {children}
    </Box>
  );
}
