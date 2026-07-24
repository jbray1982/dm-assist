import type { ReactElement } from 'react';

export interface NumberPadProps {
  onDigit: (digit: number) => void;
  onBackspace: () => void;
  onClear: () => void;
}

export function NumberPad({ onDigit, onBackspace, onClear }: NumberPadProps): ReactElement {
  // Keep focus in whichever builder field is being edited: a pad press must not blur it, or the
  // half-typed number would be discarded and the next digit would start over.
  const keepFocus = (e: React.MouseEvent) => e.preventDefault();

  return (
    <div style={{ display: 'grid', gridTemplateColumns: 'repeat(3, 1fr)', gap: 'var(--space-xs)' }}>
      {[1, 2, 3, 4, 5, 6, 7, 8, 9].map((n) => (
        <button key={n} onMouseDown={keepFocus} onClick={() => onDigit(n)} style={{ padding: 'var(--space-sm)' }}>
          {n}
        </button>
      ))}
      <button onMouseDown={keepFocus} onClick={() => onDigit(0)}>0</button>
      <button onMouseDown={keepFocus} onClick={onBackspace}>←</button>
      <button onMouseDown={keepFocus} onClick={onClear}>C</button>
    </div>
  );
}
