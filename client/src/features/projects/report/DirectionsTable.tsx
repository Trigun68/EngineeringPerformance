import { useMemo, useState } from 'react';
import { ChevronDown, ChevronRight } from 'lucide-react';
import type { PerformanceReportDirectionRow } from '../../../api/performanceReportApi';
import { fmt, fmtSigned, interpretationColor } from './reportFormat';
import { ReportBadge } from './ui';

type Props = {
  rows: PerformanceReportDirectionRow[];
};

export function DirectionsTable({ rows }: Props) {
  const [collapsed, setCollapsed] = useState<Set<number>>(() => new Set());

  const childrenByParent = useMemo(() => {
    const map = new Map<number | null, PerformanceReportDirectionRow[]>();
    for (const row of rows) {
      const key = row.parentId;
      const list = map.get(key) ?? [];
      list.push(row);
      map.set(key, list);
    }
    return map;
  }, [rows]);

  const visible = useMemo(() => {
    const out: PerformanceReportDirectionRow[] = [];
    const walk = (parentId: number | null, hidden: boolean) => {
      const kids = childrenByParent.get(parentId) ?? [];
      for (const row of kids) {
        if (!hidden) out.push(row);
        const nextHidden = hidden || collapsed.has(row.workDirectionId);
        walk(row.workDirectionId, nextHidden);
      }
    };
    walk(null, false);
    return out;
  }, [childrenByParent, collapsed]);

  const toggle = (id: number) => {
    setCollapsed((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  if (rows.length === 0) {
    return <p className="text-sm text-muted-foreground">Нет строк производительности.</p>;
  }

  return (
    <div className="overflow-x-auto">
      <table className="w-full min-w-[900px] border-collapse text-sm">
        <thead>
          <tr className="border-b border-border text-left text-xs text-muted-foreground">
            <th className="px-2 py-2 font-medium">Направление</th>
            <th className="px-2 py-2 text-right font-medium">Объём</th>
            <th className="px-2 py-2 text-right font-medium">Часы</th>
            <th className="px-2 py-2 text-right font-medium">Поддерево</th>
            <th className="px-2 py-2 text-right font-medium">Факт. произв.</th>
            <th className="px-2 py-2 text-right font-medium">Норма</th>
            <th className="px-2 py-2 text-right font-medium">σ</th>
            <th className="px-2 py-2 text-right font-medium">Z</th>
            <th className="px-2 py-2 font-medium">Интерпретация</th>
          </tr>
        </thead>
        <tbody>
          {visible.map((row) => {
            const hasChildren = (childrenByParent.get(row.workDirectionId) ?? []).length > 0;
            const isCollapsed = collapsed.has(row.workDirectionId);
            const color = interpretationColor(row.zScore);
            return (
              <tr
                key={row.workDirectionId}
                className={`border-b border-border ${row.level === 0 ? 'bg-muted/40' : ''}`}
              >
                <td className="px-2 py-2">
                  <div
                    className="flex items-center gap-2"
                    style={{ paddingLeft: `${row.level * 16}px` }}
                  >
                    {hasChildren ? (
                      <button
                        type="button"
                        onClick={() => toggle(row.workDirectionId)}
                        className="flex size-5 shrink-0 items-center justify-center rounded-sm text-muted-foreground hover:bg-accent"
                        aria-expanded={!isCollapsed}
                      >
                        {isCollapsed ? (
                          <ChevronRight className="size-4" />
                        ) : (
                          <ChevronDown className="size-4" />
                        )}
                      </button>
                    ) : (
                      <span className="size-5 shrink-0" />
                    )}
                    <span className={row.level === 0 ? 'font-semibold' : ''}>{row.name}</span>
                    <span className="font-mono text-[11px] text-muted-foreground">{row.code}</span>
                  </div>
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs">
                  {row.factPhysicalVolume == null
                    ? '—'
                    : `${fmt(row.factPhysicalVolume)} ${row.unitName ?? ''}`}
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs">
                  {fmt(row.factLaborHours)}
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs text-muted-foreground">
                  {fmt(row.subtreeHours)}
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs">
                  {fmt(row.factProductivity, 4)}
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs text-muted-foreground">
                  {fmt(row.normProductivity, 4)}
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs text-muted-foreground">
                  {fmt(row.normStdDev, 4)}
                </td>
                <td
                  className="px-2 py-2 text-right font-mono text-sm font-semibold"
                  style={{ color }}
                >
                  {fmtSigned(row.zScore)}
                </td>
                <td className="px-2 py-2">
                  {row.interpretation ? (
                    <ReportBadge style={{ color, borderColor: 'currentColor' }}>
                      {row.interpretation}
                    </ReportBadge>
                  ) : (
                    <span className="text-xs text-muted-foreground">Н/Д</span>
                  )}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
