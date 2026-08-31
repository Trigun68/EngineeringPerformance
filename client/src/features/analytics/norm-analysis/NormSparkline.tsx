import { Tooltip } from '@mui/material';

interface Props {
  values: number[];
  color: string;
}

/** Мини-график дрейфа производительности по ревизиям той же группы IIC. */
export function NormSparkline({ values, color }: Props) {
  if (values.length < 2) return null;

  const w = 72;
  const h = 24;
  const min = Math.min(...values) * 0.95;
  const max = Math.max(...values) * 1.05;
  const range = max - min || 1;
  const last = values[values.length - 1];
  const points = values
    .map((v, i) => {
      const x = (i / (values.length - 1)) * w;
      const y = h - ((v - min) / range) * h;
      return `${x.toFixed(1)},${y.toFixed(1)}`;
    })
    .join(' ');

  return (
    <Tooltip title={`Производительность по ревизиям: ${values.map((v) => v.toFixed(2)).join(' → ')}`}>
      <svg width={w} height={h} role="img" aria-label="Динамика производительности нормы">
        <polyline
          points={points}
          fill="none"
          stroke={color}
          strokeWidth={1.75}
          strokeLinejoin="round"
          strokeLinecap="round"
        />
        <circle
          cx={w}
          cy={h - ((last - min) / range) * h}
          r={2.25}
          fill={color}
        />
      </svg>
    </Tooltip>
  );
}
