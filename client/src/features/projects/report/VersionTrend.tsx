import type { PerformanceReportHistoryPoint } from '../../../api/performanceReportApi';
import { fmt, fmtSigned, interpretationColor } from './reportFormat';
import { ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';

const W = 640;
const H = 220;
const PAD_X = 28;
const PAD_Y = 28;

const PLAN = 1;
const FACT = 2;

type Props = {
  points: PerformanceReportHistoryPoint[];
};

function seriesPath(
  indexed: Array<{ i: number; z: number }>,
  x: (i: number) => number,
  y: (v: number) => number,
): string {
  return indexed.map((p) => `${x(p.i)},${y(p.z)}`).join(' ');
}

export function VersionTrend({ points }: Props) {
  const withZ = points
    .map((p, i) => ({ p, i, z: p.weightedZScore }))
    .filter((x): x is { p: PerformanceReportHistoryPoint; i: number; z: number } => x.z != null);

  if (points.length === 0 || withZ.length === 0) {
    return (
      <ReportCard>
        <ReportCardHeader>
          <ReportCardTitle className="text-base md:text-lg">Динамика Z-оценки по версиям</ReportCardTitle>
        </ReportCardHeader>
        <ReportCardContent>
          <p className="text-sm text-muted-foreground">Нет версий с рассчитанной Z-оценкой.</p>
        </ReportCardContent>
      </ReportCard>
    );
  }

  const zs = withZ.map((x) => x.z);
  const max = Math.max(...zs, 0.6);
  const min = Math.min(...zs, -0.2);
  const span = max - min || 1;

  const x = (i: number) => PAD_X + (i * (W - PAD_X * 2)) / Math.max(points.length - 1, 1);
  const y = (v: number) => PAD_Y + ((max - v) / span) * (H - PAD_Y * 2);

  const planSeries = withZ.filter((x) => x.p.direction === PLAN).map((x) => ({ i: x.i, z: x.z }));
  const factSeries = withZ.filter((x) => x.p.direction === FACT).map((x) => ({ i: x.i, z: x.z }));

  return (
    <ReportCard>
      <ReportCardHeader>
        <ReportCardTitle className="text-base md:text-lg">Динамика Z-оценки по версиям</ReportCardTitle>
        <p className="text-sm text-muted-foreground">
          Версии упорядочены по дате создания. Точки без Z не отображаются. План и факт — отдельные
          серии.
        </p>
      </ReportCardHeader>
      <ReportCardContent className="flex flex-col gap-4">
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
              points={seriesPath(planSeries, x, y)}
              fill="none"
              stroke="var(--ep-report-primary)"
              strokeWidth={2}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          ) : null}
          {factSeries.length > 1 ? (
            <polyline
              points={seriesPath(factSeries, x, y)}
              fill="none"
              stroke="var(--ep-report-perf-attention)"
              strokeWidth={2}
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeDasharray="6 4"
            />
          ) : null}

          {withZ.map(({ p, i, z }) => (
            <g key={`${p.versionId}-${p.direction}`}>
              <circle
                cx={x(i)}
                cy={y(z)}
                r={5}
                fill={interpretationColor(z)}
                stroke="var(--ep-report-card)"
                strokeWidth={2}
              />
              <text
                x={x(i)}
                y={y(z) - 12}
                textAnchor="middle"
                className="fill-[var(--ep-report-foreground)] font-mono text-[10px] font-semibold"
              >
                {fmtSigned(z)}
              </text>
              <text
                x={x(i)}
                y={H - 6}
                textAnchor="middle"
                className="fill-[var(--ep-report-muted-foreground)] text-[10px]"
              >
                {p.versionName}
              </text>
            </g>
          ))}
        </svg>

        <div className="flex flex-wrap gap-4 text-xs text-muted-foreground">
          <span className="inline-flex items-center gap-1.5">
            <span className="inline-block h-0.5 w-4 bg-primary" /> План
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span
              className="inline-block h-0.5 w-4"
              style={{
                background:
                  'repeating-linear-gradient(90deg, var(--ep-report-perf-attention) 0 4px, transparent 4px 8px)',
              }}
            />{' '}
            Факт
          </span>
        </div>

        <div className="grid grid-cols-2 gap-4 border-t border-border pt-4 sm:grid-cols-4">
          {points.map((p) => (
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
