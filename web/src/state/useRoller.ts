import { useMemo, useState, useEffect } from 'react';
import * as rollsClient from '../api/rollsClient';
import { parseSimple, formatSimple, type SimpleRoll } from '../dice/simpleExpression';
import { playRollSound } from '../audio/rollSound';
import type { DiceError, RollResult } from '../api/rollsClient';

/**
 * Everything a view needs to render the roller. Components are views over this hook; there is
 * no builder-local state anywhere else, which is what makes the "two views of one thing"
 * property (expression field vs. builder fields) structural rather than something each
 * component has to maintain by convention.
 */
export interface UseRollerResult {
  /** The single source of truth. Everything else below is derived from this. */
  expression: string;
  setExpression: (value: string) => void;

  /**
   * `parseSimple(expression)`, memoized. Non-null when the expression fits a builder-
   * representable shape; the builder fields render from this. Null (with a non-empty
   * expression) means "advanced expression" — the builder greys out but the expression field,
   * and rolling, keep working.
   */
  simple: SimpleRoll | null;

  /**
   * Applies one builder field edit (count, sides, modifier, or advantage) and regenerates
   * `expression` via `formatSimple`. This is the only way builder fields change the expression —
   * there is no separate builder state to drift out of sync.
   */
  updateSimple: <K extends keyof SimpleRoll>(field: K, value: SimpleRoll[K]) => void;

  /** True when `expression` is non-empty but not builder-representable (`simple` is null). */
  isAdvanced: boolean;

  /**
   * Whether the advantage/disadvantage toggle should be enabled. Advantage/disadvantage only has
   * a defined meaning over a single die, so this is false whenever `simple` is null (advanced
   * state) or `simple.count !== 1` — matching the other builder controls' disabled behavior
   * rather than introducing a third state.
   */
  advantageEnabled: boolean;

  /**
   * Free-text note for the next roll (design decision D3, reversed from the original MVP scope
   * by product-owner override 2026-07-12). This is raw textbox state, not expression-derived —
   * unlike the builder fields, it has no representation in `expression`. Empty/whitespace-only
   * text is normalized to `null` inside `roll()`, not here, so the field can keep showing
   * exactly what the DM typed (including trailing spaces mid-edit).
   */
  reason: string;
  setReason: (value: string) => void;

  /**
   * Posts the current `expression` (and normalized `reason`) to the API. On success: plays the
   * roll sound, sets `lastResult`, clears `lastError`, and refetches the log. On a 400: sets
   * `lastError` and leaves `lastResult`/`log` untouched — the server logs nothing on error, so
   * there is nothing to refetch.
   */
  roll: () => Promise<void>;

  lastResult: RollResult | null;
  lastError: DiceError | null;

  /**
   * The session log as last fetched from the server. This hook never trims to a cap or
   * reorders locally — the server owns that policy; the client always treats `GET /api/rolls`
   * as truth and refetches after every `roll()` and `clearLog()`.
   */
  log: RollResult[];
  clearLog: () => Promise<void>;
}

export function useRoller(): UseRollerResult {
  const [expression, setExpression] = useState('');
  const [reason, setReason] = useState('');
  const [lastResult, setLastResult] = useState<RollResult | null>(null);
  const [lastError, setLastError] = useState<DiceError | null>(null);
  const [log, setLog] = useState<RollResult[]>([]);

  const simple = useMemo(() => parseSimple(expression), [expression]);
  const isAdvanced = expression !== '' && simple === null;
  const advantageEnabled = simple !== null && simple.count === 1;

  const refetchLog = async () => {
    try {
      const newLog = await rollsClient.getLog();
      setLog(newLog);
    } catch {
      // Silently ignore log fetch errors
    }
  };

  // Fetch log on mount
  useEffect(() => {
    const fetchInitialLog = async () => {
      await refetchLog();
    };
    void fetchInitialLog();
  }, []);

  const updateSimple = <K extends keyof SimpleRoll>(field: K, value: SimpleRoll[K]) => {
    const current = simple || { count: 1, sides: 6, modifier: 0, advantage: 'normal' as const };
    const updated = { ...current, [field]: value };
    setExpression(formatSimple(updated));
  };

  const roll = async () => {
    try {
      const normalizedReason = reason.trim() === '' ? null : reason;
      const result = await rollsClient.roll(expression, normalizedReason);
      playRollSound();
      setLastResult(result);
      setLastError(null);
      await refetchLog();
    } catch (error) {
      if (error instanceof rollsClient.DiceApiError) {
        setLastError({
          code: error.code,
          message: error.message,
          position: error.position,
        });
      }
    }
  };

  const clearLog = async () => {
    await rollsClient.clearLog();
    await refetchLog();
  };

  return {
    expression,
    setExpression,
    simple,
    updateSimple,
    isAdvanced,
    advantageEnabled,
    reason,
    setReason,
    roll,
    lastResult,
    lastError,
    log,
    clearLog,
  };
}
