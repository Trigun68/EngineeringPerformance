import type { PerformanceReportHistoryPoint } from '../../../api/performanceReportApi';
import { fmt, fmtSigned } from './reportFormat';
import { ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';

const W = 640;
const H = 220;
const PAD_X = 28;
const PAD_Y = 28;

const PLAN = 1;
const FACT = 2;

const PLAN_COLOR = 'var(--ep-report-primary)';
const FACT_COLOR = 'var(--ep-report-perf-critical)';

type Props = {
  points: PerformanceReportHistoryPoint[];
};

type PointWithZ = {
  p: PerformanceReportHistoryPoint;
  z: number;
  t: number;
  snapshotChanged: boolean;
};

function seriesPath(
  items: Array<{ t: number; z: number }>,
  x: (t: number) => number,
  y: (v: number) => number,
): string {
  return items.map((p) => `${x(p.t)},${y(p.z)}`).join(' ');
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
        t: new Date(p.createdAt).getTime(),
        snapshotChanged,
      };
    })
    .filter((x): x is PointWithZ => x != null);
}

export function VersionTrend({ points }: Props) {
  const withZ = withSnapshotTint(points);

  if (points.length === 0 || withZ.length === 0) {
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
  const times = points.map((p) => new Date(p.createdAt).getTime());
  const minT = Math.min(...times);
  const maxT = Math.max(...times);
  const tSpan = maxT - minT || 1;

  const x = (t: number) => PAD_X + ((t - minT) / tSpan) * (W - PAD_X * 2);
  const y = (v: number) => PAD_Y + ((max - v) / span) * (H - PAD_Y * 2);

  const planSeries = withZ.filter((x) => x.p.direction === PLAN).map((x) => ({ t: x.t, z: x.z }));
  const factSeries = withZ.filter((x) => x.p.direction === FACT).map((x) => ({ t: x.t, z: x.z }));

  return (
    <ReportCard>
      <ReportCardHeader>
        <ReportCardTitle className="text-base md:text-lg">Динамика Z-оценки по версиям</ReportCardTitle>
        <p className="text-sm text-muted-foreground">
          Ось X — дата создания версии. План — синий, факт — красный. Смена состава снимка норм
          подсвечена более светлой точкой.
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
              stroke={PLAN_COLOR}
              strokeWidth={2}
              strokeLinecap="round"
              strokeLinejoin="round"
            />
          ) : null}
          {factSeries.length > 1 ? (
            <polyline
              points={seriesPath(factSeries, x, y)}
              fill="none"
              stroke={FACT_COLOR}
              strokeWidth={2}
              strokeLinecap="round"
              strokeLinejoin="round"
              strokeDasharray="6 4"
            />
          ) : null}

          {withZ.map(({ p, z, t, snapshotChanged }) => {
            const color = p.direction === PLAN ? PLAN_COLOR : FACT_COLOR;
            return (
              <g key={`${p.versionId}-${p.direction}`}>
                <circle
                  cx={x(t)}
                  cy={y(z)}
                  r={5}
                  fill={color}
                  fillOpacity={snapshotChanged ? 0.45 : 1}
                  stroke="var(--ep-report-card)"
                  strokeWidth={2}
                  data-series={p.direction === PLAN ? 'plan' : 'fact'}
                  data-snapshot-changed={snapshotChanged ? 'true' : 'false'}
                />
                <text
                  x={x(t)}
                  y={y(z) - 12}
                  textAnchor="middle"
                  className="fill-[var(--ep-report-foreground)] font-mono text-[10px] font-semibold"
                >
                  {fmtSigned(z)}
                </text>
                <text
                  x={x(t)}
                  y={H - 6}
                  textAnchor="middle"
                  className="fill-[var(--ep-report-muted-foreground)] text-[10px]"
                >
                  {p.versionName}
                </text>
              </g>
            );
          })}
        </svg>

        <div className="flex flex-wrap gap-4 text-xs text-muted-foreground">
          <span className="inline-flex items-center gap-1.5">
            <span className="inline-block h-0.5 w-4" style={{ backgroundColor: PLAN_COLOR }} /> План
          </span>
          <span className="inline-flex items-center gap-1.5">
            <span
              className="inline-block h-0.5 w-4"
              style={{
                background:
                  'repeating-linear-gradient(90deg, var(--ep-report-perf-critical) 0 4px, transparent 4px 8px)',
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
