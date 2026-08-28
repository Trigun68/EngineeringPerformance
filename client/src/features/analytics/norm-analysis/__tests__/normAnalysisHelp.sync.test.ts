import { readFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import clientHelp from '../normAnalysisHelp.md?raw';

const here = dirname(fileURLToPath(import.meta.url));

describe('norm analysis help markdown', () => {
  it('keeps _doc copy identical to the bundled client file', () => {
    const docs = readFileSync(resolve(here, '../../../../../../_doc/ui/norm-analysis-help.md'), 'utf8');
    expect(clientHelp).toBe(docs);
  });
});
