import type { ReactNode } from 'react';
import { Box, Paper, Typography } from '@mui/material';

export interface AnalyticsKpiCard {
  key: string;
  label: string;
  value: number | string;
  icon: ReactNode;
  color: string;
  active?: boolean;
  onClick?: () => void;
  testId?: string;
}

interface Props {
  cards: AnalyticsKpiCard[];
  testId?: string;
}

/** KPI-карточки-фильтры аналитики (мокапы FactNorm / NormAnalysis). */
export function AnalyticsKpiCards({ cards, testId }: Props) {
  return (
    <Box sx={{ display: 'flex', gap: 2, flexWrap: 'wrap' }} data-testid={testId}>
      {cards.map((card) => {
        const clickable = Boolean(card.onClick);
        return (
          <Paper
            key={card.key}
            variant="outlined"
            onClick={card.onClick}
            role={clickable ? 'button' : undefined}
            tabIndex={clickable ? 0 : undefined}
            onKeyDown={
              clickable
                ? (e) => {
                    if (e.key === 'Enter' || e.key === ' ') {
                      e.preventDefault();
                      card.onClick?.();
                    }
                  }
                : undefined
            }
            aria-pressed={clickable ? Boolean(card.active) : undefined}
            data-testid={card.testId}
            sx={{
              flex: '1 1 170px',
              minWidth: 160,
              p: 2,
              display: 'flex',
              alignItems: 'center',
              gap: 1.5,
              textAlign: 'left',
              cursor: clickable ? 'pointer' : 'default',
              borderColor: card.active ? 'primary.main' : undefined,
              borderWidth: card.active ? 2 : 1,
              backgroundColor: card.active ? 'action.selected' : 'background.paper',
              transition: 'background-color 0.15s, border-color 0.15s',
              '&:hover': clickable ? { backgroundColor: 'action.hover' } : undefined,
            }}
          >
            <Box sx={{ color: card.color, display: 'flex' }}>{card.icon}</Box>
            <Box>
              <Typography variant="h6" component="div" sx={{ color: card.color, lineHeight: 1.2 }}>
                {card.value}
              </Typography>
              <Typography variant="caption" color="text.secondary">
                {card.label}
              </Typography>
            </Box>
          </Paper>
        );
      })}
    </Box>
  );
}
