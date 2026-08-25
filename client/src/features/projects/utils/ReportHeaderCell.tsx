import { Box, type SxProps, type Theme } from '@mui/material';
import { Fragment } from 'react';

export interface ReportHeaderCellProps {
  children: string;
  align?: 'left' | 'right';
  sx?: SxProps<Theme>;
}

/** Ячейка шапки: перенос по точкам («План.Труд.Оптим.»), чтобы подписи не наезжали. */
export function ReportHeaderCell({ children, align = 'left', sx }: ReportHeaderCellProps) {
  const parts = children.split(/(?<=\.)/);
  return (
    <Box
      sx={{
        minWidth: 0,
        whiteSpace: 'normal',
        overflowWrap: 'break-word',
        lineHeight: 1.25,
        textAlign: align,
        ...sx,
      }}
    >
      {parts.map((part, index) => (
        <Fragment key={index}>
          {index > 0 ? <wbr /> : null}
          {part}
        </Fragment>
      ))}
    </Box>
  );
}
