import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';
import path from 'path';

export default defineConfig(({ mode }) => ({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, 'src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5080',
        changeOrigin: true,
        secure: false,
      },
    },
  },
  build: {
    outDir: 'dist',
    sourcemap: mode !== 'production',
    rollupOptions: {
      output: {
        // Тяжёлые библиотеки — отдельными чанками: кэшируются браузером между релизами
        // и не тянутся целиком в стартовый бандл (ISS-018).
        manualChunks: {
          highcharts: ['highcharts', 'highcharts-react-official'],
          aggrid: ['ag-grid-community', 'ag-grid-react'],
          mui: ['@mui/material', '@mui/icons-material'],
        },
      },
    },
    chunkSizeWarningLimit: 1200,
  },
}));
