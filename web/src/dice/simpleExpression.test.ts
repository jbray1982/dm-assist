import { describe, expect, it } from 'vitest';
import { formatSimple, parseSimple } from './simpleExpression';

describe('parseSimple', () => {
  it('parses a plain NdX+M expression', () => {
    expect(parseSimple('3d6+2')).toEqual({ count: 3, sides: 6, modifier: 2, advantage: 'normal' });
  });

  it('parses omitted count as 1', () => {
    expect(parseSimple('d20')).toEqual({ count: 1, sides: 20, modifier: 0, advantage: 'normal' });
  });

  it('parses a negative modifier', () => {
    expect(parseSimple('3d6-2')).toEqual({ count: 3, sides: 6, modifier: -2, advantage: 'normal' });
  });

  it('tolerates surrounding whitespace', () => {
    expect(parseSimple(' 27d3+4 ')).toEqual({ count: 27, sides: 3, modifier: 4, advantage: 'normal' });
  });

  it('accepts an uppercase D', () => {
    expect(parseSimple('2D8')).toEqual({ count: 2, sides: 8, modifier: 0, advantage: 'normal' });
  });

  // Design decision D4 (reversed from the original MVP scope by product-owner override,
  // 2026-07-12): the advantage/disadvantage toggle's canonical two forms must round-trip
  // through the builder-representable subset.
  it('recognizes the advantage sugar form (2d20kh1)', () => {
    expect(parseSimple('2d20kh1')).toEqual({ count: 1, sides: 20, modifier: 0, advantage: 'advantage' });
  });

  it('recognizes the disadvantage sugar form (2d20kl1)', () => {
    expect(parseSimple('2d20kl1')).toEqual({ count: 1, sides: 20, modifier: 0, advantage: 'disadvantage' });
  });

  it('recognizes advantage combined with a modifier', () => {
    expect(parseSimple('2d20kh1+5')).toEqual({ count: 1, sides: 20, modifier: 5, advantage: 'advantage' });
  });

  it('does not treat an arbitrary selector as advantage/disadvantage', () => {
    // Only the canonical 2dXkh1 / 2dXkl1 shapes are toggle-representable.
    expect(parseSimple('4d6kh3')).toBeNull();
    expect(parseSimple('2d20kh2')).toBeNull();
    expect(parseSimple('2d20dh1')).toBeNull();
  });

  it('returns null for parenthesized expressions', () => {
    expect(parseSimple('(2d6+3)*2')).toBeNull();
  });

  it('returns null for multiplication/division', () => {
    expect(parseSimple('2d6*3')).toBeNull();
    expect(parseSimple('2d6/3')).toBeNull();
  });

  it('returns null for a bare number', () => {
    expect(parseSimple('5')).toBeNull();
  });

  it('returns null for empty input', () => {
    expect(parseSimple('')).toBeNull();
  });
});

describe('formatSimple', () => {
  it('formats a plain roll', () => {
    expect(formatSimple({ count: 3, sides: 6, modifier: 2, advantage: 'normal' })).toBe('3d6+2');
  });

  it('formats a negative modifier', () => {
    expect(formatSimple({ count: 1, sides: 20, modifier: -3, advantage: 'normal' })).toBe('1d20-3');
  });

  it('omits the modifier suffix when the modifier is zero', () => {
    expect(formatSimple({ count: 1, sides: 20, modifier: 0, advantage: 'normal' })).toBe('1d20');
  });

  it('formats advantage as the canonical 2dXkh1 form, ignoring count', () => {
    expect(formatSimple({ count: 1, sides: 20, modifier: 0, advantage: 'advantage' })).toBe('2d20kh1');
  });

  it('formats disadvantage as the canonical 2dXkl1 form', () => {
    expect(formatSimple({ count: 1, sides: 20, modifier: 0, advantage: 'disadvantage' })).toBe('2d20kl1');
  });

  it('formats advantage with a modifier', () => {
    expect(formatSimple({ count: 1, sides: 20, modifier: 5, advantage: 'advantage' })).toBe('2d20kh1+5');
  });

  it('round-trips a plain roll through parseSimple', () => {
    const roll = { count: 4, sides: 8, modifier: -1, advantage: 'normal' as const };
    expect(parseSimple(formatSimple(roll))).toEqual(roll);
  });

  it('round-trips an advantage roll through parseSimple', () => {
    const roll = { count: 1, sides: 20, modifier: 2, advantage: 'advantage' as const };
    expect(parseSimple(formatSimple(roll))).toEqual(roll);
  });
});
