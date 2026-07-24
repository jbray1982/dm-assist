import type { ReactElement } from 'react';
import type { AdvantageState } from '../dice/simpleExpression';
import { useToast } from '../state/toastContext';

export interface AdvantageToggleProps {
  value: AdvantageState;
  onChange: (value: AdvantageState) => void;
  /**
   * Why the toggle is unavailable, or null when it is usable. Disabling is expressed as a reason
   * rather than a boolean on purpose: advantage greys out under rules the DM cannot see (the
   * count must be 1; the expression must be builder-representable), and a bare `disabled` leaves
   * them a dead control with nothing to explain it.
   */
  disabledReason?: string | null;
}

export function AdvantageToggle({ value, onChange, disabledReason }: AdvantageToggleProps): ReactElement {
  const disabled = Boolean(disabledReason);
  const toast = useToast();

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-xs)' }}>
      <div style={{ display: 'flex', gap: 'var(--space-xs)' }} title={disabledReason ?? undefined}>
        {(['normal', 'advantage', 'disadvantage'] as const).map((state) => (
          <button
            key={state}
            // `aria-disabled`, not `disabled`: a disabled button dispatches no events, so it
            // could neither answer a click nor show a tooltip. This one still refuses to act,
            // but it can say why.
            aria-disabled={disabled}
            onClick={() => (disabled ? toast.show(disabledReason!) : onChange(state))}
            aria-label={state}
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
      {disabledReason && (
        <span style={{ fontSize: '0.85em', color: 'var(--ink-muted)' }}>{disabledReason}</span>
      )}
    </div>
  );
}
