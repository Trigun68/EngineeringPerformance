import { Box, Grid, Paper, Typography } from '@mui/material';
import { usePermission } from '../../hooks/usePermission';
import { wpColors } from '../../theme/wpColors';

const SIGMA_IMAGE = '/assets/standard-deviation-interpretation.png';

export function DashboardPage() {
  const { canRead } = usePermission('Dashboard');

  if (!canRead) {
    return null;
  }

  return (
    <Box>
      <Typography sx={{ fontSize: 15, fontWeight: 600, color: '#475569', mb: 1.25 }}>
        Интерпретация стандартного отклонения (σ)
      </Typography>

      <Grid container spacing={3}>
        <Grid item xs={12} lg="auto">
          <Paper
            variant="outlined"
            sx={{
              p: 1.5,
              maxWidth: 567,
              borderColor: '#E2E8F0',
              borderRadius: 2.5,
            }}
          >
            <Box
              component="img"
              src={SIGMA_IMAGE}
              alt="Интерпретация стандартного отклонения"
              sx={{ display: 'block', maxWidth: 541, width: '100%', height: 'auto' }}
            />
          </Paper>
          <Typography sx={{ fontSize: 12, color: '#94A3B8', mt: 1, maxWidth: 567 }}>
            Рисунок: распределение значения в диапазоне от −1σ до +1σ; цветовая шкала соответствует
            положению относительно среднего (0).
          </Typography>
        </Grid>

        <Grid item xs={12} lg>
          <Typography sx={{ fontSize: 18, fontWeight: 600, color: wpColors.heading, mb: 1.5 }}>
            Методология оценки проектов
          </Typography>
          <Paper
            variant="outlined"
            sx={{
              p: 2.5,
              bgcolor: '#F8FAFC',
              borderColor: '#E2E8F0',
              borderRadius: 2.5,
            }}
          >
            <Typography sx={{ fontSize: 14, lineHeight: '22px', color: '#334155', mb: 1.75 }}>
              <Box component="span" sx={{ fontWeight: 600 }}>
                Базовая идея.{' '}
              </Box>
              Фактическая производительность по направлению ИИС сравнивается с нормативом, зафиксированным
              в снимке версии анализа. Для сопоставимости используется логарифм фактической
              производительности (часы на единицу объёма в единицах группы ИИС) — «оценка факта»; норма
              задаётся оценкой и стандартным отклонением нормы по тому же масштабу.
            </Typography>
            <Typography sx={{ fontSize: 14, lineHeight: '22px', color: '#334155', mb: 1.75 }}>
              <Box component="span" sx={{ fontWeight: 600 }}>
                Z-score.{' '}
              </Box>
              Величина Z = (оценка факта − оценка нормы) / σ нормы показывает, на сколько стандартных
              отклонений факт отклоняется от нормы. В приложении меньшее Z (в сторону отрицательных
              значений) соответствует меньшим часам на единицу объёма, то есть более высокой
              производительности относительно нормы.
            </Typography>
            <Typography sx={{ fontSize: 14, lineHeight: '22px', color: '#334155' }}>
              <Box component="span" sx={{ fontWeight: 600 }}>
                Интерпретация в системе.{' '}
              </Box>
              Категории на вкладках анализа и в отчётах выводятся по порогам Z: Z &lt; −1 — отличная
              производительность; −1 ≤ Z &lt; −0,5 — хорошая; −0,5 ≤ Z ≤ 0,5 — удовлетворительная; 0,5
              &lt; Z ≤ 1 — требует внимания; Z &gt; 1 — критически низкая производительность. Рисунок
              слева иллюстрирует положение значения в диапазоне ±1σ относительно нуля (среднее нормы).
            </Typography>
          </Paper>
        </Grid>
      </Grid>
    </Box>
  );
}
