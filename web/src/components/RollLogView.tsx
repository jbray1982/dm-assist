import type { ReactElement } from 'react';
import type { RollResult } from '../api/rollsClient';

export interface RollLogViewProps {
  entries: RollResult[];
  onClear: () => void;
}

export function RollLogView({ entries, onClear }: RollLogViewProps): ReactElement {
  return (
    <div>
      <div style={{ display: 'flex', justifyContent: 'space-between', marginBottom: 'var(--space-md)' }}>
        <h3>Log</h3>
        <button onClick={onClear}>Clear</button>
      </div>
      <div style={{ maxHeight: '300px', overflowY: 'auto', display: 'flex', flexDirection: 'column', gap: 'var(--space-sm)' }}>
        {entries.length === 0 ? (
          <div style={{ color: 'var(--ink-muted)' }}>No rolls yet</div>
        ) : (
          entries.map((r) => (
            <div key={r.id} style={{ fontSize: '0.9em', padding: 'var(--space-xs)', borderLeft: '3px solid var(--accent)' }}>
              <div style={{ fontFamily: 'monospace' }}>{r.expression}</div>
              {r.reason && <div style={{ color: 'var(--ink-muted)' }}>→ {r.reason}</div>}
              <div style={{ fontWeight: 'bold' }}>{r.total}</div>
            </div>
          ))
        )}
      </div>
    </div>
  );
}
