/** @type {import('tailwindcss').Config} */
export default {
  darkMode: ['class'],
  content: ['./src/features/projects/report/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        background: 'var(--ep-report-background)',
        foreground: 'var(--ep-report-foreground)',
        card: 'var(--ep-report-card)',
        'card-foreground': 'var(--ep-report-card-foreground)',
        primary: 'var(--ep-report-primary)',
        'primary-foreground': 'var(--ep-report-primary-foreground)',
        muted: 'var(--ep-report-muted)',
        'muted-foreground': 'var(--ep-report-muted-foreground)',
        border: 'var(--ep-report-border)',
        accent: 'var(--ep-report-accent)',
        'perf-excellent': 'var(--ep-report-perf-excellent)',
        'perf-good': 'var(--ep-report-perf-good)',
        'perf-satisfactory': 'var(--ep-report-perf-satisfactory)',
        'perf-attention': 'var(--ep-report-perf-attention)',
        'perf-critical': 'var(--ep-report-perf-critical)',
      },
      borderRadius: {
        lg: '0.625rem',
      },
    },
  },
  corePlugins: {
    // Не сбрасывать глобальные стили MUI-приложения.
    preflight: false,
  },
  plugins: [],
};
