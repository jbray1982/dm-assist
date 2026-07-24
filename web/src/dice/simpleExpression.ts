/**
 * Defines what "builder-representable" means and how to parse/format that subset of the engine
 * grammar. Deliberately NOT a dice parser — it recognizes exactly two shapes and returns `null`
 * for everything else (including selectors, parentheses, `*`, `/`), which is what puts the
 * builder into its disabled "advanced expression" state. Anything this module emits is
 * guaranteed valid server-side, because its output language is a strict subset of the engine
 * grammar.
 *
 * Recognized shapes:
 *  - Plain: optional count, `d`/`D`, sides, optional `±M` — e.g. `d20`, `27d3+4`, `3d6-2`.
 *    Parses to `{ count, sides, modifier, advantage: 'normal' }`.
 *  - Advantage/disadvantage sugar (design decision D4, added by product-owner override
 *    2026-07-12): exactly `2d{sides}kh1[±M]` or `2d{sides}kl1[±M]` — the canonical forms the
 *    advantage/disadvantage toggle emits. Parses to `{ count: 1, sides, modifier, advantage }`;
 *    `count` is always reported as 1 here because the toggle models "one d-something rolled
 *    with advantage/disadvantage," not "two dice." A count other than 1 in the *plain* shape is
 *    valid (`3d6`), but disables the advantage/disadvantage toggle in the UI (see `useRoller`'s
 *    `advantageEnabled`) — advantage only has a defined meaning over a single die.
 */

export type AdvantageState = 'normal' | 'advantage' | 'disadvantage';

export interface SimpleRoll {
  count: number;
  sides: number;
  modifier: number;
  advantage: AdvantageState;
}

/**
 * What the builder shows when the expression names nothing yet (empty expression, so
 * `parseSimple` returns null). Editing any field from here produces a real expression; until
 * then these are only on screen, which is why an empty expression must never be treated as
 * "the DM chose 1d6".
 */
export const DEFAULT_SIMPLE: SimpleRoll = { count: 1, sides: 6, modifier: 0, advantage: 'normal' };

/** Engine limits, mirrored so the builder cannot emit an expression the server would reject. */
export const MAX_COUNT = 1000;
export const MAX_SIDES = 1000;
/** Four digits is what the builder's fields accept, so the modifier cannot exceed it. */
export const MAX_MODIFIER = 9999;

/** Parses `expr` if it fits a builder-representable shape; otherwise returns `null`. */
export function parseSimple(expr: string): SimpleRoll | null {
  const trimmed = expr.trim();
  if (!trimmed) return null;

  // Check for advantage/disadvantage sugar forms first: 2d{sides}kh1[±M] or 2d{sides}kl1[±M]
  const advantageMatch = trimmed.match(/^2d(\d+)(kh1|kl1)(([+-]\d+)?)$/i);
  if (advantageMatch) {
    const sides = parseInt(advantageMatch[1], 10);
    const selector = advantageMatch[2].toLowerCase();
    const modifierStr = advantageMatch[3];
    const modifier = modifierStr ? parseInt(modifierStr, 10) : 0;
    const advantage: AdvantageState = selector === 'kh1' ? 'advantage' : 'disadvantage';
    return { count: 1, sides, modifier, advantage };
  }

  // Check for plain shape: [count]d[D]sides[±modifier]
  const plainMatch = trimmed.match(/^(\d*)d(\d+)(([+-]\d+)?)$/i);
  if (!plainMatch) return null;

  const count = plainMatch[1] ? parseInt(plainMatch[1], 10) : 1;
  const sides = parseInt(plainMatch[2], 10);
  const modifier = plainMatch[3] ? parseInt(plainMatch[3], 10) : 0;

  return { count, sides, modifier, advantage: 'normal' };
}

/**
 * Renders `roll` back to expression text. When `roll.advantage` is not `'normal'`, `roll.count`
 * is ignored and the canonical two-dice selector form is emitted instead (that is the whole
 * point of the toggle: the builder shows one conceptual die, the wire sees two).
 */
export function formatSimple(roll: SimpleRoll): string {
  if (roll.advantage === 'normal') {
    let expr = `${roll.count}d${roll.sides}`;
    if (roll.modifier !== 0) {
      expr += roll.modifier > 0 ? `+${roll.modifier}` : `${roll.modifier}`;
    }
    return expr;
  }

  // Advantage/disadvantage: always use 2dX form, ignoring roll.count
  const selector = roll.advantage === 'advantage' ? 'kh1' : 'kl1';
  let expr = `2d${roll.sides}${selector}`;
  if (roll.modifier !== 0) {
    expr += roll.modifier > 0 ? `+${roll.modifier}` : `${roll.modifier}`;
  }
  return expr;
}
