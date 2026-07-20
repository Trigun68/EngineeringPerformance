import { describe, expect, it } from 'vitest';
import { parseWindowsAccount } from '../parseWindowsAccount';

describe('parseWindowsAccount', () => {
  it('returns null for empty', () => {
    expect(parseWindowsAccount(null)).toBeNull();
    expect(parseWindowsAccount('')).toBeNull();
    expect(parseWindowsAccount('   ')).toBeNull();
  });

  it('splits DOMAIN\\sam', () => {
    expect(parseWindowsAccount('CORP\\ivanov')).toEqual({
      domain: 'CORP',
      sam: 'ivanov',
      raw: 'CORP\\ivanov',
    });
  });

  it('keeps bare SAM without domain', () => {
    expect(parseWindowsAccount('ivanov')).toEqual({
      domain: '',
      sam: 'ivanov',
      raw: 'ivanov',
    });
  });

  it('uses last backslash for nested-looking values', () => {
    expect(parseWindowsAccount('A\\B\\c')).toEqual({
      domain: 'A\\B',
      sam: 'c',
      raw: 'A\\B\\c',
    });
  });
});
