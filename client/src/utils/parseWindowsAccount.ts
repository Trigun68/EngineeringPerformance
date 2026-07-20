export type ParsedWindowsAccount = {
  domain: string;
  sam: string;
  raw: string;
};

/** Split `DOMAIN\user` (or bare SAM) for diagnostic UI. */
export function parseWindowsAccount(windowsAccount: string | null | undefined): ParsedWindowsAccount | null {
  const raw = (windowsAccount || '').trim();
  if (!raw) return null;
  const sep = raw.lastIndexOf('\\');
  if (sep <= 0 || sep === raw.length - 1) {
    return { domain: '', sam: raw, raw };
  }
  return {
    domain: raw.slice(0, sep).trim(),
    sam: raw.slice(sep + 1).trim(),
    raw,
  };
}
