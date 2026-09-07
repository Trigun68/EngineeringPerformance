import { useEffect, useMemo, useState } from 'react';
import { ChevronDown, ChevronRight } from 'lucide-react';
import type { PerformanceReportMergedDirectionRow } from '../../../api/performanceReportApi';
import { getUserPreferences, putUserPreferences } from '../../../api/userPreferencesApi';
import { reportCollapsedPrefKey } from '../../../utils/projectWorkspacePrefs';
import { fmt, fmtSigned, interpretationColor } from './reportFormat';

type Props = {
  projectId: number;
  rows: PerformanceReportMergedDirectionRow[];
};

function parseCollapsed(raw: string | undefined): Set<number> {
  if (!raw) return new Set();
  try {
    const ids = JSON.parse(raw) as unknown;
    if (!Array.isArray(ids)) return new Set();
    return new Set(
      ids.filter((id): id is number => typeof id === 'number' && Number.isFinite(id)),
    );
  } catch {
    return new Set();
  }
}

export function DirectionsTable({ projectId, rows }: Props) {
  const [collapsed, setCollapsed] = useState<Set<number>>(() => new Set());

  useEffect(() => {
    let cancelled = false;
    setCollapsed(new Set());
    void getUserPreferences()
      .then((prefs) => {
        if (cancelled) return;
        setCollapsed(parseCollapsed(prefs[reportCollapsedPrefKey(projectId)]));
      })
      .catch(() => {
        // оставляем пустой набор
      });
    return () => {
      cancelled = true;
    };
  }, [projectId]);

  const childrenByParent = useMemo(() => {
    const map = new Map<number | null, PerformanceReportMergedDirectionRow[]>();
    for (const row of rows) {
      const key = row.parentId;
      const list = map.get(key) ?? [];
      list.push(row);
      map.set(key, list);
    }
    return map;
  }, [rows]);

  const visible = useMemo(() => {
    const out: PerformanceReportMergedDirectionRow[] = [];
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
      void putUserPreferences({
        [reportCollapsedPrefKey(projectId)]: JSON.stringify([...next]),
      });
      return next;
    });
  };

  if (rows.length === 0) {
    return <p className="text-sm text-muted-foreground">Нет строк производительности.</p>;
  }

  return (
    <div className="overflow-x-auto" data-testid="report-directions-table">
      <table className="w-full min-w-[1100px] border-collapse text-sm">
        <thead>
          <tr className="border-b border-border bg-slate-800 text-left text-xs text-slate-100 dark:bg-slate-900">
            <th className="px-2 py-2.5 font-semibold">Направление</th>
            <th className="px-2 py-2.5 text-right font-semibold">Объём</th>
            <th className="px-2 py-2.5 text-right font-semibold">Труд. план</th>
            <th className="px-2 py-2.5 text-right font-semibold">Труд. факт</th>
            <th className="px-2 py-2.5 text-right font-semibold">Произв. план</th>
            <th className="px-2 py-2.5 text-right font-semibold">Произв. факт</th>
            <th className="px-2 py-2.5 text-right font-semibold">Норма</th>
            <th className="px-2 py-2.5 text-right font-semibold">σ</th>
            <th className="px-2 py-2.5 text-right font-semibold">Заложенный Z</th>
            <th className="px-2 py-2.5 text-right font-semibold">Z факт</th>
          </tr>
        </thead>
        <tbody>
          {visible.map((row) => {
            const hasChildren = (childrenByParent.get(row.workDirectionId) ?? []).length > 0;
            const isCollapsed = collapsed.has(row.workDirectionId);
            const planColor = interpretationColor(row.planZScore);
            const factColor = interpretationColor(row.factZScore);
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
                  </div>
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs">
                  {fmt(
                    row.level === 0
                      ? (row.subtreePhysicalVolume ?? row.physicalVolume)
                      : row.physicalVolume,
                    1,
                  )}
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs">
                  {fmt(row.planLaborHours, 1)}
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs">{fmt(row.factLaborHours, 1)}</td>
                <td className="px-2 py-2 text-right font-mono text-xs">
                  {fmt(row.planProductivity, 2)}
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs">
                  {fmt(row.factProductivity, 2)}
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs">
                  {fmt(row.normProductivity, 2)}
                </td>
                <td className="px-2 py-2 text-right font-mono text-xs">
                  {fmt(row.normStdDev, 2)}
                </td>
                <td
                  className="px-2 py-2 text-right font-mono text-sm font-semibold"
                  style={{ color: planColor }}
                >
                  {fmtSigned(row.planZScore)}
                </td>
                <td
                  className="px-2 py-2 text-right font-mono text-sm font-semibold"
                  style={{ color: factColor }}
                >
                  {fmtSigned(row.factZScore)}
                </td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
