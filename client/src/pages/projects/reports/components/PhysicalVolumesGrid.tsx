import { Box, Grid, Stack, Typography } from '@mui/material';
import type { PhysicalVolumeTotalByUnit } from '../../../../api/analyticsApi';
import { fmt } from '../reportCalc';

interface PhysicalVolumesGridProps {
  items: PhysicalVolumeTotalByUnit[];
}

export function PhysicalVolumesGrid({ items }: PhysicalVolumesGridProps) {
  return (
    <Stack spacing={2}>
      <Typography variant="body2" color="text.secondary">
        Итоги физических объёмов суммируются отдельно по каждой единице измерения — смешивание единиц не
        допускается.
      </Typography>

      {items.length === 0 ? (
        <Typography variant="body2" color="text.secondary">
          Физические объёмы по проекту отсутствуют.
        </Typography>
      ) : (
        <Grid container spacing={2}>
          {items.map((item) => (
            <Grid item xs={6} sm={4} lg={2.4} key={item.unitName}>
              <Box sx={{ p: 2, borderRadius: 1, border: 1, borderColor: 'divider' }}>
                <Typography variant="caption" color="text.secondary" display="block">
                  {item.unitName}
                </Typography>
                <Typography variant="subtitle1" sx={{ fontFamily: 'monospace', fontWeight: 600 }}>
                  {fmt(item.totalVolume, 0)}
                </Typography>
              </Box>
            </Grid>
          ))}
        </Grid>
      )}
    </Stack>
  );
}
