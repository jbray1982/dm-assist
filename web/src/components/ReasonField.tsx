import type { ReactElement } from 'react';

export interface ReasonFieldProps {
  reason: string;
  onChange: (value: string) => void;
}

/**
 * Optional free-text note attached to the next roll (design decision D3, reversed from the
 * original MVP scope — no reason input — by product-owner override on 2026-07-12). Purely a
 * view over `useRoller`'s `reason`/`setReason`; normalizing empty/whitespace input to `null`
 * happens in `useRoller.roll()`, not here, so this field always shows exactly what was typed.
 */
export function ReasonField({ reason, onChange }: ReasonFieldProps): ReactElement {
  return (
    <input
      type="text"
      value={reason}
      onChange={(e) => onChange(e.target.value)}
      placeholder="Why are you rolling?"
      style={{
        padding: 'var(--space-sm)',
        border: '1px solid var(--border)',
        color: 'var(--ink)',
        background: 'var(--surface)',
        width: '100%',
        boxSizing: 'border-box',
      }}
    />
  );
}
