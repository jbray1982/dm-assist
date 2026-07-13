import type { ReactElement } from 'react';
import type { DiceError } from '../api/rollsClient';

export interface ExpressionFieldProps {
  expression: string;
  onChange: (value: string) => void;
  /** Renders a message under the field; `error.position` locates a caret/highlight when set. */
  error: DiceError | null;
}

/**
 * Direct typed entry into the single source of truth. Always live, even when the builder is
 * disabled in the advanced-expression state — this field is what drives that state.
 */
export function ExpressionField({ expression, onChange, error }: ExpressionFieldProps): ReactElement {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-sm)' }}>
      <input
        type="text"
        value={expression}
        onChange={(e) => onChange(e.target.value)}
        placeholder="e.g., 4d6kh3"
        style={{
          padding: 'var(--space-sm)',
          border: error ? '1px solid var(--error)' : '1px solid var(--border)',
          color: 'var(--ink)',
          background: 'var(--surface)',
          fontFamily: 'monospace',
        }}
      />
      {error && (
        <div style={{ color: 'var(--error)', fontSize: '0.9em' }}>
          <div>{error.message}</div>
          {error.position !== null && (
            <div style={{ fontFamily: 'monospace', marginTop: '0.25em' }}>
              {' '.repeat(error.position)}^
            </div>
          )}
        </div>
      )}
    </div>
  );
}
