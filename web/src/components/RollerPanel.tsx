import type { ReactElement } from 'react';
import { useState } from 'react';
import type { UseRollerResult } from '../state/useRoller';
import { ExpressionField } from './ExpressionField';
import { ReasonField } from './ReasonField';
import { AdvantageToggle } from './AdvantageToggle';
import { NumberPad } from './NumberPad';
import { RollResultView } from './RollResultView';
import { RollLogView } from './RollLogView';

export interface RollerPanelProps {
  roller: UseRollerResult;
}

export function RollerPanel({ roller }: RollerPanelProps): ReactElement {
  const [focusedField, setFocusedField] = useState<'count' | 'sides' | 'modifier'>('count');

  const handleDigit = (digit: number) => {
    if (roller.isAdvanced) return;
    const current = roller.simple || { count: 1, sides: 6, modifier: 0, advantage: 'normal' as const };
    const digitStr = digit.toString();
    if (focusedField === 'count') {
      roller.updateSimple('count', Math.min(1000, parseInt((current.count.toString() + digitStr).slice(-4)) || 1));
    } else if (focusedField === 'sides') {
      roller.updateSimple('sides', Math.min(1000, parseInt((current.sides.toString() + digitStr).slice(-4)) || 1));
    } else {
      roller.updateSimple('modifier', parseInt((current.modifier.toString() + digitStr).slice(-4)) || 0);
    }
  };

  const handleBackspace = () => {
    if (roller.isAdvanced) return;
    const current = roller.simple || { count: 1, sides: 6, modifier: 0, advantage: 'normal' as const };
    if (focusedField === 'count') {
      roller.updateSimple('count', Math.max(1, Math.floor(current.count / 10)));
    } else if (focusedField === 'sides') {
      roller.updateSimple('sides', Math.max(1, Math.floor(current.sides / 10)));
    } else {
      roller.updateSimple('modifier', Math.floor(current.modifier / 10));
    }
  };

  return (
    <div style={{ display: 'flex', gap: 'var(--space-md)', flexDirection: 'column', padding: 'var(--space-lg)' }}>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 'var(--space-md)' }}>
        <div>
          <ExpressionField expression={roller.expression} onChange={roller.setExpression} error={roller.lastError} />
          <div style={{ marginTop: 'var(--space-md)' }}>
            <ReasonField reason={roller.reason} onChange={roller.setReason} />
          </div>
          {roller.simple && (
            <div style={{ marginTop: 'var(--space-md)' }}>
              <AdvantageToggle value={roller.simple.advantage} onChange={(v) => roller.updateSimple('advantage', v)} disabled={!roller.advantageEnabled} />
            </div>
          )}
          <button onClick={() => void roller.roll()} style={{ marginTop: 'var(--space-md)', width: '100%', padding: 'var(--space-md)', background: 'var(--accent)', color: 'var(--surface)', border: 'none', cursor: 'pointer' }}>
            Roll
          </button>
          {roller.lastResult && (
            <div style={{ marginTop: 'var(--space-md)' }}>
              <RollResultView result={roller.lastResult} />
            </div>
          )}
          <div style={{ marginTop: 'var(--space-md)' }}>
            <NumberPad onDigit={handleDigit} onBackspace={handleBackspace} onClear={() => roller.setExpression('')} />
          </div>
          <div style={{ marginTop: 'var(--space-md)', fontSize: '0.85em', color: 'var(--ink-muted)' }}>
            Focused: {focusedField} (click number pad)
            <button onClick={() => setFocusedField('count')} style={{ marginLeft: 'var(--space-xs)' }}>
              Count
            </button>
            <button onClick={() => setFocusedField('sides')} style={{ marginLeft: 'var(--space-xs)' }}>
              Sides
            </button>
            <button onClick={() => setFocusedField('modifier')} style={{ marginLeft: 'var(--space-xs)' }}>
              Mod
            </button>
          </div>
        </div>
        <div>
          <RollLogView entries={roller.log} onClear={() => void roller.clearLog()} />
        </div>
      </div>
    </div>
  );
}
