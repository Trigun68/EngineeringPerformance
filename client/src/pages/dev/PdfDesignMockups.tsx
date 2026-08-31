import { Box, Stack, Typography } from '@mui/material';
import type { ReactNode } from 'react';

// Точное зеркало токенов backend/EngineeringPerformance.Application/Export/ReportVisualTheme.cs
// Дизайн-система "Executive Technical" — единый стиль для всех 10 PDF-отчётов.
const theme = {
  primary: '#1B2A38',
  primaryDark: '#10181F',
  accentA: '#2E86C1',
  accentDark: '#1B5A85',
  accentSoft: '#D6EAF8',
  accentB: '#8E44AD',
  positive: '#1E8449',
  negative: '#C0392B',
  neutral: '#B9770E',
  muted: '#7B8794',
  cardBg: '#EBF5FB',
  pageBg: '#F7F9FB',
  ruleColor: '#E3E7EB',
  sectionBg: '#F1F4F7',
  coverGradientTop: '#22384A',
  coverGradientBottom: '#10181F',
  watermarkGray: '#2A3F52',
  coverMetaCardBg: '#1A2B3A',
  coverMutedText: '#8A97A5',
  coverBodyText: '#C7D0D9',
  coverFooterText: '#7E8B99',
  riskBg: '#FBEAEA',
  attentionBg: '#FCF2E3',
  strengthBg: '#EAF5EF',
  dataBg: '#F1F3F5',
  white: '#FFFFFF',
} as const;

const FONT = "'DejaVu Sans', 'Arial', sans-serif";

// Масштаб: 1 мм ≈ 3px → A4 портрет 210×297мм = 630×891px, ландшафт 297×210мм = 891×630px.
const MM = 3;
const PAGE_W = 210 * MM;
const PAGE_H = 297 * MM;
const PAGE_W_L = 297 * MM;
const PAGE_H_L = 210 * MM;

function PageFrame({
  width,
  height,
  children,
  label,
}: {
  width: number;
  height: number;
  children: ReactNode;
  label: string;
}) {
  return (
    <Stack spacing={1.5} alignItems="center">
      <Box
        sx={{
          width,
          height,
          bgcolor: theme.white,
          boxShadow: '0 20px 50px -12px rgba(16,24,31,0.35), 0 0 0 1px rgba(16,24,31,0.06)',
          overflow: 'hidden',
          position: 'relative',
          fontFamily: FONT,
        }}
      >
        {children}
      </Box>
      <Typography
        sx={{ fontFamily: FONT, fontSize: 12, letterSpacing: 1, color: '#8A97A5', textTransform: 'uppercase' }}
      >
        {label}
      </Typography>
    </Stack>
  );
}

// Точное зеркало ReportVisualTheme.ReportKindLabel / ReportKindColor.
const KIND_META = {
  forecast: { label: 'ПРОГНОЗ', color: theme.accentA },
  fact: { label: 'ОЦЕНКА ФАКТА', color: theme.accentB },
  compare: { label: 'СРАВНЕНИЕ ВЕРСИЙ', color: theme.neutral },
  trend: { label: 'ТРЕНДОВЫЙ АНАЛИЗ', color: theme.positive },
  combined: { label: 'СВОДНЫЙ ОТЧЁТ', color: theme.accentDark },
} as const;

function KindBadge({ label, color }: { label: string; color: string }) {
  return (
    <Box
      sx={{
        bgcolor: color,
        color: theme.white,
        fontSize: 8.5,
        fontWeight: 600,
        letterSpacing: 0.5,
        px: 1.1,
        py: 0.55,
        borderRadius: '3px',
        display: 'inline-block',
      }}
    >
      {label}
    </Box>
  );
}

// ---------------------------------------------------------------------------
// 1. Титульная страница
// ---------------------------------------------------------------------------

function CoverTeaserKpi({ label, value, sub }: { label: string; value: string; sub?: string }) {
  return (
    <Box sx={{ flex: 1 }}>
      <Typography sx={{ fontFamily: FONT, color: theme.coverMutedText, fontSize: 8 }}>{label}</Typography>
      <Typography sx={{ fontFamily: FONT, color: theme.white, fontWeight: 700, fontSize: 19, mt: 0.3 }}>
        {value}
      </Typography>
      {sub ? (
        <Typography sx={{ fontFamily: FONT, color: theme.white, fontSize: 11, mt: 0.35 }}>{sub}</Typography>
      ) : null}
      <Box sx={{ width: 26, height: 2, bgcolor: theme.accentA, mt: 0.6 }} />
    </Box>
  );
}

function CoverPageMockup({
  kindLabel = 'ОЦЕНКА ФАКТА',
  kindColor = theme.accentB,
  title = (
    <>
      Отчёт по оценке
      <br />
      фактической трудоёмкости
    </>
  ),
  teasers = [
    { label: 'ЧАСЫ ФАКТ', value: '12 480 ч' },
    { label: 'ВЗВЕШ. Z', value: '−0.34' },
    { label: 'ПЕРЦЕНТИЛЬ', value: 'P62' },
    { label: 'ОЦЕНКА', value: 'Хорошо' },
  ],
}: {
  kindLabel?: string;
  kindColor?: string;
  title?: ReactNode;
  teasers?: { label: string; value: string; sub?: string }[];
}) {
  return (
    <PageFrame width={PAGE_W} height={PAGE_H} label="Титульная страница · A4 портрет">
      <Box
        sx={{
          position: 'absolute',
          inset: 0,
          background: `linear-gradient(135deg, ${theme.coverGradientTop}, ${theme.coverGradientBottom})`,
        }}
      >
        <Box sx={{ p: 5, height: '100%', display: 'flex', flexDirection: 'column' }}>
          <Stack direction="row" alignItems="flex-start" justifyContent="space-between">
            <Box
              sx={{
                width: 46,
                height: 46,
                borderRadius: '23px',
                bgcolor: theme.watermarkGray,
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                color: theme.white,
                fontWeight: 700,
                fontSize: 15,
              }}
            >
              EP
            </Box>
            <KindBadge label={kindLabel} color={kindColor} />
          </Stack>

          <Typography
            sx={{ fontFamily: FONT, color: theme.white, fontWeight: 700, fontSize: 27, mt: 4, lineHeight: 1.15 }}
          >
            {title}
          </Typography>
          <Typography sx={{ fontFamily: FONT, color: theme.coverMutedText, fontSize: 11.5, mt: 0.6 }}>
            Engineering Performance · Инженерная аналитика трудоёмкости
          </Typography>

          <Box
            sx={{
              mt: 3.5,
              bgcolor: theme.coverMetaCardBg,
              borderLeft: `2px solid ${theme.accentA}`,
              p: 2,
              display: 'flex',
              flexDirection: 'column',
              gap: 1.1,
            }}
          >
            <Stack direction="row" spacing={4}>
              <MetaField label="ПРОЕКТ" value="Реконструкция ГПУ-14" />
              <MetaField label="ВЕРСИЯ" value="Версия 3.2" />
            </Stack>
            <Stack direction="row" spacing={4}>
              <MetaField label="ДАТА ФОРМИРОВАНИЯ" value="26.08.2026 09:41 UTC" />
              <MetaField label="СИСТЕМА" value="Engineering Performance" />
            </Stack>
            <Typography sx={{ fontFamily: FONT, color: theme.coverBodyText, fontSize: 8.5, mt: 0.4 }}>
              Дисциплин: 6 · Разделов: 24 · Позиций: 318
            </Typography>
          </Box>

          <Stack direction="row" spacing={3} sx={{ mt: 3.5 }}>
            {teasers.map((item) => (
              <CoverTeaserKpi key={item.label} label={item.label} value={item.value} sub={item.sub} />
            ))}
          </Stack>

          <Box sx={{ flexGrow: 1 }} />
          <Typography sx={{ fontFamily: FONT, color: theme.coverFooterText, fontSize: 8.5 }}>
            Engineering Performance · Confidential
          </Typography>
        </Box>
      </Box>
    </PageFrame>
  );
}

function MetaField({ label, value }: { label: string; value: string }) {
  return (
    <Box sx={{ flex: 1 }}>
      <Typography sx={{ fontFamily: FONT, color: theme.coverMutedText, fontSize: 7.5 }}>{label}</Typography>
      <Typography sx={{ fontFamily: FONT, color: theme.white, fontWeight: 600, fontSize: 10.5, mt: 0.2 }}>
        {value}
      </Typography>
    </Box>
  );
}

// ---------------------------------------------------------------------------
// Общий хедер / футер контентной страницы
// ---------------------------------------------------------------------------

function ContentHeader({
  title,
  project,
  version,
  kindLabel,
  kindColor,
  extraMeta,
}: {
  title: string;
  project: string;
  version: string;
  kindLabel: string;
  kindColor: string;
  extraMeta?: string;
}) {
  return (
    <Box sx={{ px: 3.5, pt: 3 }}>
      <Stack direction="row" justifyContent="space-between" alignItems="flex-start">
        <Box>
          <Typography sx={{ fontFamily: FONT, fontWeight: 600, fontSize: 15, color: theme.primary }}>
            {title}
          </Typography>
          <Typography sx={{ fontFamily: FONT, fontSize: 9, color: theme.muted, mt: 0.2 }}>
            {project} · {version}
          </Typography>
        </Box>
        <KindBadge label={kindLabel} color={kindColor} />
      </Stack>
      {extraMeta && (
        <Typography sx={{ fontFamily: FONT, fontSize: 8, color: theme.muted, mt: 0.4 }}>{extraMeta}</Typography>
      )}
      <Box sx={{ height: 2, bgcolor: theme.accentA, mt: 1 }} />
    </Box>
  );
}

function ContentFooter({ page }: { page: string }) {
  return (
    <Box sx={{ position: 'absolute', left: 0, right: 0, bottom: 0, px: 3.5, pb: 2 }}>
      <Box sx={{ height: 1, bgcolor: theme.ruleColor }} />
      <Stack direction="row" justifyContent="space-between" sx={{ mt: 0.8 }}>
        <Typography sx={{ fontFamily: FONT, fontSize: 7.5, color: theme.muted }}>
          Engineering Performance · Confidential
        </Typography>
        <Typography sx={{ fontFamily: FONT, fontSize: 7.5, color: theme.muted }}>{page}</Typography>
        <Typography sx={{ fontFamily: FONT, fontSize: 7.5, color: theme.muted }}>
          2026-08-26 09:41 UTC
        </Typography>
      </Stack>
    </Box>
  );
}

// ---------------------------------------------------------------------------
// KPI-карточки + шкала Z-score
// ---------------------------------------------------------------------------

function KpiCard({ label, value, dotColor }: { label: string; value: string; dotColor?: string }) {
  return (
    <Box sx={{ flex: 1, bgcolor: theme.white, p: 1.3 }}>
      <Typography sx={{ fontFamily: FONT, fontSize: 7.5, color: theme.muted, letterSpacing: 0.3 }}>
        {label.toUpperCase()}
      </Typography>
      <Stack direction="row" alignItems="center" spacing={0.6} sx={{ mt: 0.4 }}>
        {dotColor && (
          <Box sx={{ width: 7, height: 7, borderRadius: '50%', bgcolor: dotColor, flexShrink: 0 }} />
        )}
        <Typography sx={{ fontFamily: FONT, fontSize: 14, fontWeight: 600, color: theme.primary }}>
          {value}
        </Typography>
      </Stack>
      <Box sx={{ height: 2, bgcolor: theme.accentA, mt: 0.7 }} />
    </Box>
  );
}

const Z_BANDS: { label: string; range: string; color: string }[] = [
  { label: 'Критично', range: 'Z > 1', color: theme.negative },
  { label: 'Внимание', range: '0.5…1', color: theme.negative },
  { label: 'Удовл.', range: '−0.5…0.5', color: theme.neutral },
  { label: 'Хорошо', range: '−1…−0.5', color: theme.positive },
  { label: 'Отлично', range: 'Z < −1', color: theme.positive },
];

function ZScaleLegend({ z }: { z: number }) {
  const clamped = Math.max(-2, Math.min(2, z));
  const left = Math.min(0.94, Math.max(0.02, (2 - clamped) / 4)) * 100;
  return (
    <Box>
      <Typography sx={{ fontFamily: FONT, fontSize: 9.5, fontWeight: 600, color: theme.primary, mb: 0.5 }}>
        Шкала оценки (Z-score)
      </Typography>
      <Stack direction="row" spacing={0.2}>
        {Z_BANDS.map((b) => (
          <Box key={b.label} sx={{ flex: 1, bgcolor: b.color, py: 0.6, textAlign: 'center' }}>
            <Typography sx={{ fontFamily: FONT, fontSize: 7.5, fontWeight: 600, color: theme.white }}>
              {b.label}
            </Typography>
            <Typography sx={{ fontFamily: FONT, fontSize: 6.5, color: theme.white }}>{b.range}</Typography>
          </Box>
        ))}
      </Stack>
      <Box sx={{ position: 'relative', height: 16, mt: 0.3 }}>
        <Box
          sx={{
            position: 'absolute',
            left: `${left}%`,
            transform: 'translateX(-50%)',
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
          }}
        >
          <Typography sx={{ fontFamily: FONT, fontSize: 9, color: theme.primary, lineHeight: 1 }}>▲</Typography>
          <Typography sx={{ fontFamily: FONT, fontSize: 6.5, color: theme.muted }}>Z = {z.toFixed(2)}</Typography>
        </Box>
      </Box>
    </Box>
  );
}

// ---------------------------------------------------------------------------
// Callout-блоки
// ---------------------------------------------------------------------------

type CalloutKind = 'risk' | 'attention' | 'strength' | 'data';

const CALLOUT_STYLE: Record<CalloutKind, { border: string; bg: string; glyph: string }> = {
  risk: { border: theme.negative, bg: theme.riskBg, glyph: '⚠' },
  attention: { border: theme.neutral, bg: theme.attentionBg, glyph: '⚠' },
  strength: { border: theme.positive, bg: theme.strengthBg, glyph: '✓' },
  data: { border: theme.muted, bg: theme.dataBg, glyph: '●' },
};

function Callout({ kind, title, detail }: { kind: CalloutKind; title: string; detail: string }) {
  const s = CALLOUT_STYLE[kind];
  return (
    <Stack
      direction="row"
      spacing={1}
      sx={{ bgcolor: s.bg, borderTop: `2px solid ${s.border}`, p: 1.1 }}
    >
      <Typography sx={{ fontFamily: FONT, fontSize: 12, color: s.border, fontWeight: 700 }}>{s.glyph}</Typography>
      <Box>
        <Typography sx={{ fontFamily: FONT, fontSize: 9, fontWeight: 600, color: theme.primary }}>{title}</Typography>
        <Typography sx={{ fontFamily: FONT, fontSize: 8, color: theme.muted, mt: 0.2 }}>{detail}</Typography>
      </Box>
    </Stack>
  );
}

// ---------------------------------------------------------------------------
// Иерархическая таблица
// ---------------------------------------------------------------------------

function InterpretationChip({ text, color }: { text: string; color: string }) {
  return (
    <Box
      sx={{
        display: 'inline-block',
        bgcolor: color,
        color: theme.white,
        fontSize: 7.5,
        fontWeight: 600,
        px: 0.8,
        py: 0.2,
        borderRadius: '3px',
      }}
    >
      {text}
    </Box>
  );
}

function IndentedLabel({ text, level }: { text: string; level: number }) {
  return (
    <Stack direction="row" alignItems="center" spacing={0.5} sx={{ pl: level * 1.3 }}>
      {level > 0 && <Box sx={{ width: 2, height: 11, bgcolor: theme.ruleColor }} />}
      <Typography sx={{ fontFamily: FONT, fontSize: 8, color: theme.primary }}>{text}</Typography>
    </Stack>
  );
}

type TableRow = { level: number; cells: string[]; interp?: { text: string; color: string } };

function HierarchicalTable({
  title,
  headers,
  rows,
}: {
  title: string;
  headers: string[];
  rows: TableRow[];
}) {
  return (
    <Box>
      <Typography sx={{ fontFamily: FONT, fontSize: 10.5, fontWeight: 600, color: theme.primary, mb: 0.7 }}>
        {title}
      </Typography>
      <Box>
        <Stack direction="row" sx={{ borderBottom: `2px solid ${theme.accentA}` }}>
          {headers.map((h) => (
            <Box key={h} sx={{ flex: 1, py: 0.6, px: 0.6 }}>
              <Typography sx={{ fontFamily: FONT, fontSize: 7.5, fontWeight: 600, color: theme.primary }}>
                {h}
              </Typography>
            </Box>
          ))}
        </Stack>
        {rows.map((row, idx) => (
          <Stack
            key={idx}
            direction="row"
            sx={{
              bgcolor: idx % 2 === 1 ? theme.sectionBg : theme.white,
              borderBottom: `0.5px solid ${theme.ruleColor}`,
            }}
          >
            {row.cells.map((cell, i) => (
              <Box key={i} sx={{ flex: 1, py: 0.55, px: 0.6 }}>
                {i === 0 && row.level > 0 ? (
                  <IndentedLabel text={cell} level={row.level} />
                ) : row.interp && i === row.cells.length - 1 ? (
                  <InterpretationChip text={row.interp.text} color={row.interp.color} />
                ) : (
                  <Typography sx={{ fontFamily: FONT, fontSize: 7.8, color: theme.primary }}>{cell}</Typography>
                )}
              </Box>
            ))}
          </Stack>
        ))}
      </Box>
    </Box>
  );
}

// ---------------------------------------------------------------------------
// 2. Контентная страница — «Оценка факта» (портрет)
// ---------------------------------------------------------------------------

function FactContentPageMockup() {
  return (
    <PageFrame width={PAGE_W} height={PAGE_H} label="Контентная страница · Оценка факта · A4 портрет">
      <ContentHeader
        title="Оценка фактической трудоёмкости"
        project="Реконструкция ГПУ-14"
        version="Версия 3.2"
        kindLabel="ОЦЕНКА ФАКТА"
        kindColor={theme.accentB}
        extraMeta="Дисциплина: Технологическая · Раздел: Основное оборудование"
      />
      <Box sx={{ px: 3.5, pt: 2, pb: 5, bgcolor: theme.pageBg, height: '100%' }}>
        <Stack spacing={1.6}>
          <Stack direction="row" spacing={1}>
            <KpiCard label="Часы (факт)" value="12 480 ч" />
            <KpiCard label="Взвеш. Z" value="−0.34" />
            <KpiCard label="Перцентиль" value="P62" />
            <KpiCard label="Оценка" value="Хорошо" dotColor={theme.positive} />
          </Stack>

          <Box sx={{ bgcolor: theme.white, p: 1.4 }}>
            <ZScaleLegend z={-0.34} />
          </Box>

          <Stack spacing={0.8}>
            <Callout
              kind="strength"
              title="Сильная сторона: земляные работы"
              detail="Z = −1.62, перцентиль P8 — трудоёмкость существенно ниже нормы по группе аналогов."
            />
            <Callout
              kind="attention"
              title="Внимание: электромонтажные работы"
              detail="Z = 0.71, перцентиль P76 — отклонение выше среднего, рекомендуется проверить нормы."
            />
          </Stack>

          <Box sx={{ bgcolor: theme.white, p: 1.4 }}>
            <HierarchicalTable
              title="Детализация по разделам"
              headers={['Раздел / позиция', 'Часы', 'Z-score', 'Перцентиль', 'Интерпретация']}
              rows={[
                { level: 0, cells: ['Основное оборудование', '6 210 ч', '−0.34', 'P62', ''] },
                {
                  level: 1,
                  cells: ['Монтаж технологических блоков', '4 120 ч', '−0.51', 'P54', 'Хорошо'],
                  interp: { text: 'Хорошо', color: theme.positive },
                },
                {
                  level: 1,
                  cells: ['Пусконаладочные работы', '2 090 ч', '0.12', 'P58', 'Удовл.'],
                  interp: { text: 'Удовл.', color: theme.neutral },
                },
                { level: 0, cells: ['Электромонтажные работы', '3 340 ч', '0.71', 'P76', ''] },
                {
                  level: 1,
                  cells: ['Прокладка кабельных линий', '2 010 ч', '0.94', 'P81', 'Внимание'],
                  interp: { text: 'Внимание', color: theme.negative },
                },
              ]}
            />
          </Box>
        </Stack>
      </Box>
      <ContentFooter page="2 / 6" />
    </PageFrame>
  );
}

// ---------------------------------------------------------------------------
// 3. Контентная страница — «Сравнение версий» (ландшафт, с графиком)
// ---------------------------------------------------------------------------

function ChartPlaceholder({ title }: { title: string }) {
  const bars = [42, 58, 34, 71, 49, 63, 38, 55];
  return (
    <Box sx={{ bgcolor: theme.white, p: 1.6 }}>
      <Typography sx={{ fontFamily: FONT, fontSize: 9.5, fontWeight: 600, color: theme.primary, mb: 1 }}>
        {title}
      </Typography>
      <Stack direction="row" spacing={1} alignItems="flex-end" sx={{ height: 90, px: 1 }}>
        {bars.map((h, i) => (
          <Box
            key={i}
            sx={{
              flex: 1,
              height: `${h}%`,
              bgcolor: i % 3 === 0 ? theme.accentA : theme.accentSoft,
              borderTop: i % 3 === 0 ? 'none' : `2px solid ${theme.accentA}`,
            }}
          />
        ))}
      </Stack>
      <Box sx={{ height: 1, bgcolor: theme.ruleColor, mt: 0.5 }} />
      <Typography sx={{ fontFamily: FONT, fontSize: 7, color: theme.muted, mt: 0.5 }}>
        Версия 3.1 (светлый) → Версия 3.2 (акцент) по разделам, часы
      </Typography>
    </Box>
  );
}

function ChartLinePlaceholder({
  title,
  caption,
  points,
  secondPoints,
}: {
  title: string;
  caption: string;
  points: number[];
  secondPoints?: number[];
}) {
  const toPath = (pts: number[]) =>
    pts
      .map((v, i) => `${i === 0 ? 'M' : 'L'} ${(i / (pts.length - 1)) * 100} ${100 - v}`)
      .join(' ');
  return (
    <Box sx={{ bgcolor: theme.white, p: 1.6 }}>
      <Typography sx={{ fontFamily: FONT, fontSize: 9.5, fontWeight: 600, color: theme.primary, mb: 1 }}>
        {title}
      </Typography>
      <Box sx={{ height: 100, position: 'relative' }}>
        <svg viewBox="0 0 100 100" preserveAspectRatio="none" style={{ width: '100%', height: '100%' }}>
          {[25, 50, 75].map((y) => (
            <line key={y} x1={0} y1={y} x2={100} y2={y} stroke={theme.ruleColor} strokeWidth={0.4} />
          ))}
          {secondPoints && (
            <path d={toPath(secondPoints)} fill="none" stroke={theme.accentB} strokeWidth={1.4} vectorEffect="non-scaling-stroke" />
          )}
          <path d={toPath(points)} fill="none" stroke={theme.accentA} strokeWidth={1.6} vectorEffect="non-scaling-stroke" />
        </svg>
      </Box>
      <Box sx={{ height: 1, bgcolor: theme.ruleColor, mt: 0.5 }} />
      <Typography sx={{ fontFamily: FONT, fontSize: 7, color: theme.muted, mt: 0.5 }}>{caption}</Typography>
    </Box>
  );
}

// ---------------------------------------------------------------------------
// 4. Контентная страница — «Прогноз» (ландшафт, KPI + callout + график сценариев)
// ---------------------------------------------------------------------------

function ForecastContentPageMockup() {
  return (
    <PageFrame width={PAGE_W_L} height={PAGE_H_L} label="Контентная страница · Прогноз — Итого · A4 ландшафт">
      <ContentHeader
        title="Прогноз — Итого"
        project="Реконструкция ГПУ-14"
        version="Версия 3.2"
        kindLabel={KIND_META.forecast.label}
        kindColor={KIND_META.forecast.color}
      />
      <Box sx={{ px: 3.5, pt: 1.8, pb: 4.5, bgcolor: theme.pageBg, height: '100%' }}>
        <Stack spacing={1.4}>
          <Stack direction="row" spacing={1}>
            <KpiCard label="Оптимистичный" value="15 890 ч" />
            <KpiCard label="Базовый" value="18 240 ч" />
            <KpiCard label="Пессимистичный" value="21 470 ч" />
          </Stack>
          <Callout
            kind="data"
            title="Разброс сценариев"
            detail="Диапазон между оптимистичным и пессимистичным сценарием — 5 580 ч (30% от базового). Наибольший вклад — направление «Электромонтаж»."
          />
          <Stack direction="row" spacing={1.6}>
            <Box sx={{ flex: 1 }}>
              <ChartPlaceholder title="Сравнение сценариев трудоёмкости" />
            </Box>
            <Box sx={{ flex: 1 }}>
              <ChartPlaceholder title="Top-15 направлений IIC по базовым часам" />
            </Box>
          </Stack>
        </Stack>
      </Box>
      <ContentFooter page="2 / 4" />
    </PageFrame>
  );
}

// ---------------------------------------------------------------------------
// 5. Контентная страница — «Трендовый анализ» (ландшафт, линейные графики)
// ---------------------------------------------------------------------------

function TrendContentPageMockup() {
  return (
    <PageFrame width={PAGE_W_L} height={PAGE_H_L} label="Контентная страница · Трендовый анализ · A4 ландшафт">
      <ContentHeader
        title="Трендовый анализ"
        project="Проект ID: 142"
        version="Формула: v1-docset-proportional"
        kindLabel={KIND_META.trend.label}
        kindColor={KIND_META.trend.color}
        extraMeta="Гранулярность: по версиям"
      />
      <Box sx={{ px: 3.5, pt: 1.8, pb: 4.5, bgcolor: theme.pageBg, height: '100%' }}>
        <Stack spacing={1.4}>
          <Callout
            kind="attention"
            title="Рост отклонения факта от прогноза"
            detail="Последние 3 версии показывают устойчивый рост Δ(факт-прогноз): +6% → +9% → +13%. Рекомендуется пересмотреть нормы для направления «Электромонтаж»."
          />
          <ChartLinePlaceholder
            title="Факт vs Прогноз"
            caption="Факт (синий) · Прогноз/План (фиолетовый) — часы по датам"
            points={[70, 62, 58, 65, 55, 48, 40, 35]}
            secondPoints={[60, 60, 58, 56, 52, 50, 46, 42]}
          />
          <Stack direction="row" spacing={1.6}>
            <Box sx={{ flex: 1 }}>
              <ChartPlaceholder title="Δ Факт − Прогноз по версиям" />
            </Box>
            <Box sx={{ flex: 1 }}>
              <ChartLinePlaceholder
                title="S-curve — накопительный факт"
                caption="Накопительные фактические часы по датам"
                points={[95, 90, 82, 70, 55, 38, 20, 4]}
              />
            </Box>
          </Stack>
        </Stack>
      </Box>
      <ContentFooter page="1 / 3" />
    </PageFrame>
  );
}

// ---------------------------------------------------------------------------
// 6. Контентная страница — «Сводный отчёт» (ландшафт, KPI + Z-шкала + callouts)
// ---------------------------------------------------------------------------

function CombinedContentPageMockup() {
  return (
    <PageFrame width={PAGE_W_L} height={PAGE_H_L} label="Контентная страница · Сводный отчёт · A4 ландшафт">
      <ContentHeader
        title="Сводный отчёт"
        project="Проект ID: 142"
        version="План v3 · Факт v5 · Сравнение 4/5"
        kindLabel={KIND_META.combined.label}
        kindColor={KIND_META.combined.color}
      />
      <Box sx={{ px: 3.5, pt: 1.8, pb: 4.5, bgcolor: theme.pageBg, height: '100%' }}>
        <Stack spacing={1.3}>
          <Stack direction="row" spacing={1}>
            <KpiCard label="Прогноз база" value="18 240 ч" />
            <KpiCard label="Факт часы" value="12 480 ч" />
            <KpiCard label="Взвеш. Z" value="−0.34" />
            <KpiCard label="Оценка" value="Хорошо" dotColor={theme.positive} />
          </Stack>
          <Box sx={{ bgcolor: theme.white, p: 1.2 }}>
            <ZScaleLegend z={-0.34} />
          </Box>
          <Callout
            kind="strength"
            title="Проект в целом эффективен"
            detail="Взвешенный Z-score −0.34 соответствует оценке «Хорошо» — фактическая трудоёмкость ниже нормы по группе аналогов."
          />
          <Typography sx={{ fontFamily: FONT, fontSize: 9.5, fontWeight: 600, color: theme.primary }}>
            Ключевые визуализации
          </Typography>
          <Stack direction="row" spacing={1.6}>
            <Box sx={{ flex: 1 }}>
              <ChartPlaceholder title="1. Прогнозирование — сценарии" />
            </Box>
            <Box sx={{ flex: 1 }}>
              <ChartPlaceholder title="2. Оценка факта — Z-score top-10" />
            </Box>
          </Stack>
        </Stack>
      </Box>
      <ContentFooter page="1 / 5" />
    </PageFrame>
  );
}

function CompareContentPageMockup() {
  return (
    <PageFrame width={PAGE_W_L} height={PAGE_H_L} label="Контентная страница · Сравнение версий · A4 ландшафт">
      <ContentHeader
        title="Сравнение версий — Детально"
        project="Реконструкция ГПУ-14"
        version="v3.1 → v3.2"
        kindLabel="СРАВНЕНИЕ ВЕРСИЙ"
        kindColor={theme.neutral}
        extraMeta="Постатейное сравнение · Позиций: 318"
      />
      <Box sx={{ px: 3.5, pt: 1.8, pb: 4.5, bgcolor: theme.pageBg, height: '100%' }}>
        <Stack direction="row" spacing={1.6} sx={{ height: '100%' }}>
          <Box sx={{ flex: 1 }}>
            <ChartPlaceholder title="Динамика часов по разделам" />
            <Box sx={{ mt: 1 }}>
              <Callout
                kind="data"
                title="Итог сравнения"
                detail="Суммарное отклонение +4.1% (+512 ч) относительно версии 3.1, основной рост — электромонтаж."
              />
            </Box>
          </Box>
          <Box sx={{ flex: 1.3, bgcolor: theme.white, p: 1.4 }}>
            <HierarchicalTable
              title="Позиции с наибольшим отклонением"
              headers={['Позиция', 'v3.1', 'v3.2', 'Δ', 'Оценка']}
              rows={[
                {
                  level: 0,
                  cells: ['Прокладка кабельных линий', '1 620 ч', '2 010 ч', '+390 ч', 'Рост'],
                  interp: { text: 'Рост', color: theme.negative },
                },
                {
                  level: 0,
                  cells: ['Монтаж технологических блоков', '4 340 ч', '4 120 ч', '−220 ч', 'Снижение'],
                  interp: { text: 'Снижение', color: theme.positive },
                },
                {
                  level: 0,
                  cells: ['Пусконаладочные работы', '1 980 ч', '2 090 ч', '+110 ч', 'Стабильно'],
                  interp: { text: 'Стабильно', color: theme.neutral },
                },
              ]}
            />
          </Box>
        </Stack>
      </Box>
      <ContentFooter page="3 / 6" />
    </PageFrame>
  );
}

// ---------------------------------------------------------------------------
// Легенда токенов
// ---------------------------------------------------------------------------

function Swatch({ label, hex }: { label: string; hex: string }) {
  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <Box sx={{ width: 22, height: 22, bgcolor: hex, borderRadius: '4px', boxShadow: 'inset 0 0 0 1px rgba(0,0,0,0.08)' }} />
      <Box>
        <Typography sx={{ fontFamily: FONT, fontSize: 12, color: '#1B2A38', fontWeight: 600 }}>{label}</Typography>
        <Typography sx={{ fontFamily: FONT, fontSize: 11, color: '#7B8794' }}>{hex}</Typography>
      </Box>
    </Stack>
  );
}

function TabSection({
  tabName,
  reportNames,
  children,
}: {
  tabName: string;
  reportNames: string;
  children: ReactNode;
}) {
  return (
    <Box sx={{ mt: 6 }}>
      <Stack direction="row" alignItems="baseline" spacing={1.5} sx={{ mb: 2.5 }}>
        <Typography sx={{ fontFamily: FONT, fontSize: 18, fontWeight: 700, color: theme.primary }}>
          Вкладка «{tabName}»
        </Typography>
        <Typography sx={{ fontFamily: FONT, fontSize: 12, color: theme.muted }}>{reportNames}</Typography>
      </Stack>
      <Stack direction="row" spacing={5} sx={{ flexWrap: 'wrap', justifyContent: 'center' }} useFlexGap>
        {children}
      </Stack>
    </Box>
  );
}

export default function PdfDesignMockups() {
  return (
    <Box
      sx={{
        position: 'fixed',
        inset: 0,
        overflowY: 'auto',
        overflowX: 'hidden',
        bgcolor: '#F1F4F7',
        py: 6,
      }}
    >
      <Box sx={{ maxWidth: 1300, mx: 'auto', px: 3 }}>
        <Typography sx={{ fontFamily: FONT, fontSize: 26, fontWeight: 700, color: '#1B2A38' }}>
          Дизайн-система PDF-отчётов — «Executive Technical»
        </Typography>
        <Typography sx={{ fontFamily: FONT, fontSize: 14, color: '#7B8794', mt: 0.5, maxWidth: 780 }}>
          Единый визуальный стиль для всех 10 PDF-отчётов, доступных на вкладках страницы «Проекты»: глубокий
          графитовый + инженерный синий акцент, полноценная обложка, структурированный хедер/футер, KPI-карточки,
          шкала Z-score с маркером, краткие авто-выводы (callout) и стилизованные иерархические таблицы. Ниже —
          статичные макеты в масштабе, точно повторяющие композицию QuestPDF-документа; обложка и хедер/футер
          общие для всех типов — различается только бейдж, заголовок и содержимое страницы.
        </Typography>

        <Box sx={{ bgcolor: '#FFFFFF', borderRadius: '10px', p: 2.5, mt: 4, boxShadow: '0 1px 0 rgba(16,24,31,0.06)' }}>
          <Typography sx={{ fontFamily: FONT, fontSize: 12, fontWeight: 700, color: '#1B2A38', letterSpacing: 0.5, mb: 1.5 }}>
            ПАЛИТРА
          </Typography>
          <Stack direction="row" spacing={4} flexWrap="wrap" useFlexGap>
            <Swatch label="Primary" hex={theme.primary} />
            <Swatch label="Accent A" hex={theme.accentA} />
            <Swatch label="Accent B" hex={theme.accentB} />
            <Swatch label="Positive" hex={theme.positive} />
            <Swatch label="Neutral" hex={theme.neutral} />
            <Swatch label="Negative" hex={theme.negative} />
            <Swatch label="Muted" hex={theme.muted} />
          </Stack>
        </Box>

        <Box sx={{ bgcolor: '#FFFFFF', borderRadius: '10px', p: 2.5, mt: 3, boxShadow: '0 1px 0 rgba(16,24,31,0.06)' }}>
          <Typography sx={{ fontFamily: FONT, fontSize: 12, fontWeight: 700, color: '#1B2A38', letterSpacing: 0.5, mb: 1.5 }}>
            БЕЙДЖИ ТИПОВ ОТЧЁТА (обложка и хедер каждой страницы)
          </Typography>
          <Stack direction="row" spacing={2} flexWrap="wrap" useFlexGap>
            {Object.values(KIND_META).map((k) => (
              <KindBadge key={k.label} label={k.label} color={k.color} />
            ))}
          </Stack>
        </Box>

        <Box sx={{ mt: 6 }}>
          <Stack direction="row" alignItems="baseline" spacing={1.5} sx={{ mb: 2.5 }}>
            <Typography sx={{ fontFamily: FONT, fontSize: 18, fontWeight: 700, color: theme.primary }}>
              Титульная страница — общий шаблон
            </Typography>
            <Typography sx={{ fontFamily: FONT, fontSize: 12, color: theme.muted }}>
              Факт — часы/Z; прогноз — три сценария с часами и стоимостью ₽ (только титул)
            </Typography>
          </Stack>
          <Stack direction="row" spacing={2} sx={{ justifyContent: 'center', flexWrap: 'wrap', rowGap: 3 }}>
            <CoverPageMockup />
            <CoverPageMockup
              kindLabel={KIND_META.forecast.label}
              kindColor={KIND_META.forecast.color}
              title={
                <>
                  Прогноз трудоёмкости
                  <br />
                  по сценариям
                </>
              }
              teasers={[
                { label: 'ОПТИМИСТИЧНЫЙ', value: '15 890 ч', sub: '23 835 000 ₽' },
                { label: 'БАЗОВЫЙ', value: '18 240 ч', sub: '27 360 000 ₽' },
                { label: 'ПЕССИМИСТИЧНЫЙ', value: '21 470 ч', sub: '32 205 000 ₽' },
              ]}
            />
          </Stack>
        </Box>

        <TabSection tabName="Планирование" reportNames="Прогноз — Итого / По объектам / Детально">
          <ForecastContentPageMockup />
        </TabSection>

        <TabSection tabName="Оценка факта" reportNames="Факт — Итого / По объектам / Детально">
          <FactContentPageMockup />
        </TabSection>

        <TabSection tabName="Сравнение версий" reportNames="Сравнение — Итого / По объектам / Детально / Полный">
          <CompareContentPageMockup />
        </TabSection>

        <TabSection tabName="Тренды" reportNames="Трендовый анализ">
          <TrendContentPageMockup />
        </TabSection>

        <TabSection tabName="Проекты → Сводный отчёт PDF" reportNames="Сводный отчёт (прогноз + факт + сравнение + тренд)">
          <CombinedContentPageMockup />
        </TabSection>
      </Box>
    </Box>
  );
}
