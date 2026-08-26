import type { CSSProperties, ReactNode } from 'react';
import { cn } from './reportFormat';

export function ReportCard({
  className,
  children,
  style,
}: {
  className?: string;
  children: ReactNode;
  style?: CSSProperties;
}) {
  return (
    <div
      className={cn(
        'rounded-lg border border-border bg-card text-card-foreground shadow-sm',
        className,
      )}
      style={style}
    >
      {children}
    </div>
  );
}

export function ReportCardHeader({
  className,
  children,
}: {
  className?: string;
  children: ReactNode;
}) {
  return <div className={cn('flex flex-col gap-1.5 px-4 pt-4', className)}>{children}</div>;
}

export function ReportCardTitle({
  className,
  children,
}: {
  className?: string;
  children: ReactNode;
}) {
  return <h3 className={cn('text-base font-semibold leading-none', className)}>{children}</h3>;
}

export function ReportCardContent({
  className,
  children,
}: {
  className?: string;
  children: ReactNode;
}) {
  return <div className={cn('px-4 py-4', className)}>{children}</div>;
}

export function ReportBadge({
  children,
  className,
  style,
}: {
  children: ReactNode;
  className?: string;
  style?: CSSProperties;
}) {
  return (
    <span
      className={cn(
        'inline-flex items-center rounded-sm border border-border px-2 py-0.5 text-xs font-medium',
        className,
      )}
      style={style}
    >
      {children}
    </span>
  );
}
