import { Box, Stack, Typography, Chip } from '@mui/material';
import ArrowUpwardIcon from '@mui/icons-material/ArrowUpward';
import ArrowDownwardIcon from '@mui/icons-material/ArrowDownward';

// ---------------------------------------------------------------------------
// Изолированный dev-макет редизайна вкладки «Отчёт» на статичных примерных
// данных. Не импортирует реальные API/типы проекта — только визуальная
// демонстрация нового плана: сводка план/факт, обновлённая шкала Z,
// динамика по версиям с двумя линиями (план + факт).
// ---------------------------------------------------------------------------

const FONT = "'Inter', 'Segoe UI', sans-serif";

// Новая палитра интерпретации Z-оценки — смягчённый красный, более выразительный
// жёлтый для «удовлетворительно».
const INTERPRETATION = {
  excellent: { label: 'Отлично', color: '#0F9D6B', bg: '#E5F7EF' },
  good: { label: 'Хорошо', color: '#2F9E6E', bg: '#EAF6F0' },
  satisfactory: { label: 'Удовлетворительно', color: '#C77A1F', bg: '#FBF0E1' },
  attention: { label: 'Требует внимания', color: '#D2691E', bg: '#FCEEE3' },
  critical: { label: 'Критично', color: '#C6483F', bg: '#F8E9E7' },
} as const;

const PAGE_BG = '#F4F6F8';
const CARD_BG = '#FFFFFF';
const PRIMARY = '#1B2A38';
const MUTED = '#7B8794';
const BORDER = '#E4E9ED';
const ACCENT_PLAN = '#8E6FD8'; // фиолетовый — план (менее акцентный)
const ACCENT_FACT = '#2F6FE0'; // синий — факт (акцентный, крупный маркер)

function SectionLabel({ children }: { children: string }) {
  return (
    <Typography
      sx={{ fontFamily: FONT, fontSize: 11, fontWeight: 700, color: MUTED, letterSpacing: 0.6, textTransform: 'uppercase' }}
    >
      {children}
    </Typography>
  );
}

// ---------------------------------------------------------------------------
// 1. Сводка План / Факт — замена SummaryCards
// ---------------------------------------------------------------------------

function DeltaChip({ deltaPct }: { deltaPct: number }) {
  const isOver = deltaPct > 0;
  const color = isOver ? '#C6483F' : '#0F9D6B';
  const bg = isOver ? '#F8E9E7' : '#E5F7EF';
  return (
    <Chip
      size="small"
      icon={isOver ? <ArrowUpwardIcon sx={{ fontSize: 14 }} /> : <ArrowDownwardIcon sx={{ fontSize: 14 }} />}
      label={`${isOver ? '+' : ''}${deltaPct.toFixed(1)}%`}
      sx={{
        fontFamily: FONT,
        fontWeight: 700,
        fontSize: 12,
        color,
        bgcolor: bg,
        '& .MuiChip-icon': { color },
        height: 24,
      }}
    />
  );
}

function PlanFactBlock({
  kind,
  sourceLabel,
  hours,
  z,
  interpretation,
}: {
  kind: 'plan' | 'fact';
  sourceLabel: string;
  hours: number;
  z: number;
  interpretation: keyof typeof INTERPRETATION;
}) {
  const meta = INTERPRETATION[interpretation];
  const accent = kind === 'plan' ? ACCENT_PLAN : ACCENT_FACT;
  return (
    <Box sx={{ flex: 1, bgcolor: CARD_BG, borderRadius: '12px', border: `1px solid ${BORDER}`, p: 2.5, position: 'relative', overflow: 'hidden' }}>
      <Box sx={{ position: 'absolute', top: 0, left: 0, width: 4, height: '100%', bgcolor: accent }} />
      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1.5 }}>
        <Stack direction="row" alignItems="center" spacing={1}>
          <Box sx={{ width: 8, height: 8, borderRadius: '50%', bgcolor: accent }} />
          <Typography sx={{ fontFamily: FONT, fontSize: 13, fontWeight: 700, color: PRIMARY }}>
            {kind === 'plan' ? 'План' : 'Факт'}
          </Typography>
        </Stack>
        <Typography sx={{ fontFamily: FONT, fontSize: 11, color: MUTED }}>{sourceLabel}</Typography>
      </Stack>
      <Stack direction="row" spacing={4}>
        <Box>
          <Typography sx={{ fontFamily: FONT, fontSize: 11, color: MUTED, mb: 0.3 }}>Трудоёмкость</Typography>
          <Typography sx={{ fontFamily: FONT, fontSize: 26, fontWeight: 800, color: PRIMARY, lineHeight: 1.1 }}>
            {hours.toLocaleString('ru-RU')} ч
          </Typography>
        </Box>
        <Box>
          <Typography sx={{ fontFamily: FONT, fontSize: 11, color: MUTED, mb: 0.3 }}>Z-оценка</Typography>
          <Typography sx={{ fontFamily: FONT, fontSize: 26, fontWeight: 800, color: meta.color, lineHeight: 1.1 }}>
            {z > 0 ? '+' : ''}
            {z.toFixed(2)}
          </Typography>
        </Box>
      </Stack>
      <Chip
        size="small"
        label={meta.label}
        sx={{ mt: 1.5, fontFamily: FONT, fontWeight: 700, fontSize: 11.5, color: meta.color, bgcolor: meta.bg, height: 24 }}
      />
    </Box>
  );
}

function PlanFactSummaryMockup() {
  return (
    <Box sx={{ bgcolor: PAGE_BG, borderRadius: '16px', p: 3 }}>
      <SectionLabel>Сводка по проекту</SectionLabel>
      <Stack direction="row" spacing={2} sx={{ mt: 1.5, mb: 2 }}>
        <PlanFactBlock
          kind="plan"
          sourceLabel="из версии Факт v5 (основная)"
          hours={18240}
          z={-0.18}
          interpretation="good"
        />
        <PlanFactBlock
          kind="fact"
          sourceLabel="Факт v5 (основная)"
          hours={16980}
          z={-0.34}
          interpretation="good"
        />
      </Stack>
      <Stack direction="row" spacing={2} alignItems="center" sx={{ px: 0.5 }}>
        <Typography sx={{ fontFamily: FONT, fontSize: 12, color: MUTED }}>Отклонение факта от плана:</Typography>
        <DeltaChip deltaPct={-6.9} />
        <Box sx={{ flex: 1 }} />
        <Typography sx={{ fontFamily: FONT, fontSize: 12, color: MUTED }}>
          Перцентиль: <b style={{ color: PRIMARY }}>38-й</b> · Метод агрегации: <b style={{ color: PRIMARY }}>Взвешенный по IIC</b>
        </Typography>
      </Stack>
    </Box>
  );
}

// ---------------------------------------------------------------------------
// 2. Обновлённая шкала Z-оценки
// ---------------------------------------------------------------------------

const ZONES = [
  { key: 'critical', from: -3, to: -1.5, color: '#DDA9A4', label: 'Критично' },
  { key: 'attention', from: -1.5, to: -0.5, color: '#F0C48A', label: 'Внимание' },
  { key: 'satisfactory', from: -0.5, to: 0.5, color: '#F4CF62', label: 'Удовл.' },
  { key: 'good', from: 0.5, to: 1.5, color: '#A8DDBE', label: 'Хорошо' },
  { key: 'excellent', from: 1.5, to: 3, color: '#6FCB9F', label: 'Отлично' },
];
const Z_MIN = -3;
const Z_MAX = 3;
const pctOf = (z: number) => ((z - Z_MIN) / (Z_MAX - Z_MIN)) * 100;

// Две раздельные дорожки-строчки (План / Факт) над шкалой — исключает коллизию
// подписей даже когда оба значения Z близки друг к другу.
function MarkerTrack({
  z,
  rowLabel,
  color,
  emphasized,
}: {
  z: number;
  rowLabel: string;
  color: string;
  emphasized?: boolean;
}) {
  const left = pctOf(z);
  const dim = emphasized ? 20 : 13;
  return (
    <Stack direction="row" alignItems="center" spacing={1.2} sx={{ height: emphasized ? 30 : 22 }}>
      <Box sx={{ width: 46, textAlign: 'right', flexShrink: 0 }}>
        <Typography
          sx={{
            fontFamily: FONT,
            fontSize: emphasized ? 11.5 : 10.5,
            fontWeight: emphasized ? 800 : 700,
            color: emphasized ? color : MUTED,
          }}
        >
          {rowLabel}
        </Typography>
      </Box>
      <Box sx={{ flex: 1, position: 'relative', height: '100%' }}>
        <Box
          sx={{
            position: 'absolute',
            left: `${left}%`,
            top: '50%',
            transform: 'translate(-50%, -50%)',
            width: dim,
            height: dim,
            borderRadius: '50%',
            bgcolor: color,
            border: emphasized ? '3px solid #FFFFFF' : '2px solid #FFFFFF',
            boxShadow: emphasized
              ? `0 0 0 2.5px ${color}, 0 3px 8px rgba(0,0,0,0.28)`
              : `0 0 0 1.5px ${color}`,
          }}
        />
        <Box
          sx={{
            position: 'absolute',
            left: `${left}%`,
            top: '50%',
            transform: emphasized ? 'translate(14px, -50%)' : 'translate(11px, -50%)',
            bgcolor: emphasized ? color : 'transparent',
            color: emphasized ? '#FFFFFF' : MUTED,
            px: emphasized ? 0.9 : 0,
            py: emphasized ? 0.15 : 0,
            borderRadius: '6px',
            fontFamily: FONT,
            fontSize: emphasized ? 11 : 10,
            fontWeight: emphasized ? 800 : 600,
            whiteSpace: 'nowrap',
          }}
        >
          {z > 0 ? '+' : ''}
          {z.toFixed(2)}
        </Box>
      </Box>
    </Stack>
  );
}

function ZScaleMockup() {
  const planZ = -0.18;
  const factZ = -0.34;
  return (
    <Box sx={{ bgcolor: CARD_BG, borderRadius: '16px', border: `1px solid ${BORDER}`, p: 3 }}>
      <SectionLabel>Шкала интерпретации Z-оценки</SectionLabel>
      <Box sx={{ mt: 2, mb: 0.5 }}>
        <MarkerTrack z={planZ} rowLabel="План" color={ACCENT_PLAN} />
        <MarkerTrack z={factZ} rowLabel="Факт" color={ACCENT_FACT} emphasized />
      </Box>
      <Box sx={{ pl: '58px', pr: 0 }}>
        <Box sx={{ height: 1, bgcolor: BORDER, mb: 1 }} />
        <Box sx={{ display: 'flex', height: 14, borderRadius: '7px', overflow: 'hidden' }}>
          {ZONES.map((z) => (
            <Box key={z.key} sx={{ flex: z.to - z.from, bgcolor: z.color }} />
          ))}
        </Box>
        <Stack direction="row" sx={{ mt: 1 }}>
          {ZONES.map((z) => (
            <Box key={z.key} sx={{ flex: z.to - z.from, textAlign: 'center' }}>
              <Typography sx={{ fontFamily: FONT, fontSize: 10.5, color: MUTED, fontWeight: 600 }}>{z.label}</Typography>
            </Box>
          ))}
        </Stack>
        <Stack direction="row" justifyContent="space-between" sx={{ mt: 0.5 }}>
          <Typography sx={{ fontFamily: FONT, fontSize: 10, color: MUTED }}>−3</Typography>
          <Typography sx={{ fontFamily: FONT, fontSize: 10, color: MUTED }}>0</Typography>
          <Typography sx={{ fontFamily: FONT, fontSize: 10, color: MUTED }}>+3</Typography>
        </Stack>
      </Box>
    </Box>
  );
}

// ---------------------------------------------------------------------------
// 3. Динамика Z-оценки по версиям — линии План + Факт, хронологический порядок
// ---------------------------------------------------------------------------

type TrendPoint = { date: string; label: string; plan?: number; fact?: number };

const TREND_DATA: TrendPoint[] = [
  { date: '2025-02-10', label: '10 фев', plan: 0.42 },
  { date: '2025-03-04', label: '4 мар', plan: 0.21 },
  { date: '2025-04-18', label: '18 апр', plan: -0.05 },
  { date: '2025-06-02', label: '2 июн', plan: -0.18 },
  { date: '2025-07-21', label: '21 июл', plan: -0.18, fact: -0.34 },
];

function TrendChartMockup() {
  const W = 640;
  const H = 220;
  const padL = 36;
  const padR = 16;
  const padT = 16;
  const padB = 28;
  const innerW = W - padL - padR;
  const innerH = H - padT - padB;

  const yFor = (z: number) => padT + innerH * (1 - (z + 1) / 2); // диапазон -1..+1 для читаемости
  const xFor = (i: number) => padL + (i / (TREND_DATA.length - 1)) * innerW;

  const planPts = TREND_DATA.map((p, i) => (p.plan !== undefined ? [xFor(i), yFor(p.plan)] : null)).filter(
    (p): p is number[] => p !== null,
  );
  const planPath = planPts.map((p, i) => `${i === 0 ? 'M' : 'L'} ${p[0]} ${p[1]}`).join(' ');

  const factIdx = TREND_DATA.findIndex((p) => p.fact !== undefined);
  const factPoint = factIdx >= 0 ? [xFor(factIdx), yFor(TREND_DATA[factIdx].fact!)] : null;

  return (
    <Box sx={{ bgcolor: CARD_BG, borderRadius: '16px', border: `1px solid ${BORDER}`, p: 3 }}>
      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1 }}>
        <SectionLabel>Динамика Z-оценки по версиям</SectionLabel>
        <Stack direction="row" spacing={2.5}>
          <Stack direction="row" alignItems="center" spacing={0.7}>
            <Box sx={{ width: 20, height: 2.5, bgcolor: ACCENT_PLAN, borderRadius: 2 }} />
            <Typography sx={{ fontFamily: FONT, fontSize: 11.5, color: MUTED, fontWeight: 600 }}>План (по версиям)</Typography>
          </Stack>
          <Stack direction="row" alignItems="center" spacing={0.7}>
            <Box sx={{ width: 10, height: 10, borderRadius: '50%', bgcolor: ACCENT_FACT, border: '2px solid #FFFFFF', boxShadow: `0 0 0 1.5px ${ACCENT_FACT}` }} />
            <Typography sx={{ fontFamily: FONT, fontSize: 11.5, color: MUTED, fontWeight: 600 }}>Факт (основная версия)</Typography>
          </Stack>
        </Stack>
      </Stack>

      <svg width="100%" viewBox={`0 0 ${W} ${H}`} style={{ display: 'block' }}>
        {/* Нулевая линия */}
        <line x1={padL} y1={yFor(0)} x2={W - padR} y2={yFor(0)} stroke={BORDER} strokeWidth={1} strokeDasharray="3 3" />
        <text x={padL - 8} y={yFor(0) + 3} textAnchor="end" fontSize={9.5} fill={MUTED} fontFamily={FONT}>
          0
        </text>
        <text x={padL - 8} y={yFor(1) + 3} textAnchor="end" fontSize={9.5} fill={MUTED} fontFamily={FONT}>
          +1
        </text>
        <text x={padL - 8} y={yFor(-1) + 3} textAnchor="end" fontSize={9.5} fill={MUTED} fontFamily={FONT}>
          −1
        </text>

        {/* Линия плана */}
        <path d={planPath} fill="none" stroke={ACCENT_PLAN} strokeWidth={2.25} strokeLinecap="round" strokeLinejoin="round" />
        {planPts.map((p, i) => (
          <circle key={i} cx={p[0]} cy={p[1]} r={3.5} fill="#FFFFFF" stroke={ACCENT_PLAN} strokeWidth={2} />
        ))}

        {/* Крупный акцентный маркер факта */}
        {factPoint && (
          <>
            <circle cx={factPoint[0]} cy={factPoint[1]} r={9} fill={ACCENT_FACT} opacity={0.16} />
            <circle cx={factPoint[0]} cy={factPoint[1]} r={6} fill={ACCENT_FACT} stroke="#FFFFFF" strokeWidth={2.5} />
          </>
        )}

        {/* Ось X: даты версий в хронологическом порядке */}
        {TREND_DATA.map((p, i) => (
          <text
            key={p.date}
            x={xFor(i)}
            y={H - 8}
            textAnchor="middle"
            fontSize={10}
            fill={MUTED}
            fontFamily={FONT}
            fontWeight={p.fact !== undefined ? 700 : 400}
          >
            {p.label}
          </text>
        ))}
      </svg>
      <Typography sx={{ fontFamily: FONT, fontSize: 11, color: MUTED, mt: 0.5 }}>
        Версии отсортированы по дате создания (хронологически) — план показывает эволюцию по всем версиям плана,
        факт — единственная точка по основной версии факта.
      </Typography>
    </Box>
  );
}

// ---------------------------------------------------------------------------
// Полная страница макета
// ---------------------------------------------------------------------------

function Swatch({ label, hex }: { label: string; hex: string }) {
  return (
    <Stack direction="row" alignItems="center" spacing={1}>
      <Box sx={{ width: 22, height: 22, borderRadius: '6px', bgcolor: hex, border: '1px solid rgba(0,0,0,0.08)' }} />
      <Box>
        <Typography sx={{ fontFamily: FONT, fontSize: 11.5, fontWeight: 700, color: PRIMARY }}>{label}</Typography>
        <Typography sx={{ fontFamily: FONT, fontSize: 10, color: MUTED }}>{hex}</Typography>
      </Box>
    </Stack>
  );
}

export default function ReportRedesignMockup() {
  return (
    <Box sx={{ position: 'fixed', inset: 0, overflowY: 'auto', overflowX: 'hidden', bgcolor: '#EEF1F4', py: 5 }}>
      <Box sx={{ maxWidth: 900, mx: 'auto', px: 3 }}>
        <Typography sx={{ fontFamily: FONT, fontSize: 24, fontWeight: 800, color: PRIMARY }}>
          Макет — вкладка «Отчёт» (редизайн)
        </Typography>
        <Typography sx={{ fontFamily: FONT, fontSize: 13.5, color: MUTED, mt: 0.5, mb: 3, maxWidth: 640 }}>
          Статичный интерактивный макет на примерных данных: новая сводка План/Факт вместо текущих карточек, шкала
          Z-оценки с более мягкой красной зоной и ярче выделенным «удовлетворительно», крупный акцентный маркер
          факта, и график динамики с двумя линиями (план/факт) в строгом хронологическом порядке дат.
        </Typography>

        <Box sx={{ bgcolor: CARD_BG, borderRadius: '12px', p: 2, mb: 3, border: `1px solid ${BORDER}` }}>
          <SectionLabel>Палитра интерпретации (обновлено)</SectionLabel>
          <Stack direction="row" spacing={3} flexWrap="wrap" useFlexGap sx={{ mt: 1.5 }}>
            <Swatch label="Критично (смягчён)" hex={ZONES[0].color} />
            <Swatch label="Внимание" hex={ZONES[1].color} />
            <Swatch label="Удовл. (ярче)" hex={ZONES[2].color} />
            <Swatch label="Хорошо" hex={ZONES[3].color} />
            <Swatch label="Отлично" hex={ZONES[4].color} />
            <Swatch label="Акцент: План" hex={ACCENT_PLAN} />
            <Swatch label="Акцент: Факт" hex={ACCENT_FACT} />
          </Stack>
        </Box>

        <Stack spacing={3}>
          <PlanFactSummaryMockup />
          <ZScaleMockup />
          <TrendChartMockup />
        </Stack>
      </Box>
    </Box>
  );
}
