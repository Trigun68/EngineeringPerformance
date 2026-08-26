import { defineConfig, type Plugin } from 'vite';
import react from '@vitejs/plugin-react';
import { readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import path from 'path';
import { brotliCompressSync, constants as zlibConstants, gzipSync } from 'node:zlib';

const COMPRESS_EXTENSIONS = new Set(['.js', '.css', '.html', '.svg', '.json', '.txt', '.xml', '.map']);
const MIN_COMPRESS_BYTES = 1024;

function walkFiles(dir: string, acc: string[] = []): string[] {
  for (const name of readdirSync(dir)) {
    const full = path.join(dir, name);
    if (statSync(full).isDirectory()) {
      walkFiles(full, acc);
    } else {
      acc.push(full);
    }
  }
  return acc;
}

/** Рядом с ассетами пишет .gz / .br — nginx gzip_static отдаёт готовый gzip без CPU. */
function precompressPlugin(): Plugin {
  return {
    name: 'precompress-static',
    apply: 'build',
    closeBundle() {
      const dist = path.resolve(__dirname, 'dist');
      for (const file of walkFiles(dist)) {
        const ext = path.extname(file);
        if (!COMPRESS_EXTENSIONS.has(ext) || file.endsWith('.gz') || file.endsWith('.br')) {
          continue;
        }
        const buf = readFileSync(file);
        if (buf.length < MIN_COMPRESS_BYTES) {
          continue;
        }
        writeFileSync(`${file}.gz`, gzipSync(buf, { level: 5 }));
        writeFileSync(
          `${file}.br`,
          brotliCompressSync(buf, {
            params: { [zlibConstants.BROTLI_PARAM_QUALITY]: 5 },
          }),
        );
      }
    },
  };
}

export default defineConfig(({ mode }) => ({
  plugins: [react(), precompressPlugin()],
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
