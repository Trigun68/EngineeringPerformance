import {
  Box,
  Paper,
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';

interface InterpretationRow {
  range: string;
  label: string;
  color: string;
  status: string;
}

const Z_SCORE_ROWS: InterpretationRow[] = [
  { range: '< −1,0', label: 'Отличная производительность', color: '#4CAF50', status: 'В норме' },
  { range: 'от −1,0 до −0,5', label: 'Хорошая производительность', color: '#8BC34A', status: 'В норме' },
  { range: 'от −0,5 до +0,5', label: 'Удовлетворительная производительность', color: '#FFC107', status: 'Стабильный' },
  { range: 'от +0,5 до +1,0', label: 'Требует внимания', color: '#FF9800', status: 'Требует внимания' },
  { range: '> +1,0', label: 'Критически низкая производительность', color: '#F44336', status: 'Критический' },
];

const PD_ROWS = [
  { range: '< 7%', label: 'Очень высокая', color: '#4CAF50' },
  { range: 'от 7% до < 9%', label: 'Высокая', color: '#8BC34A' },
  { range: 'от 9% до < 11%', label: 'Стандартная', color: '#FFC107' },
  { range: '≥ 11%', label: 'Пониженная', color: '#FF9800' },
];

function ColorDot({ color }: { color: string }) {
  return (
    <Box
      component="span"
      sx={{
        display: 'inline-block',
        width: 10,
        height: 10,
        borderRadius: '50%',
        bgcolor: color,
        mr: 1,
        verticalAlign: 'middle',
      }}
    />
  );
}

function InterpretationTable({ rows, showStatus }: { rows: InterpretationRow[]; showStatus?: boolean }) {
  return (
    <TableContainer component={Paper} variant="outlined" sx={{ mb: 3 }}>
      <Table size="small">
        <TableHead>
          <TableRow>
            <TableCell>Диапазон</TableCell>
            <TableCell>Интерпретация</TableCell>
            {showStatus && <TableCell>Статус</TableCell>}
          </TableRow>
        </TableHead>
        <TableBody>
          {rows.map((row) => (
            <TableRow key={row.range}>
              <TableCell>{row.range}</TableCell>
              <TableCell>
                <ColorDot color={row.color} />
                {row.label}
              </TableCell>
              {showStatus && <TableCell>{row.status}</TableCell>}
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </TableContainer>
  );
}

export function HelpPage() {
  return (
    <Box maxWidth={900}>
      <Typography variant="h5" gutterBottom>
        Справка по показателям
      </Typography>
      <Typography variant="body2" color="text.secondary" paragraph>
        Справочная информация о шкалах и порогах, используемых в отчётах приложения.
        Пороги применяются единообразно в оценке, прогнозе и сравнении версий.
      </Typography>

      <Typography variant="h6" sx={{ mt: 2, mb: 1 }}>
        1. Z-оценка по направлениям IIC
      </Typography>
      <Typography variant="body2" paragraph>
        Z = (фактическая оценка − нормативная оценка) / нормативное стандартное отклонение.
        Фактическая оценка — натуральный логарифм фактической производительности.
      </Typography>
      <InterpretationTable rows={Z_SCORE_ROWS} showStatus />

      <Typography variant="h6" sx={{ mt: 2, mb: 1 }}>
        2. Точность сравнения (PD%)
      </Typography>
      <Typography variant="body2" paragraph>
        Средний показатель PD% по характеристикам проекта определяет качественную оценку точности анализа.
      </Typography>
      <TableContainer component={Paper} variant="outlined" sx={{ mb: 3 }}>
        <Table size="small">
          <TableHead>
            <TableRow>
              <TableCell>Диапазон PD%</TableCell>
              <TableCell>Оценка точности</TableCell>
            </TableRow>
          </TableHead>
          <TableBody>
            {PD_ROWS.map((row) => (
              <TableRow key={row.range}>
                <TableCell>{row.range}</TableCell>
                <TableCell>
                  <ColorDot color={row.color} />
                  {row.label}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </TableContainer>

      <Typography variant="h6" sx={{ mt: 2, mb: 1 }}>
        3. PEPM (взвешенное Z по проекту)
      </Typography>
      <Typography variant="body2" paragraph>
        PEPM = Σ(Zᵢ × Hᵢ) / ΣHᵢ, где Hᵢ — фактические трудозатраты по направлению.
        Интерпретация PEPM использует те же пороги Z-оценки; детальная таблица перцентилей
        доступна в отчёте «Оценка» и в desktop-справке (WPF reference).
      </Typography>

      <Typography variant="caption" color="text.secondary" display="block">
        Источники: PerformanceService, ProjectVersionAnalysisService, ComparisonAccuracy.
      </Typography>
    </Box>
  );
}
