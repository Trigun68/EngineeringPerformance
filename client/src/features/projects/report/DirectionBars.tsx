import type { PerformanceReportDirectionRow } from '../../../api/performanceReportApi';
import { fmt, fmtSigned, interpretationColor } from './reportFormat';
import { ReportCard, ReportCardContent, ReportCardHeader, ReportCardTitle } from './ui';

const LIMIT = 3;

type Props = {
  rows: PerformanceReportDirectionRow[];
  title: string;
  description: string;
};

/** Корневые направления с Z: дивергентные полосы. */
export function DirectionBars({ rows, title, description }: Props) {
  const ranked = rows
    .filter((r) => r.level === 0 && r.zScore != null)
    .sort((a, b) => (a.zScore ?? 0) - (b.zScore ?? 0));

  return (
    <ReportCard className="h-full">
      <ReportCardHeader>
        <ReportCardTitle>{title}</ReportCardTitle>
        <p className="text-sm text-muted-foreground">{description}</p>
      </ReportCardHeader>
      <ReportCardContent className="flex flex-col gap-3">
        {ranked.length === 0 ? (
          <p className="text-sm text-muted-foreground">Нет направлений с Z-оценкой.</p>
        ) : (
          ranked.map((row) => {
            const z = row.zScore!;
            const width = (Math.min(Math.abs(z), LIMIT) / LIMIT) * 50;
            const color = interpretationColor(z);
            return (
              <div key={row.workDirectionId} className="flex items-center gap-3">
                <div className="flex w-40 shrink-0 flex-col md:w-52">
                  <span className="truncate text-sm font-medium" title={row.name}>
                    {row.name}
                  </span>
                  <span className="font-mono text-[11px] text-muted-foreground">
                    {row.code} · {fmt(row.subtreeHours, 0)} ч
                  </span>
                </div>
                <div className="relative h-6 flex-1 rounded-sm bg-muted">
                  <span className="absolute bottom-0 left-1/2 top-0 w-px bg-border" />
                  <span
                    className="absolute bottom-1 top-1 rounded-sm"
                    style={{
                      backgroundColor: color,
                      width: `${width}%`,
                      left: z < 0 ? `${50 - width}%` : '50%',
                    }}
                  />
                </div>
                <span
                  className="w-14 shrink-0 text-right font-mono text-sm font-semibold"
                  style={{ color }}
                >
                  {fmtSigned(z)}
                </span>
              </div>
            );
          })
        )}
        <p className="mt-auto border-t border-border pt-3 text-xs text-muted-foreground">
          Влево от оси — опережение нормы, вправо — перерасход трудозатрат.
        </p>
      </ReportCardContent>
    </ReportCard>
  );
}
