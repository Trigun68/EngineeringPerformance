import { act, renderHook } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { useDelayedBusy } from '../useDelayedBusy';

describe('useDelayedBusy', () => {
  beforeEach(() => {
    vi.useFakeTimers();
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it('stays false until delay elapses while busy', () => {
    const { result, rerender } = renderHook(
      ({ busy }) => useDelayedBusy(busy, 1000),
      { initialProps: { busy: true } },
    );

    expect(result.current).toBe(false);
    act(() => {
      vi.advanceTimersByTime(999);
    });
    expect(result.current).toBe(false);
    act(() => {
      vi.advanceTimersByTime(1);
    });
    expect(result.current).toBe(true);

    rerender({ busy: false });
    expect(result.current).toBe(false);
  });

  it('does not show if busy ends before delay', () => {
    const { result, rerender } = renderHook(
      ({ busy }) => useDelayedBusy(busy, 1000),
      { initialProps: { busy: true } },
    );

    act(() => {
      vi.advanceTimersByTime(500);
    });
    rerender({ busy: false });
    act(() => {
      vi.advanceTimersByTime(1000);
    });
    expect(result.current).toBe(false);
  });

  it('shows immediately when delayMs is 0', () => {
    const { result } = renderHook(() => useDelayedBusy(true, 0));
    expect(result.current).toBe(true);
  });
});
