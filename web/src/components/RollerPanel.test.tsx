import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { describe, expect, it, vi } from 'vitest';
import { RollerPanel } from './RollerPanel';
import { ToastProvider } from './ToastProvider';
import { useRoller } from '../state/useRoller';

// The panel is a view over useRoller, and the behavior worth testing here is the round trip
// between them (a keystroke becomes an expression). So these render the real hook against a
// stubbed transport rather than a hand-built mock of it, which would let the two drift.
vi.mock('../api/rollsClient', async () => {
  const actual = await vi.importActual<typeof import('../api/rollsClient')>('../api/rollsClient');
  return { ...actual, roll: vi.fn(), getLog: vi.fn().mockResolvedValue([]), clearLog: vi.fn() };
});
vi.mock('../audio/rollSound');

function Panel() {
  return <RollerPanel roller={useRoller()} />;
}

function Harness() {
  return (
    <ToastProvider>
      <Panel />
    </ToastProvider>
  );
}

const expression = () => screen.getByPlaceholderText('e.g., 4d6kh3') as HTMLInputElement;
const field = (name: 'count' | 'sides' | 'modifier') => screen.getByLabelText(name) as HTMLInputElement;
const pad = (key: string) => screen.getByRole('button', { name: key });
const toast = () => screen.getByRole('status');

describe('RollerPanel builder fields', () => {
  it('takes typed digits directly, replacing the displayed value', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(field('count'));
    await user.keyboard('4');
    await user.click(field('sides'));
    await user.keyboard('20');

    expect(expression().value).toBe('4d20');
  });

  it('starts a fresh number after Clear rather than appending to the shown default', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(field('count'));
    await user.keyboard('3');
    await user.click(pad('C'));
    expect(expression().value).toBe('');

    // The field shows the default 1 here, but the DM never chose it — a 4 means 4, not 14.
    await user.click(pad('4'));
    expect(expression().value).toBe('4d6');
  });

  it('lets the number pad build a multi-digit value while a field holds focus', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(field('sides'));
    await user.click(pad('1'));
    await user.click(pad('0'));
    await user.click(pad('0'));

    expect(expression().value).toBe('1d100');
  });

  it('keeps the expression intact while a field is cleared mid-edit', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(field('count'));
    await user.keyboard('27');
    expect(expression().value).toBe('27d6');

    await user.clear(field('count'));
    // Nothing to commit, so the expression holds its last good value...
    expect(expression().value).toBe('27d6');
    // ...and the emptied field is what the DM sees, not a value snapped back under them.
    expect(field('count').value).toBe('');

    await user.keyboard('8');
    expect(expression().value).toBe('8d6');
  });

  it('clamps a typed value to the engine limits', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(field('sides'));
    await user.keyboard('9999');

    expect(expression().value).toBe('1d1000');
  });

  it('refuses edits, without going silent, for an expression the builder cannot represent', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.type(expression(), '4d6kh3');
    for (const name of ['count', 'sides', 'modifier'] as const) {
      expect(field(name)).toHaveAttribute('aria-disabled', 'true');
      expect(field(name)).toHaveAttribute('readonly');
    }

    // The rejected keystroke still lands somewhere the DM can see.
    await user.click(field('count'));
    expect(toast()).toHaveTextContent(/beyond the builder/);
    expect(expression().value).toBe('4d6kh3');
  });

  it('keeps the advantage toggle on screen when the expression is empty', async () => {
    render(<Harness />);

    // It used to unmount here, which both hid the control and jumped the layout.
    expect(screen.getByRole('button', { name: 'advantage' })).toHaveAttribute('aria-disabled', 'false');
  });

  it('explains, on click, why advantage is unavailable over a pool of dice', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(field('count'));
    await user.keyboard('9');

    const advantage = screen.getByRole('button', { name: 'advantage' });
    expect(advantage).toHaveAttribute('aria-disabled', 'true');
    expect(screen.getByText(/needs a count of 1 \(this rolls 9\)/)).toBeInTheDocument();

    await user.click(advantage);
    expect(toast()).toHaveTextContent(/needs a count of 1 \(this rolls 9\)/);
    // Refusing to act is the point: the roll must not have picked up advantage.
    expect(expression().value).toBe('9d6');
  });

  it('explains, on click, why advantage is unavailable for an advanced expression', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.type(expression(), '4d6kh3');
    await user.click(screen.getByRole('button', { name: 'advantage' }));

    expect(toast()).toHaveTextContent(/beyond the builder/);
    expect(expression().value).toBe('4d6kh3');
  });

  it('locks the count, rather than swallowing the edit, while advantage is on', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    // 2d6kl1 is disadvantage sugar, not an advanced expression: it parses to one d6 rolled twice,
    // so sides and modifier stay editable while the count is pinned at 1.
    await user.type(expression(), '2d6kl1');

    expect(field('count')).toHaveAttribute('aria-disabled', 'true');
    expect(field('sides')).toHaveAttribute('aria-disabled', 'false');
    expect(field('modifier')).toHaveAttribute('aria-disabled', 'false');

    await user.click(field('count'));
    expect(toast()).toHaveTextContent(/Disadvantage rolls a single die twice, so the count is fixed at 1/);
    expect(expression().value).toBe('2d6kl1');
  });

  it('refuses the number pad too, which used to be a back door into the locked count', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    // Focus the count while it is still editable, then lock it by turning advantage on — the pad
    // is still aimed at the count, and formatSimple would silently drop whatever it wrote.
    await user.click(field('count'));
    await user.type(expression(), '2d6kh1');
    await user.click(pad('3'));

    expect(toast()).toHaveTextContent(/Advantage rolls a single die twice, so the count is fixed at 1/);
    expect(expression().value).toBe('2d6kh1');
  });

  it('still edits the sides and modifier while advantage pins the count', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.type(expression(), '2d6kh1');
    await user.click(field('sides'));
    await user.keyboard('20');
    await user.click(field('modifier'));
    await user.keyboard('3');

    expect(expression().value).toBe('2d20kh1+3');
  });

  it('drops the standing hint, and works again, once the control becomes usable', async () => {
    const user = userEvent.setup();
    render(<Harness />);

    await user.click(field('count'));
    await user.keyboard('9');
    await user.click(screen.getByRole('button', { name: 'advantage' }));
    expect(toast()).toHaveTextContent(/needs a count of 1/);

    await user.click(field('count'));
    await user.keyboard('1');

    const advantage = screen.getByRole('button', { name: 'advantage' });
    expect(advantage).toHaveAttribute('aria-disabled', 'false');

    // The standing hint under the buttons describes the control as it is *now*, so it goes at
    // once. The toast is a reply to a click already made — it stays until it expires on its own.
    const standingHints = screen
      .queryAllByText(/needs a count of 1/)
      .filter((el) => el.closest('[role="status"]') === null);
    expect(standingHints).toHaveLength(0);

    await user.click(advantage);
    expect(expression().value).toBe('2d6kh1');
  });
});
