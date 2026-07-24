import type { ReactElement } from 'react';
import { useState } from 'react';
import type { UseRollerResult } from '../state/useRoller';
import { ExpressionField } from './ExpressionField';
import { ReasonField } from './ReasonField';
import { AdvantageToggle } from './AdvantageToggle';
import { NumberPad } from './NumberPad';
import { RollResultView } from './RollResultView';
import { RollLogView } from './RollLogView';
import { DiceBuilder, type BuilderField } from './DiceBuilder';
import { SyntaxLegend } from './SyntaxLegend';
import { DEFAULT_SIMPLE, MAX_COUNT, MAX_SIDES, MAX_MODIFIER } from '../dice/simpleExpression';
import { useToast } from '../state/toastContext';

export interface RollerPanelProps {
  roller: UseRollerResult;
}

export function RollerPanel({ roller }: RollerPanelProps): ReactElement {
  const [focusedField, setFocusedField] = useState<BuilderField>('count');
  /**
   * Text mid-entry in `focusedField`, or null when that field shows its committed value. This is
   * NOT a second copy of the roll: a field being typed into passes through states no expression
   * can hold (`""` while cleared, `"0"` before the next digit), and the buffer exists only to
   * carry those keystrokes until they parse. The expression stays the source of truth — every
   * parseable keystroke commits to it immediately. Both the keyboard and the number pad write
   * through here, so neither can start appending digits to a value the DM never chose.
   */
  const [draft, setDraft] = useState<string | null>(null);

  const current = roller.simple ?? DEFAULT_SIMPLE;
  const toast = useToast();
  const fieldValue = (field: BuilderField) =>
    field === 'modifier' ? Math.abs(current.modifier) : current[field];

  // Every control the builder refuses states its own rule, so a DM who pokes at a dead one gets
  // an answer instead of silence.
  const ADVANCED_REASON = 'This expression is beyond the builder — edit it in the expression field below.';

  const builderDisabledReason = roller.isAdvanced ? ADVANCED_REASON : null;

  // Advantage rolls one die twice, so it has no meaning over a pool.
  const advantageDisabledReason = roller.advantageEnabled
    ? null
    : roller.isAdvanced
      ? ADVANCED_REASON
      : `Advantage rolls a single die twice, so it needs a count of 1 (this rolls ${current.count}).`;

  // The same rule seen from the other side: with advantage on, `formatSimple` emits the two-dice
  // selector form and drops the count. A live-looking count field would take a number and throw
  // it away, so it locks — and every path that writes to a field has to honor that, not just the
  // field itself, or the number pad becomes a back door into the value the lock is protecting.
  const countLockedReason =
    !roller.isAdvanced && current.advantage !== 'normal'
      ? `${current.advantage === 'advantage' ? 'Advantage' : 'Disadvantage'} rolls a single die twice, so the count is fixed at 1. Set the toggle to ↑↓ to roll a pool.`
      : null;

  const lockedReason = (field: BuilderField) => (field === 'count' ? countLockedReason : null);

  /** Commits `text` to the expression. Empty (or unparseable) text stays in the draft. */
  const commit = (field: BuilderField, text: string) => {
    const value = parseInt(text, 10);
    if (Number.isNaN(value)) return;
    if (field === 'modifier') {
      // The sign is the toggle's business; the field only carries magnitude.
      const sign = current.modifier < 0 ? -1 : 1;
      roller.updateSimple('modifier', sign * Math.min(value, MAX_MODIFIER));
    } else {
      const max = field === 'count' ? MAX_COUNT : MAX_SIDES;
      roller.updateSimple(field, Math.min(Math.max(value, 1), max));
    }
  };

  /** True (having said why) when `field` cannot take an edit right now. */
  const refuse = (field: BuilderField) => {
    const reason = builderDisabledReason ?? lockedReason(field);
    if (reason === null) return false;
    toast.show(reason);
    return true;
  };

  const handleFocusField = (field: BuilderField) => {
    setFocusedField(field);
    setDraft(null);
  };

  const handleEditField = (field: BuilderField, text: string) => {
    if (refuse(field)) return;
    const digits = text.replace(/\D/g, '').slice(0, 4);
    setFocusedField(field);
    setDraft(digits);
    commit(field, digits);
  };

  // A digit continues the number being typed, or starts a new one when the field hasn't been
  // touched since it gained focus — matching the select-on-focus behavior of the fields.
  const handleDigit = (digit: number) => {
    if (refuse(focusedField)) return;
    const next = ((draft ?? '') + digit).slice(0, 4);
    setDraft(next);
    commit(focusedField, next);
  };

  const handleBackspace = () => {
    if (refuse(focusedField)) return;
    const next = (draft ?? String(fieldValue(focusedField))).slice(0, -1);
    setDraft(next);
    commit(focusedField, next);
  };

  const handleClear = () => {
    roller.setExpression('');
    setDraft(null);
  };

  const handleToggleSign = () => {
    if (roller.isAdvanced || current.modifier === 0) return;
    roller.updateSimple('modifier', -current.modifier);
  };

  return (
    <div style={{ display: 'flex', gap: 'var(--space-md)', flexDirection: 'column', padding: 'var(--space-lg)' }}>
      <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 'var(--space-md)' }}>
        <div>
          <DiceBuilder
            simple={roller.simple}
            disabledReason={builderDisabledReason}
            fieldReasons={countLockedReason ? { count: countLockedReason } : undefined}
            focusedField={focusedField}
            draft={draft}
            onFocusField={handleFocusField}
            onEditField={handleEditField}
            onBlurField={() => setDraft(null)}
            onToggleSign={handleToggleSign}
          />
          <div style={{ marginTop: 'var(--space-md)' }}>
            <ExpressionField expression={roller.expression} onChange={roller.setExpression} error={roller.lastError} />
          </div>
          <div style={{ marginTop: 'var(--space-md)' }}>
            <ReasonField reason={roller.reason} onChange={roller.setReason} />
          </div>
          <div style={{ marginTop: 'var(--space-md)' }}>
            <AdvantageToggle value={current.advantage} onChange={(v) => roller.updateSimple('advantage', v)} disabledReason={advantageDisabledReason} />
          </div>
          <button onClick={() => void roller.roll()} style={{ marginTop: 'var(--space-md)', width: '100%', padding: 'var(--space-md)', background: 'var(--accent)', color: 'var(--surface)', border: 'none', cursor: 'pointer' }}>
            Roll
          </button>
          {roller.lastResult && (
            <div style={{ marginTop: 'var(--space-md)' }}>
              <RollResultView result={roller.lastResult} />
            </div>
          )}
          <div style={{ marginTop: 'var(--space-md)' }}>
            <NumberPad onDigit={handleDigit} onBackspace={handleBackspace} onClear={handleClear} />
          </div>
        </div>
        <div style={{ display: 'flex', flexDirection: 'column', gap: 'var(--space-md)' }}>
          {/* The legend leads the right column: it is reference for the builder opposite it, and
              below the log it would sit off-screen exactly when a DM needs it. */}
          <SyntaxLegend />
          <RollLogView entries={roller.log} onClear={() => void roller.clearLog()} />
        </div>
      </div>
    </div>
  );
}
