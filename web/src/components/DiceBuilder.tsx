import type { ReactElement } from 'react';
import { DEFAULT_SIMPLE, type SimpleRoll } from '../dice/simpleExpression';
import { useToast } from '../state/toastContext';

export type BuilderField = 'count' | 'sides' | 'modifier';

export interface DiceBuilderProps {
  /** Current parse of the expression; null renders the defaults (1 d 6 + 0). */
  simple: SimpleRoll | null;
  /**
   * Why the whole row is unavailable (the advanced-expression state), or null when it is
   * editable. A reason rather than a boolean: greying a control out without saying why leaves
   * the DM poking at a dead field with no way to find out what is wrong.
   */
  disabledReason: string | null;
  /**
   * Why an individual field is locked while the rest of the row still works — advantage pins
   * the count at 1, for instance. Without this the field would look live and silently discard
   * every edit, which is worse than being visibly dead.
   */
  fieldReasons?: Partial<Record<BuilderField, string>>;
  /** Which field the number pad edits, and the one `draft` (if any) belongs to. */
  focusedField: BuilderField;
  /**
   * Text mid-entry in `focusedField`, or null when that field shows its committed value. A
   * half-typed field can read `""` or `"0"`, neither of which any expression can represent, so
   * the caller holds this buffer; the builder only renders it.
   */
  draft: string | null;
  onFocusField: (field: BuilderField) => void;
  onEditField: (field: BuilderField, text: string) => void;
  onBlurField: () => void;
  /** Flip the modifier's sign (no-op when the modifier is 0). */
  onToggleSign: () => void;
}

/**
 * The [count] d [sides] ± [modifier] view over the expression. Pure view: values come from
 * `simple` (or `draft` mid-edit), and every change leaves through a callback — this component
 * holds no state of its own. The fields are real text inputs, so they can be typed into
 * directly; the number pad is an alternative for touch, not the only way in.
 */
export function DiceBuilder({
  simple,
  disabledReason,
  fieldReasons = {},
  focusedField,
  draft,
  onFocusField,
  onEditField,
  onBlurField,
  onToggleSign,
}: DiceBuilderProps): ReactElement {
  const roll = simple ?? DEFAULT_SIMPLE;
  const disabled = disabledReason !== null;
  const toast = useToast();

  const box = (field: BuilderField, value: number) => {
    // The row's reason outranks the field's: in the advanced state nothing here is editable,
    // whatever a field might otherwise have to say for itself.
    const reason = disabledReason ?? fieldReasons[field] ?? null;
    const locked = reason !== null;
    const focused = !locked && focusedField === field;
    return (
      <input
        type="text"
        inputMode="numeric"
        value={focused && draft !== null ? draft : String(value)}
        maxLength={4}
        // `readOnly` + `aria-disabled` rather than `disabled`: a disabled input dispatches no
        // events, so it could not answer a click. This one still refuses every edit, but a DM
        // who clicks or types into it is told why instead of meeting silence.
        readOnly={locked}
        aria-disabled={locked}
        aria-label={field}
        onFocus={(e) => {
          if (locked) return;
          onFocusField(field);
          // Select on entry so the first keystroke replaces the value rather than appending to
          // it — the number pad's first digit does the same (see RollerPanel's handleDigit).
          e.currentTarget.select();
        }}
        onClick={() => locked && toast.show(reason)}
        onKeyDown={(e) => {
          if (locked && /^[0-9]$/.test(e.key)) toast.show(reason);
        }}
        onMouseUp={(e) => e.preventDefault()} // or the click would collapse that selection
        onChange={(e) => onEditField(field, e.target.value)}
        onBlur={onBlurField}
        title={fieldReasons[field]}
        style={{
          width: '3.5em',
          padding: 'var(--space-sm)',
          fontFamily: 'monospace',
          fontSize: '1.2em',
          textAlign: 'center',
          color: 'var(--ink)',
          background: 'var(--surface)',
          border: focused ? '2px solid var(--focus)' : '2px solid var(--border)',
          borderRadius: 'var(--radius-sm)',
          // A field locked on its own (advantage pinning the count) must read as dead while the
          // rest of the row reads as live; the row-level grey-out already covers the other case.
          opacity: !disabled && locked ? 0.45 : 1,
          cursor: locked ? 'not-allowed' : undefined,
        }}
      />
    );
  };

  return (
    <div
      title={disabledReason ?? undefined}
      style={{
        display: 'flex',
        alignItems: 'center',
        gap: 'var(--space-sm)',
        opacity: disabled ? 0.45 : 1,
      }}
    >
      {box('count', roll.count)}
      <span style={{ color: 'var(--accent)', fontFamily: 'var(--font-display)', fontSize: '1.1em' }}>d</span>
      {box('sides', roll.sides)}
      <button
        aria-disabled={disabled}
        onClick={() => (disabledReason !== null ? toast.show(disabledReason) : onToggleSign())}
        aria-label="toggle modifier sign"
        style={{
          padding: 'var(--space-sm)',
          fontFamily: 'monospace',
          fontSize: '1.2em',
          color: 'var(--accent)',
          background: 'var(--surface-raised)',
          border: '2px solid var(--border)',
          borderRadius: 'var(--radius-sm)',
          cursor: disabled ? 'not-allowed' : 'pointer',
        }}
      >
        {roll.modifier < 0 ? '−' : '+'}
      </button>
      {box('modifier', Math.abs(roll.modifier))}
    </div>
  );
}
