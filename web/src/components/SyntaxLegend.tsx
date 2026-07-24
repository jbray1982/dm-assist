import type { ReactElement } from 'react';

/** One row per grammar feature, matching the engine grammar in docs/dice-roller/001-mvp-spec.md. */
const ROWS: ReadonlyArray<readonly [string, string]> = [
  ['NdX', 'roll N dice with X sides (3d6). N defaults to 1: d20 = 1d20'],
  ['+ - * /', 'arithmetic; / is integer division, rounding toward zero'],
  ['khN / klN', 'keep the highest / lowest N dice (4d6kh3). N defaults to 1'],
  ['dhN / dlN', 'drop the highest / lowest N dice (5d10dl2)'],
  ['( )', 'grouping: (2d6+3)*2'],
  ['2d20kh1', 'advantage; 2d20kl1 is disadvantage (or use the toggle)'],
];

export function SyntaxLegend(): ReactElement {
  return (
    // Reference material a DM consults *while* composing an expression, so it sits open by
    // default and collapses once the notation is familiar.
    <details
      open
      style={{
        padding: 'var(--space-sm) var(--space-md)',
        background: 'var(--surface-raised)',
        border: '1px solid var(--border)',
        borderRadius: 'var(--radius-sm)',
        fontSize: '0.85em',
        color: 'var(--ink-muted)',
      }}
    >
      <summary style={{ fontFamily: 'var(--font-display)', color: 'var(--ink)', cursor: 'pointer' }}>
        Dice notation
      </summary>
      <dl
        style={{
          display: 'grid',
          gridTemplateColumns: 'auto 1fr',
          gap: 'var(--space-xs) var(--space-md)',
          margin: 'var(--space-sm) 0 0',
        }}
      >
        {ROWS.map(([code, meaning]) => (
          <div key={code} style={{ display: 'contents' }}>
            <dt style={{ fontFamily: 'monospace', color: 'var(--ink)', whiteSpace: 'nowrap' }}>{code}</dt>
            <dd style={{ margin: 0 }}>{meaning}</dd>
          </div>
        ))}
      </dl>
      <div style={{ marginTop: 'var(--space-sm)' }}>Limits: at most 1000 dice and 1000 sides per roll.</div>
    </details>
  );
}
