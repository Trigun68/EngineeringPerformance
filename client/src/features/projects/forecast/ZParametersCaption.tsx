import { Box, Typography } from '@mui/material';

const captionSx = {
  border: '2px solid #27AE60',
  backgroundColor: '#E8F8F0',
  borderRadius: 1,
  px: 1.5,
  py: 1,
  mb: 2,
} as const;

const textSx = {
  color: '#000',
  fontWeight: 700,
  fontSize: '15px',
  lineHeight: 1.4,
} as const;

export interface ZParametersCaptionProps {
  planVersionSelected?: boolean;
}

export function ZParametersCaption({ planVersionSelected = true }: ZParametersCaptionProps) {
  const suffix = planVersionSelected
    ? 'для выбранной версии плана'
    : '— выберите версию плана';

  return (
    <Box sx={captionSx} data-testid="z-parameters-caption">
      <Typography sx={textSx}>
        Z-параметры сценариев по группам физобъёмов IIC {suffix}
      </Typography>
    </Box>
  );
}
