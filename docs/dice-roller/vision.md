# Dice Roller — Vision

## Vision

Dice rolling is the load-bearing procedural primitive of the entire assistant. Every other
system that claims to be *grounded* — monster upscaling, encounter resolution, narrative
beats with real stakes — ultimately cashes out in a die roll that actually happened. The
dice roller is therefore not a utility bolted onto the side of the product; it is the first
and most important proof that this assistant does real math instead of plausible-sounding
math.

The dream version is a single roll engine behind three front doors: a **DM-facing builder**
for typing or assembling a roll by hand, a **contextual roll** taken directly off a stat
block (click the goblin's scimitar; it knows its own attack bonus and damage die), and an
**MCP tool** the LLM calls when it needs a number. No caller gets a private path and no
caller gets a private log. The long-term bet is that this assistant could one day *be* the
DM — and the only thing that could ever make an LLM trustworthy enough to run a fight is a
complete, attributed audit trail of every number it rolled. That audit trail is designed in
from the first commit, not retrofitted when it becomes urgent.

## User Experience

Mid-session, the DM needs a number in under two seconds and needs to believe it. The roller
is always to hand: a compact builder — a count field, a `D`, a sides field, a `±`, a
modifier field, driven by a number pad — sitting above an expression field that shows
`27d3+4`. The two are the same thing seen twice; type into the expression and the builder
re-populates, nudge the builder and the expression regenerates.

Rolling is *legible*. The result does not just say `14` — it shows the dice that produced it,
faces and all, with discarded dice visibly struck through: `4d6kh3 → [6] [5] [3] ~~[1]~~ = 14`.
A DM can eyeball that and trust it. Rolling is also *satisfying*: a sound, and eventually
dice that tumble across a canvas before settling.

Behind the roller sits a running log of the session's rolls — enough to answer "wait, what
was that roll two rolls ago?" — clearable whenever the table moves on.

## Mechanics & Systems

- **Roll engine.** A standalone library: tokenizer → parser → AST → evaluator. The grammar is
  a small *language*, not a set of special cases, so new dice mechanics are grammar additions
  rather than rewrites. Deliberately free of any web-framework dependency so that additional
  callers (notably an MCP server) can wrap it without refactoring.
- **Grammar.** `NdX`, `+`/`-`/`*`/`/` arithmetic, parentheses, keep-highest / keep-lowest /
  drop-highest / drop-lowest. Advantage and disadvantage are sugar over `2d20kh1` / `2d20kl1`
  rather than special-cased concepts. Room to grow into exploding dice, rerolls, and dice pools.
- **Roll result.** Every roll — regardless of caller — produces the same structured record:
  the normalized expression, every individual die face, which dice were kept, the modifier,
  the total, an **attribution** (who rolled this and why), a **visibility** flag
  (shown/hidden), and a timestamp.
- **Attribution is non-negotiable.** The LLM gets no privileged, unlogged roll path. If a
  roll happened, it is in the log and it says who asked for it. This is the single property
  that makes the autonomous-DM north star a gradual promotion instead of a rewrite.
- **Hidden vs. shown.** A DM classically rolls perception behind the screen. Meaningless in a
  single-user local cockpit, so nothing consumes the flag today — but it exists in the data
  model from day one because it is the hook a future player-facing portal hangs off, and it
  is miserable to retrofit.
- **Roll log.** Session-scoped working memory: bounded recent history, clearable, in-memory.
  It is explicitly *not* campaign canon — nobody needs to recall a die roll from six months
  ago.
- **Theming.** The roller is rendered entirely through design tokens. No hardcoded colors,
  including on any canvas surface.

## Open Questions

- What shape should LLM roll attribution take — free-text reason, or a structured
  (intent, subject) pair that other systems can query?
- Does the hidden/shown flag ever acquire a real consumer, or does the player portal stay
  hypothetical?
- Rolling on a **random table** (a d100 result mapping to an outcome) is dice-adjacent but
  arguably not the dice roller's job. Does it live here, or in the system that owns the table?
- Does the roll log ever need to persist? Currently no. If Session Summarization ever wants
  "the fight where the barbarian crit twice," that answer changes.
- Tumbling dice: a canned animation, or real physics? The former is likely 90% of the joy for
  10% of the work.

## Possible Next Directions

Deliberately unranked — the next step will be chosen after the MVP has been used, not before.

- **Flow C — MCP exposure.** Wrap the engine as an MCP tool so an LLM can roll. Unblocked
  today; needs no LLM Provider Abstraction, because any external MCP client can drive it.
  This is the iteration that proves the product's central grounding bet.
- **Flow B — stat-block rolls.** Click the goblin's scimitar. Blocked on the SRD Content
  Corpus; proves the engine composes with real game data.
- **Tumbling dice.** Canvas animation plus sound. Pure delight, zero architectural risk.
- **Dice tray.** Tap `d6` four times to build `4d6` — a faster path than the builder for
  common rolls.
- **Grammar expansion.** Exploding dice, rerolls, dice pools. Cheap, because the AST was
  built for it.
- **Player portal.** Gives the hidden/shown flag its first real consumer.

## Out of Scope

- **Encounter and combat state.** Initiative, positions, HP, action economy. The dice roller
  is called *by* that system; it does not become it. Tactical decision-making by the LLM is a
  separate feature entirely.
- **Campaign canon.** Rolls are session scratch. Durable world-state belongs to Campaign
  Tracking.
- **A player-facing application.** The cockpit is the DM's. The hidden/shown flag anticipates
  a player surface; it does not build one.
