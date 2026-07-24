# Dice Roller — 001 MVP Spec

## Goal

Ship a DM-facing dice roller that is fast, legible, and trustworthy, and in doing so stand up
the repository's first working code: an ASP.NET Core backend, a React/TypeScript frontend, and
a standalone dice-engine library that later callers (an MCP server; a stat-block surface) can
reuse without modification.

The MVP demonstrates one of the vision's three front doors — the DM builder — and deliberately
defers the other two. Its success condition is that a DM can build or type any roll the grammar
supports, see the individual dice that produced the total, and scan a short history of the
session's rolls.

## In Scope

- A standalone C# dice engine: tokenizer → parser → AST → evaluator.
- Grammar: `NdX`, `+` `-` `*` `/`, parentheses, and keep/drop selectors (`kh` `kl` `dh` `dl`).
- Structured roll results carrying every individual die face and its kept/dropped state.
- Attribution and visibility fields on every roll (populated, but only one value used in MVP).
- The builder UI (count / `D` / sides / `±` / modifier), whose fields can be typed into directly
  or driven by an on-screen number pad.
- An expression field, two-way bound to the builder, that accepts direct typed input.
- A session roll log: bounded, in-memory, clearable.
- A sound effect on roll.
- The "Scroll" theme (sepia, parchment) implemented through design tokens.
- Solution scaffold: backend, frontend, test project, and the HTTP boundary between them.

> **As-built:** the MVP UI also ships an optional reason input and a three-state
> advantage/disadvantage toggle — diverged from planned exclusion of both from MVP scope
> (product owner vetoed D3/D4 during implementation; see `handoffs/design-2.md` §10a).

## Out of Scope (for MVP)

- **MCP exposure.** No LLM interaction exists anywhere in the product yet; a tool nobody calls
  is not worth building. The engine's independence from the web API is what keeps this cheap
  later — see Extension Hooks.
- **Stat-block rolls (flow B).** Requires the SRD Content Corpus, which does not exist.
- **Tumbling dice / canvas animation.** Sound only. Punted per owner.
- **Dice tray** (tap `d6` four times to build `4d6`).
- **Exploding dice, rerolls, dice pools.** The AST is built to accept them; the grammar is not
  extended to include them.
- **Persistence of any kind.** No SQLite, no EF Core. The roll log dies with the process.
- **A theme switcher.** One theme, wired through tokens.
- **Hidden rolls as a behavior.** The flag exists in the model; nothing reads it.

## Data Model

In-memory only. No database.

```csharp
enum RollSource { Dm, StatBlock, Llm }      // only Dm is produced in MVP
enum RollVisibility { Shown, Hidden }       // only Shown is produced in MVP

record RollRequest(
    string Expression,
    RollSource Source,
    string? Reason,                          // "why" — null for a bare DM roll
    RollVisibility Visibility);

record DieRoll(int Sides, int Face, bool Kept);

record RollResult(
    Guid Id,
    string Expression,                       // normalized form
    IReadOnlyList<DieRoll> Dice,             // every die rolled, in roll order
    int Total,
    RollSource Source,
    string? Reason,
    RollVisibility Visibility,
    DateTimeOffset RolledAt);
```

`Dice` records dice that were rolled and then discarded by a keep/drop selector, with
`Kept = false`. The UI relies on this to strike them through; discarding them at evaluation
time would make the math illegible.

## Grammar

```
expression := term (('+' | '-') term)*
term       := factor (('*' | '/') factor)*
factor     := number | dice | '(' expression ')' | '-' factor
dice       := [number] 'd' number [selector]
selector   := ('kh' | 'kl' | 'dh' | 'dl') [number]     // count defaults to 1
```

- Omitted dice count defaults to `1` (`d20` == `1d20`).
- Division is integer division, truncating toward zero (5e halves and rounds down constantly).
- Advantage and disadvantage are **not** grammar concepts — they are `2d20kh1` and `2d20kl1`.
  The UI may offer a toggle, but it emits an expression.

## Behaviors

**Rolling**
- A valid expression produces a `RollResult` and prepends it to the log.
- Every die rolled appears in `Dice`, including dice discarded by a selector.
- The result view renders faces individually, striking through `Kept = false` dice, and shows
  the total: `4d6kh3 → [6] [5] [3] ~~[1]~~ = 14`.
- A sound effect fires on roll.

**Builder ↔ expression sync**
- The expression string is the single source of truth. The builder is a view over it.
- Editing a builder field regenerates the expression.
- The one exception, and it is deliberately narrow: a field being typed into passes through
  states no expression can hold (`""` while cleared, `"0"` before the next digit), so the panel
  holds a transient edit buffer for the focused field alone. Every parseable keystroke commits to
  the expression immediately, and the buffer is dropped on blur. It is a keystroke carrier, not a
  second copy of the roll — an empty expression must never be read as "the DM chose 1d6".
- The number pad and the keyboard write through that same buffer, so neither can append digits to
  a default the DM never chose (pressing `C` then `4` yields `4d6`, not `14d6`).
- `2dXkh1` / `2dXkl1` are the advantage sugar forms, **not** advanced expressions: they parse back
  to one die rolled twice, so the builder stays live for them. But `formatSimple` drops `count`
  whenever advantage is on, so the count field locks (and says why) while the toggle is set —
  otherwise it would accept a number and silently discard it. Every write path honors the lock,
  the number pad included; a control that refuses an edit must refuse it from every direction.
- Typing a valid expression that fits the `NdX±M` shape re-populates the builder fields.
- Typing a valid expression the builder **cannot** represent (`4d6kh3`, `(2d6+3)*2`) puts the
  builder into a disabled **advanced expression** state — visibly greyed out, indicating the
  expression field is driving. The roll still works normally. Clearing back to a simple
  expression re-enables the builder.

**Validation and edge cases**
- Invalid syntax produces a parse error naming the offending position. No roll is logged.
- Dice count of `0` or fewer, or sides of `0` or fewer, is an error (`0d6`, `2d0`).
- A selector keeping/dropping more dice than were rolled is an error (`2d6kh3`).
- Roll size is capped — at most **1000 dice** in a single expression and at most **1000 sides**
  on a die — to prevent a typo (`10000d20`) from hanging the UI. Exceeding a cap is an error.
- Division by zero is an error.

> **As-built:** cap and other semantic errors always carry a `position` pointing at the
> offending token — diverged from the planned nullable-position case implied for violations
> spanning the whole expression (see `handoffs/review-2.md`; `DiceExpressionException`'s doc
> comment was amended to match).

**Log**
- Holds the most recent **100** rolls, newest first; older entries fall off.
- A Clear action empties it. No confirmation.
- The log does not survive a process restart, by design.

## Integration Points

None exist — this is the repository's first code. The MVP therefore *establishes* the
integration surface the rest of the product builds on:

- **Solution layout.** `src/DmAssist.Dice` (engine, no web dependencies), `src/DmAssist.Api`
  (ASP.NET Core), `tests/DmAssist.Dice.Tests`, `web/` (Vite + React + TS).

  > **As-built:** an additional `tests/DmAssist.Api.Tests` project was added — diverged from
  > the single test project named above, because log cap/clear behavior and the error contract
  > live in the API host, not the engine (declared deviation; see `handoffs/design-2.md` §2).
- **HTTP boundary.** JSON over HTTP. `POST /api/rolls` takes an expression and returns a
  `RollResult`; `GET /api/rolls` returns the log; `DELETE /api/rolls` clears it. Streaming
  (SSE) is not needed here — that arrives with the generative features.
- **Design-token layer.** The Scroll theme's tokens, and the rule that no component hardcodes
  a color.

## Extension Hooks

These must be wired now even though nothing consumes them yet. Each is cheap today and
expensive to retrofit.

- **`DmAssist.Dice` takes no dependency on ASP.NET Core.** The web API is *one consumer* of the
  engine. An MCP server will be a second consumer wrapping the same library. If the engine grows
  tendrils into request handling, flow C becomes a rewrite rather than a wrapper. This is the
  single most important constraint in this spec.
- **`IRandomSource`** abstracts the RNG so the engine can be driven with a seeded or scripted
  source in tests. A dice engine that cannot be tested deterministically cannot be trusted.
- **`RollSource` / `Reason` / `RollVisibility`** are populated on every roll even though the MVP
  only ever writes `Dm` / `null` / `Shown`. They are what make LLM rolls auditable later, and
  what a player portal reads.
- **AST node types** are the grammar's extension point. Exploding dice and rerolls should be new
  node kinds, not new branches in a regex.
- **Design tokens** are what make the Theming System — and the "ALL RED" conformance theme that
  catches components which skipped the tokens — possible without a UI rewrite.

## Acceptance Criteria

- [ ] `dotnet build` and `dotnet test` succeed; the frontend builds and runs against the API.
- [ ] `DmAssist.Dice` compiles with no reference to ASP.NET Core or any web framework.
- [ ] The engine evaluates `NdX`, arithmetic (`+ - * /`), parentheses, and `kh`/`kl`/`dh`/`dl`.
- [ ] `d20` is accepted and treated as `1d20`.
- [ ] `4d6kh3` returns four dice, exactly three marked `Kept`, and a total of only the kept dice.
- [ ] `2d20kh1` and `2d20kl1` produce correct advantage and disadvantage results.
- [ ] Integer division truncates toward zero.
- [ ] The engine is deterministic when given a seeded `IRandomSource`; tests prove it.
- [ ] Invalid syntax, `0d6`, `2d0`, `2d6kh3`, division by zero, and rolls exceeding the 1000-dice
      or 1000-sides caps all produce errors and log nothing.
- [ ] A DM can build a roll with the number pad and roll it.
- [ ] A DM can type `27` straight into the builder's count field; pressing `C` then `4` gives
      `4d6`, not `14d6`.
- [ ] Typing `27d3+4` into the expression field populates the builder fields.
- [ ] Typing `4d6kh3` disables the builder into the advanced-expression state; the roll still works.
- [ ] Results display every die face, with dropped dice struck through, alongside the total.
- [ ] A sound effect plays on roll.
- [ ] The log shows recent rolls newest-first, caps at 100, and can be cleared.
- [ ] Every `RollResult` carries `Source`, `Reason`, `Visibility`, and `RolledAt`.
- [ ] The UI renders in the Scroll theme with no hardcoded colors in any component.
