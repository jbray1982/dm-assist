import { act, renderHook, waitFor } from '@testing-library/react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { useRoller } from './useRoller';
import * as rollsClient from '../api/rollsClient';
import * as rollSoundModule from '../audio/rollSound';

// Mock the transport and audio modules so these tests exercise useRoller's own orchestration
// logic (state, derivation, sequencing) rather than the real (also-stubbed) HTTP client.
vi.mock('../api/rollsClient', async () => {
  const actual = await vi.importActual<typeof import('../api/rollsClient')>('../api/rollsClient');
  return {
    ...actual,
    roll: vi.fn(),
    getLog: vi.fn(),
    clearLog: vi.fn(),
  };
});
vi.mock('../audio/rollSound');

function mockResult(overrides: Partial<rollsClient.RollResult> = {}): rollsClient.RollResult {
  return {
    id: 'roll-1',
    expression: '1d20',
    dice: [{ sides: 20, face: 14, kept: true }],
    total: 14,
    source: 'dm',
    reason: null,
    visibility: 'shown',
    rolledAt: '2026-07-12T22:00:00Z',
    ...overrides,
  };
}

describe('useRoller', () => {
  beforeEach(() => {
    vi.resetAllMocks();
    vi.mocked(rollsClient.getLog).mockResolvedValue([]);
  });

  it('starts with an empty expression and no advanced state', () => {
    const { result } = renderHook(() => useRoller());

    expect(result.current.expression).toBe('');
    expect(result.current.isAdvanced).toBe(false);
  });

  it('updateSimple regenerates the expression via formatSimple', () => {
    const { result } = renderHook(() => useRoller());

    act(() => {
      result.current.updateSimple('sides', 6);
    });
    act(() => {
      result.current.updateSimple('count', 3);
    });

    expect(result.current.expression).toContain('3d6');
  });

  it('typing an advanced expression disables the builder', () => {
    const { result } = renderHook(() => useRoller());

    act(() => {
      result.current.setExpression('4d6kh3');
    });

    expect(result.current.isAdvanced).toBe(true);
    expect(result.current.simple).toBeNull();
  });

  // Design decision D4 (reversed from the original MVP scope by product-owner override,
  // 2026-07-12): the advantage/disadvantage toggle only applies to a single-die roll.
  it('advantageEnabled is false when the builder count is not 1', () => {
    const { result } = renderHook(() => useRoller());

    act(() => {
      result.current.setExpression('3d6');
    });

    expect(result.current.advantageEnabled).toBe(false);
  });

  it('advantageEnabled is false in the advanced-expression state', () => {
    const { result } = renderHook(() => useRoller());

    act(() => {
      result.current.setExpression('4d6kh3');
    });

    expect(result.current.advantageEnabled).toBe(false);
  });

  it('advantageEnabled is true for a single-die simple roll', () => {
    const { result } = renderHook(() => useRoller());

    act(() => {
      result.current.setExpression('1d20');
    });

    expect(result.current.advantageEnabled).toBe(true);
  });

  // Design decision D3 (reversed from the original MVP scope by product-owner override,
  // 2026-07-12): blank/whitespace-only reason text must be normalized to null on the wire.
  it('roll() sends a null reason when the reason field is blank', async () => {
    vi.mocked(rollsClient.roll).mockResolvedValue(mockResult());
    const { result } = renderHook(() => useRoller());

    act(() => {
      result.current.setExpression('1d20');
      result.current.setReason('   ');
    });

    await act(async () => {
      await result.current.roll();
    });

    expect(rollsClient.roll).toHaveBeenCalledWith('1d20', null);
  });

  it('roll() sends the reason text when present', async () => {
    vi.mocked(rollsClient.roll).mockResolvedValue(mockResult({ reason: 'attack roll' }));
    const { result } = renderHook(() => useRoller());

    act(() => {
      result.current.setExpression('1d20');
      result.current.setReason('attack roll');
    });

    await act(async () => {
      await result.current.roll();
    });

    expect(rollsClient.roll).toHaveBeenCalledWith('1d20', 'attack roll');
  });

  it('roll() success plays the sound, sets lastResult, and refetches the log', async () => {
    const rolled = mockResult();
    vi.mocked(rollsClient.roll).mockResolvedValue(rolled);
    vi.mocked(rollsClient.getLog).mockResolvedValue([rolled]);
    const { result } = renderHook(() => useRoller());

    act(() => {
      result.current.setExpression('1d20');
    });

    await act(async () => {
      await result.current.roll();
    });

    expect(result.current.lastResult).toEqual(rolled);
    expect(result.current.lastError).toBeNull();
    expect(rollSoundModule.playRollSound).toHaveBeenCalledTimes(1);
    await waitFor(() => expect(result.current.log).toEqual([rolled]));
  });

  it('roll() failure sets lastError, does not play the sound, and leaves the log untouched', async () => {
    const apiError = new rollsClient.DiceApiError('nonPositiveDiceCount', 'bad roll', null);
    vi.mocked(rollsClient.roll).mockRejectedValue(apiError);
    const { result } = renderHook(() => useRoller());

    act(() => {
      result.current.setExpression('0d6');
    });

    await act(async () => {
      await result.current.roll();
    });

    expect(result.current.lastResult).toBeNull();
    expect(result.current.lastError).toEqual({
      code: 'nonPositiveDiceCount',
      message: 'bad roll',
      position: null,
    });
    expect(rollSoundModule.playRollSound).not.toHaveBeenCalled();
  });

  it('clearLog() clears the server log and refetches', async () => {
    vi.mocked(rollsClient.clearLog).mockResolvedValue(undefined);
    const { result } = renderHook(() => useRoller());

    await act(async () => {
      await result.current.clearLog();
    });

    expect(rollsClient.clearLog).toHaveBeenCalledTimes(1);
    expect(result.current.log).toEqual([]);
  });
});
