import type { ReactElement } from 'react';
import type { RollResult } from '../api/rollsClient';

export interface RollResultViewProps {
  result: RollResult;
}

export function RollResultView({ result }: RollResultViewProps): ReactElement {
  return (
    <div style={{ padding: 'var(--space-md)', border: '1px solid var(--border)', borderRadius: 'var(--radius-md)' }}>
      <div style={{ fontFamily: 'monospace', marginBottom: 'var(--space-sm)' }}>{result.expression}</div>
      <div style={{ display: 'flex', gap: 'var(--space-xs)', flexWrap: 'wrap', marginBottom: 'var(--space-sm)' }}>
        {result.dice.map((d, i) => (
          <span
            key={i}
            style={{
              padding: 'var(--space-xs)',
              border: '1px solid var(--border)',
              textDecoration: d.kept ? 'none' : 'line-through',
              opacity: d.kept ? 1 : 0.5,
            }}
          >
            {d.face}
          </span>
        ))}
      </div>
      <div style={{ fontSize: '1.2em', fontWeight: 'bold', color: 'var(--accent)' }}>= {result.total}</div>
    </div>
  );
}
