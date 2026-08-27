import type { ReactNode } from 'react';
import { Accordion, AccordionDetails, AccordionSummary, Typography } from '@mui/material';
import ExpandMoreIcon from '@mui/icons-material/ExpandMore';

interface Props {
  children: ReactNode;
  testId: string;
  title?: string;
}

/** Вторичные блоки аналитики — свёрнуты по умолчанию, как «Дополнительно» в отчёте. */
export function AnalyticsExtraAccordion({ children, testId, title = 'Дополнительно' }: Props) {
  return (
    <Accordion disableGutters defaultExpanded={false} data-testid={testId} sx={{ boxShadow: 'none', '&:before': { display: 'none' } }}>
      <AccordionSummary expandIcon={<ExpandMoreIcon />} data-testid={`${testId}-summary`}>
        <Typography variant="subtitle1">{title}</Typography>
      </AccordionSummary>
      <AccordionDetails sx={{ px: 0 }}>{children}</AccordionDetails>
    </Accordion>
  );
}
