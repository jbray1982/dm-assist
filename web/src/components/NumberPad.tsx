import type { ReactElement } from 'react';

export interface NumberPadProps {
  onDigit: (digit: number) => void;
  onBackspace: () => void;
  onClear: () => void;
}

export function NumberPad({ onDigit, onBackspace, onClear }: NumberPadProps): ReactElement {
  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 'var(--space-xs)' }}>
      {[1, 2, 3, 4, 5, 6, 7, 8, 9].map((n) => (
        <button key={n} onClick={() => onDigit(n)} style={{ padding: 'var(--space-sm)' }}>
          {n}
        </button>
      ))}
      <button onClick={() => onDigit(0)}>0</button>
      <button onClick={onBackspace}>←</button>
      <button onClick={onClear}>C</button>
    </div>
  );
}
