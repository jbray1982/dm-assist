/**
 * HTTP client for `/api/rolls`. Hides transport (paths, fetch, error-body decoding) so nothing
 * above this module ever sees `fetch`, `Response`, or a status code — callers get typed results
 * or a thrown `DiceApiError`.
 */

export type RollSource = 'dm' | 'statBlock' | 'llm';
export type RollVisibility = 'shown' | 'hidden';

export interface DieRoll {
  sides: number;
  face: number;
  kept: boolean;
}

/** Mirrors the API's `RollResultDto`. `rolledAt` is an ISO-8601 string, not a `Date`. */
export interface RollResult {
  id: string;
  expression: string;
  dice: DieRoll[];
  total: number;
  source: RollSource;
  reason: string | null;
  visibility: RollVisibility;
  rolledAt: string;
}

/** Shape of the API's `application/problem+json` error extensions. */
export interface DiceError {
  code: string;
  message: string;
  position: number | null;
}

/** Thrown by every client function on a non-2xx response; carries the decoded error body. */
export class DiceApiError extends Error implements DiceError {
  readonly code: string;
  readonly position: number | null;

  constructor(code: string, message: string, position: number | null) {
    super(message);
    this.name = 'DiceApiError';
    this.code = code;
    this.position = position;
  }
}

/**
 * Rolls `expression` and appends it to the session log. `reason` is sent as-is; normalizing
 * empty/whitespace input to `null` is the caller's responsibility (see `useRoller.roll()`) —
 * this client is a dumb pipe, not a place for builder policy.
 *
 * @throws {DiceApiError} when the server rejects the expression (400).
 */
export async function roll(expression: string, reason?: string | null): Promise<RollResult> {
  const response = await fetch('/api/rolls', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ expression, reason: reason ?? null })
  });

  if (!response.ok) {
    interface ProblemDetailsError {
      extensions?: { code?: string; position?: number };
      detail?: string;
      title?: string;
    }
    const errorBody = await response.json() as ProblemDetailsError;
    throw new DiceApiError(
      errorBody.extensions?.code || 'unknown',
      errorBody.detail || errorBody.title || response.statusText,
      errorBody.extensions?.position ?? null
    );
  }

  return (await response.json()) as RollResult;
}

/** Returns the session's roll log, newest first, as currently retained server-side. */
export async function getLog(): Promise<RollResult[]> {
  const response = await fetch('/api/rolls', { method: 'GET' });
  if (!response.ok) throw new DiceApiError('unknown', response.statusText, null);
  return (await response.json()) as RollResult[];
}

/** Clears the session's roll log. */
export async function clearLog(): Promise<void> {
  const response = await fetch('/api/rolls', { method: 'DELETE' });
  if (!response.ok) throw new DiceApiError('unknown', response.statusText, null);
}
