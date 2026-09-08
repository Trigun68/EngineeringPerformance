/**
 * Проверка gzip-бюджета JS-чанков после vite build.
 * Лимиты фиксированы: любой чанк > 900 КБ gzip или сумма JS > 2.5 МБ gzip → exit 1.
 */
import { existsSync, readdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { gzipSync } from 'node:zlib';

const MAX_CHUNK_BYTES = 900 * 1024;
const MAX_TOTAL_BYTES = 2.5 * 1024 * 1024;

const clientRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const distDir = join(clientRoot, 'dist');
const assetsDir = join(distDir, 'assets');

function formatKb(bytes) {
  return `${(bytes / 1024).toFixed(1)} KB`;
}

if (!existsSync(distDir) || !existsSync(assetsDir)) {
  console.error('Bundle budget: dist/ not found. Run vite build first.');
  process.exit(1);
}

const files = readdirSync(assetsDir)
  .filter((name) => name.endsWith('.js'))
  .sort();

if (files.length === 0) {
  console.error('Bundle budget: no JS chunks in dist/assets/. Run vite build first.');
  process.exit(1);
}

const rows = files.map((name) => {
  const gzipBytes = gzipSync(readFileSync(join(assetsDir, name))).length;
  return { name, gzipBytes };
});

const totalGzip = rows.reduce((sum, row) => sum + row.gzipBytes, 0);
const largest = Math.max(...rows.map((row) => row.gzipBytes));
const offenders = rows
  .filter((row) => row.gzipBytes > MAX_CHUNK_BYTES)
  .sort((a, b) => b.gzipBytes - a.gzipBytes);
const totalExceeded = totalGzip > MAX_TOTAL_BYTES;

if (offenders.length > 0 || totalExceeded) {
  console.error('Bundle budget FAILED.');
  if (offenders.length > 0) {
    console.error(`Chunks over ${formatKb(MAX_CHUNK_BYTES)} gzip:`);
    for (const row of offenders) {
      console.error(`  ${row.name}: ${formatKb(row.gzipBytes)} gzip`);
    }
  }
  if (totalExceeded) {
    console.error(
      `Total gzip JS ${formatKb(totalGzip)} exceeds ${formatKb(MAX_TOTAL_BYTES)} (${rows.length} chunks).`,
    );
  }
  process.exit(1);
}

console.log(
  `Bundle budget OK: ${rows.length} JS chunks, largest ${formatKb(largest)} gzip, total ${formatKb(totalGzip)} gzip (limits: 900 KB / chunk, 2.5 MB total).`,
);
