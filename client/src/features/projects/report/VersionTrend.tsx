import type { PerformanceReportHistoryPoint } from '../../../api/performanceReportApi';
import { fmt, fmtSigned, interpretationColor } from './reportFormat';
import { ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';

const W = 640;
const H = 200;
const PAD_X = 28;
const PAD_Y = 24;

type Props = {
  points: PerformanceReportHistoryPoint[];
};

export function VersionTrend({ points }: Props) {
  if (points.length === 0) {
    return (
      <ReportCard>
        <ReportCardHeader>
          <ReportCardTitle>Динамика Z-оценки по версиям</ReportCardTitle>
        </ReportCardHeader>
        <ReportCardContent>
          <p className="text-sm text-muted-foreground">Нет версий с рассчитанной Z-оценкой.</p>
        </ReportCardContent>
      </ReportCard>
    );
  }

  const values = points.map((p) => p.weightedZScore ?? 0);
  const max = Math.max(...values, 0.6);
  const min = Math.min(...values, -0.2);
  const span = max - min || 1;

  const x = (i: number) => PAD_X + (i * (W - PAD_X * 2)) / Math.max(points.length - 1, 1);
  const y = (v: number) => PAD_Y + ((max - v) / span) * (H - PAD_Y * 2);

  const line = points
    .map((p, i) => `${x(i)},${y(p.weightedZScore ?? 0)}`)
    .join(' ');
  const last = points[points.length - 1];

  return (
    <ReportCard>
      <ReportCardHeader>
        <ReportCardTitle>Динамика Z-оценки по версиям</ReportCardTitle>
        <p className="text-sm text-muted-foreground">
          Версии упорядочены по дате создания (план перед фактом при одинаковой дате).
        </p>
      </ReportCardHeader>
      <ReportCardContent className="flex flex-col gap-4">
        <svg
          viewBox={`0 0 ${W} ${H}`}
          className="h-52 w-full"
          role="img"
          aria-label={`Динамика Z: от ${fmtSigned(points[0].weightedZScore)} до ${fmtSigned(last.weightedZScore)}`}
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
          <polyline
            points={line}
            fill="none"
            stroke="var(--ep-report-primary)"
            strokeWidth={2}
            strokeLinecap="round"
            strokeLinejoin="round"
          />
          {points.map((p, i) => (
            <g key={`${p.versionId}-${p.direction}`}>
              <circle
                cx={x(i)}
                cy={y(p.weightedZScore ?? 0)}
                r={5}
                fill={interpretationColor(p.weightedZScore)}
                stroke="var(--ep-report-card)"
                strokeWidth={2}
              />
              <text
                x={x(i)}
                y={y(p.weightedZScore ?? 0) - 12}
                textAnchor="middle"
                className="fill-[var(--ep-report-foreground)] font-mono text-[10px] font-semibold"
              >
                {fmtSigned(p.weightedZScore)}
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

        <div className="grid grid-cols-2 gap-4 border-t border-border pt-4 sm:grid-cols-4">
          {points.map((p) => (
            <div key={`${p.versionId}-meta`} className="flex flex-col gap-0.5">
              <span className="text-xs text-muted-foreground">
                {new Date(p.createdAt).toLocaleDateString('ru-RU')} ·{' '}
                {p.direction === 1 ? 'План' : 'Факт'}
              </span>
              <span className="text-sm font-medium">{p.versionName}</span>
              <span className="font-mono text-xs text-muted-foreground">
                {fmt(p.totalLaborHours, 0)} ч
              </span>
            </div>
          ))}
        </div>
      </ReportCardContent>
    </ReportCard>
  );
}
