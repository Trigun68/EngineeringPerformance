import { useState } from 'react';
import type { PerformanceReportHistoryPoint } from '../../../api/performanceReportApi';
import { fmt, fmtSigned } from './reportFormat';
import { ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';

const W = 640;
const H = 220;
const PAD_X = 32;
const PAD_Y = 36;

const PLAN = 1;
const FACT = 2;

// Акценты план/факт — паритет новому дизайну (reportCalc ACCENT_PLAN / ACCENT_FACT).
const PLAN_COLOR = 'var(--ep-report-accent-plan)';
const FACT_COLOR = 'var(--ep-report-accent-fact)';

type Props = {
  points: PerformanceReportHistoryPoint[];
};

type PointWithZ = {
  p: PerformanceReportHistoryPoint;
  z: number;
  snapshotChanged: boolean;
};

function seriesPath(items: Array<{ cx: number; z: number }>, y: (v: number) => number): string {
  return items.map((p) => `${p.cx},${y(p.z)}`).join(' ');
}

function withSnapshotTint(points: PerformanceReportHistoryPoint[]): PointWithZ[] {
  const previousKey = new Map<number, string | null | undefined>();
  return points
    .map((p) => {
      const z = p.weightedZScore;
      if (z == null) return null;
      const prev = previousKey.get(p.direction);
      const snapshotChanged =
        prev !== undefined && (p.normSnapshotKey ?? null) !== (prev ?? null);
      previousKey.set(p.direction, p.normSnapshotKey);
      return {
        p,
        z,
        snapshotChanged,
      };
    })
    .filter((x): x is PointWithZ => x != null);
}

/** Строгая хронология: по дате создания, при равенстве — по id версии. */
function byChronology(a: PerformanceReportHistoryPoint, b: PerformanceReportHistoryPoint): number {
  const dt = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
  return dt !== 0 ? dt : a.versionId - b.versionId;
}

function scenarioTooltipLines(p: PerformanceReportHistoryPoint): string[] {
  const line = (label: string, z: number | null | undefined, hours: number | null | undefined) =>
    `${label}: Z ${fmtSigned(z)} · ${fmt(hours, 0)} ч`;
  return [
    p.versionName,
    line('Оптимистичный', p.optimisticZScore, p.optimisticLaborHours),
    line('Базовый', p.baseZScore, p.baseLaborHours),
    line('Пессимистичный', p.pessimisticZScore, p.pessimisticLaborHours),
  ];
}

export function VersionTrend({ points }: Props) {
  const [hoveredKey, setHoveredKey] = useState<string | null>(null);
  // Ось X — категориальная, точки отсортированы по дате: порядок всегда хронологический,
  // даже когда у версий совпадают даты (time-scale схлопывал бы их в одну координату).
  const sortedPoints = [...points].sort(byChronology);
  const withZ = withSnapshotTint(sortedPoints);

  if (sortedPoints.length === 0 || withZ.length === 0) {
    return (
      <ReportCard>
        <ReportCardHeader>
          <ReportCardTitle className="text-base md:text-lg">Динамика Z-оценки по версиям</ReportCardTitle>
        </ReportCardHeader>
        <ReportCardContent>
          <p className="text-sm text-muted-foreground" data-testid="report-version-trend-empty">
            Нет версий с рассчитанной Z-оценкой.
          </p>
        </ReportCardContent>
      </ReportCard>
    );
  }

  const zs = withZ.map((x) => x.z);
  const max = Math.max(...zs, 0.6);
  const min = Math.min(...zs, -0.2);
  const span = max - min || 1;

  const indexByKey = new Map(withZ.map((x, i) => [`${x.p.versionId}-${x.p.direction}`, i]));
  const xIndex = (key: string) => {
    const i = indexByKey.get(key) ?? 0;
    return withZ.length === 1
      ? W / 2
      : PAD_X + (i / (withZ.length - 1)) * (W - PAD_X * 2);
  };
  const y = (v: number) => PAD_Y + ((max - v) / span) * (H - PAD_Y * 2);

  const keyOf = (p: PerformanceReportHistoryPoint) => `${p.versionId}-${p.direction}`;
  const hovered = hoveredKey ? withZ.find((x) => keyOf(x.p) === hoveredKey)?.p : undefined;
  const planSeries = withZ
    .filter((x) => x.p.direction === PLAN)
    .map((x) => ({ cx: xIndex(keyOf(x.p)), z: x.z }));
  const factSeries = withZ
    .filter((x) => x.p.direction === FACT)
    .map((x) => ({ cx: xIndex(keyOf(x.p)), z: x.z }));

  return (
    <ReportCard>
      <ReportCardHeader>
        <ReportCardTitle className="text-base md:text-lg">Динамика Z-оценки по версиям</ReportCardTitle>
        <p className="text-sm text-muted-foreground">
          Ось X — дата создания версии в хронологическом порядке. План — синий, факт — тёмный.
          Смена состава снимка норм подсвечена более светлой точкой.
        </p>
      </ReportCardHeader>
      <ReportCardContent className="flex flex-col gap-4">
        <div className="relative">
        <svg
          viewBox={`0 0 ${W} ${H}`}
          className="h-56 w-full"
          role="img"
          aria-label="Динамика Z-оценки по версиям плана и факта"
          data-testid="report-version-trend"
        >
          <line
            x1={PAD_X}
            x2={W - PAD_X}
            y1={y(0)}
            y2={y(0)}
            stroke="var(--ep-report-border)"
            strokeDasharray="4 4"
          />
          <text
            x={W - PAD_X}
            y={y(0) - 6}
            textAnchor="end"
            className="fill-[var(--ep-report-muted-foreground)] text-[10px]"
          >
            норма (Z = 0)
          </text>

          {planSeries.length > 1 ? (
            <polyline
              points={seriesPath(planSeries, y)}
              fill="none"
              stroke={PLAN_COLOR}
              strokeWidth={2}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          ) : null}
          {factSeries.length > 1 ? (
            <polyline
              points={seriesPath(factSeries, y)}
              fill="none"
              stroke={FACT_COLOR}
              strokeWidth={2.5}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          ) : null}

          {withZ.map(({ p, z, snapshotChanged }) => {
            const isFact = p.direction === FACT;
            const color = isFact ? FACT_COLOR : PLAN_COLOR;
            const cx = xIndex(keyOf(p));
            const cy = y(z);
            const pointKey = keyOf(p);
            const tooltipLines = scenarioTooltipLines(p);
            return (
              <g
                key={pointKey}
                data-testid="report-version-trend-point"
                onMouseEnter={() => setHoveredKey(pointKey)}
                onMouseLeave={() => setHoveredKey((current) => (current === pointKey ? null : current))}
              >
                <title>{tooltipLines.join('\n')}</title>
                {isFact ? (
                  <circle
                    cx={cx}
                    cy={cy}
                    r={9}
                    fill={color}
                    fillOpacity={0.25}
                    aria-hidden
                  />
                ) : null}
                <circle
                  cx={cx}
                  cy={cy}
                  r={isFact ? 6 : 5}
                  fill={color}
                  fillOpacity={snapshotChanged ? 0.45 : 1}
                  stroke="var(--ep-report-card)"
                  strokeWidth={2}
                  data-series={isFact ? 'fact' : 'plan'}
                  data-snapshot-changed={snapshotChanged ? 'true' : 'false'}
                />
                <text
                  x={cx}
                  y={cy - 22}
                  textAnchor="middle"
                  className="fill-[var(--ep-report-foreground)] font-mono text-[10px] font-semibold"
                  data-testid="report-version-trend-point-label"
                >
                  {fmtSigned(z)}
                </text>
                <text
                  x={cx}
                  y={cy - 10}
                  textAnchor="middle"
                  className="fill-[var(--ep-report-muted-foreground)] font-mono text-[9px]"
                  data-testid="report-version-trend-point-hours"
                >
                  {fmt(p.totalLaborHours, 0)} ч
                </text>
                <text
                  x={cx}
                  y={H - 18}
                  textAnchor="middle"
                  className="fill-[var(--ep-report-muted-foreground)] text-[9px]"
                >
                  {p.versionName}
                </text>
                <text
                  x={cx}
                  y={H - 6}
                  textAnchor="middle"
                  className="fill-[var(--ep-report-muted-foreground)] text-[10px]"
                  data-testid="report-version-trend-date"
                >
                  {new Date(p.createdAt).toLocaleDateString('ru-RU')}
                </text>
              </g>
            );
          })}
        </svg>
        {hovered ? (
          <div
            data-testid="report-version-trend-tooltip"
            className="pointer-events-none absolute right-2 top-2 rounded-md border border-border bg-card px-2.5 py-1.5 text-[11px] leading-4 text-foreground shadow-sm"
          >
            {scenarioTooltipLines(hovered).map((line, index) => (
              <div key={line} className={index === 0 ? 'font-semibold' : undefined}>
                {line}
              </div>
            ))}
          </div>
        ) : null}
        </div>

        <div className="flex flex-wrap gap-4 text-xs text-muted-foreground">
          <span className="inline-flex items-center gap-1.5">
            <span className="inline-block h-0.5 w-4" style={{ backgroundColor: PLAN_COLOR }} /> План
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span className="inline-block h-1 w-4" style={{ backgroundColor: FACT_COLOR }} /> Факт
          </span>
        </div>

        <div className="grid grid-cols-2 gap-4 border-t border-border pt-4 sm:grid-cols-4">
          {sortedPoints.map((p) => (
            <div key={`${p.versionId}-meta`} className="flex flex-col gap-0.5">
              <span className="text-xs text-muted-foreground">
                {new Date(p.createdAt).toLocaleDateString('ru-RU')} ·{' '}
                {p.direction === PLAN ? 'План' : 'Факт'}
              </span>
              <span className="text-sm font-medium">{p.versionName}</span>
              <span className="font-mono text-xs text-muted-foreground">
                Z {fmtSigned(p.weightedZScore)} · {fmt(p.totalLaborHours, 0)} ч
              </span>
            </div>
          ))}
        </div>
      </ReportCardContent>
    </ReportCard>
  );
}
