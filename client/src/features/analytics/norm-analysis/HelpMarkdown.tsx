import { Fragment, type ReactNode } from 'react';
import {
  Table,
  TableBody,
  TableCell,
  TableContainer,
  TableHead,
  TableRow,
  Typography,
} from '@mui/material';

function renderInline(text: string): ReactNode[] {
  const parts: ReactNode[] = [];
  const re = /\*\*([^*]+)\*\*/g;
  let last = 0;
  let match: RegExpExecArray | null;
  let key = 0;
  while ((match = re.exec(text)) !== null) {
    if (match.index > last) {
      parts.push(<Fragment key={key++}>{text.slice(last, match.index)}</Fragment>);
    }
    parts.push(
      <strong key={key++}>{match[1]}</strong>,
    );
    last = match.index + match[0].length;
  }
  if (last < text.length) {
    parts.push(<Fragment key={key++}>{text.slice(last)}</Fragment>);
  }
  return parts;
}

function isTableSeparator(line: string): boolean {
  return /^\|?\s*:?-{3,}:?\s*(\|\s*:?-{3,}:?\s*)+\|?$/.test(line.trim());
}

function splitRow(line: string): string[] {
  const trimmed = line.trim().replace(/^\|/, '').replace(/\|$/, '');
  return trimmed.split('|').map((c) => c.trim());
}

type Block =
  | { kind: 'h'; level: 1 | 2 | 3; text: string }
  | { kind: 'p'; text: string }
  | { kind: 'ul'; items: string[] }
  | { kind: 'table'; headers: string[]; rows: string[][] }
  | { kind: 'hr' };

function parseBlocks(source: string): Block[] {
  const lines = source.replace(/\r\n/g, '\n').split('\n');
  const blocks: Block[] = [];
  let i = 0;
  while (i < lines.length) {
    const line = lines[i];
    const trimmed = line.trim();
    if (trimmed === '') {
      i += 1;
      continue;
    }
    if (trimmed === '---') {
      blocks.push({ kind: 'hr' });
      i += 1;
      continue;
    }
    if (trimmed.startsWith('### ')) {
      blocks.push({ kind: 'h', level: 3, text: trimmed.slice(4) });
      i += 1;
      continue;
    }
    if (trimmed.startsWith('## ')) {
      blocks.push({ kind: 'h', level: 2, text: trimmed.slice(3) });
      i += 1;
      continue;
    }
    if (trimmed.startsWith('# ')) {
      blocks.push({ kind: 'h', level: 1, text: trimmed.slice(2) });
      i += 1;
      continue;
    }
    if (trimmed.startsWith('|') && i + 1 < lines.length && isTableSeparator(lines[i + 1])) {
      const headers = splitRow(trimmed);
      i += 2;
      const rows: string[][] = [];
      while (i < lines.length && lines[i].trim().startsWith('|')) {
        rows.push(splitRow(lines[i]));
        i += 1;
      }
      blocks.push({ kind: 'table', headers, rows });
      continue;
    }
    if (trimmed.startsWith('- ')) {
      const items: string[] = [];
      while (i < lines.length && lines[i].trim().startsWith('- ')) {
        items.push(lines[i].trim().slice(2));
        i += 1;
      }
      blocks.push({ kind: 'ul', items });
      continue;
    }
    const para: string[] = [trimmed];
    i += 1;
    while (
      i < lines.length &&
      lines[i].trim() !== '' &&
      !lines[i].trim().startsWith('#') &&
      !lines[i].trim().startsWith('|') &&
      !lines[i].trim().startsWith('- ') &&
      lines[i].trim() !== '---'
    ) {
      para.push(lines[i].trim());
      i += 1;
    }
    blocks.push({ kind: 'p', text: para.join(' ') });
  }
  return blocks;
}

const headingVariant = { 1: 'h5', 2: 'h6', 3: 'subtitle1' } as const;

/** Узкий рендер справки: заголовки, абзацы, списки, таблицы, **жирный**. */
export function HelpMarkdown({ source, omitH1 = false }: { source: string; omitH1?: boolean }) {
  const blocks = parseBlocks(source).filter((b) => !(omitH1 && b.kind === 'h' && b.level === 1));
  return (
    <>
      {blocks.map((block, index) => {
        if (block.kind === 'hr') {
          return <hr key={index} style={{ border: 0, borderTop: '1px solid currentColor', opacity: 0.2, margin: '16px 0' }} />;
        }
        if (block.kind === 'h') {
          return (
            <Typography
              key={index}
              variant={headingVariant[block.level]}
              component={block.level === 1 ? 'h2' : block.level === 2 ? 'h3' : 'h4'}
              sx={{ mt: block.level === 1 ? 0 : 2.5, mb: 1, fontWeight: 600 }}
            >
              {renderInline(block.text)}
            </Typography>
          );
        }
        if (block.kind === 'p') {
          return (
            <Typography key={index} variant="body2" sx={{ mb: 1.25, lineHeight: 1.55 }}>
              {renderInline(block.text)}
            </Typography>
          );
        }
        if (block.kind === 'ul') {
          return <BoxList key={index} items={block.items} />;
        }
        return (
          <TableContainer key={index} sx={{ mb: 2, overflowX: 'auto' }}>
            <Table size="small" sx={{ '& th, & td': { verticalAlign: 'top' } }}>
              <TableHead>
                <TableRow>
                  {block.headers.map((h) => (
                    <TableCell key={h} sx={{ fontWeight: 600 }}>
                      {renderInline(h)}
                    </TableCell>
                  ))}
                </TableRow>
              </TableHead>
              <TableBody>
                {block.rows.map((row, ri) => (
                  <TableRow key={ri}>
                    {row.map((cell, ci) => (
                      <TableCell key={ci}>{renderInline(cell)}</TableCell>
                    ))}
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </TableContainer>
        );
      })}
    </>
  );
}

function BoxList({ items }: { items: string[] }) {
  return (
    <ul style={{ margin: '0 0 12px', paddingLeft: 22 }}>
      {items.map((item, i) => (
        <li key={i}>
          <Typography variant="body2" component="span" sx={{ lineHeight: 1.55 }}>
            {renderInline(item)}
          </Typography>
        </li>
      ))}
    </ul>
  );
}
