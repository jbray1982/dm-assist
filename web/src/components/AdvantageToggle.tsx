import type { ReactElement } from 'react';
import type { AdvantageState } from '../dice/simpleExpression';

export interface AdvantageToggleProps {
  value: AdvantageState;
  onChange: (value: AdvantageState) => void;
  disabled?: boolean;
}

export function AdvantageToggle({ value, onChange, disabled }: AdvantageToggleProps): ReactElement {
  return (
    <div style={{ display: 'flex', gap: 'var(--space-xs)' }}>
      {(['normal', 'advantage', 'disadvantage'] as const).map((state) => (
        <button
          key={state}
          onClick={() => onChange(state)}
          disabled={disabled}
          style={{
            padding: 'var(--space-xs) var(--space-sm)',
            border: value === state ? '2px solid var(--accent)' : '1px solid var(--border)',
            background: value === state ? 'var(--accent)' : 'var(--surface)',
            color: value === state ? 'var(--surface)' : 'var(--ink)',
            cursor: disabled ? 'not-allowed' : 'pointer',
            opacity: disabled ? 0.5 : 1,
          }}
        >
          {state === 'normal' ? '↑↓' : state === 'advantage' ? '↑' : '↓'}
        </button>
      ))}
    </div>
  );
}
