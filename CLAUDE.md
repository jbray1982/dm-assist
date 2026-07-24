# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A Dungeon Master's assistant for D&D 5e, grounded in the 5e SRD — specifically **SRD 5.2.1**
(the 2024 rules, released under CC-BY-4.0). It runs locally in a browser and blends three kinds
of capability:

- **Procedural** — deterministic math and algorithms (dice, dungeon generation, CR math).
- **Generative** — LLM-authored content (narrative beats, NPCs, monster variants).
- **Persistent** — a campaign world-state that is read back as planning context, not just written to.

The design bet is that generative features must be *grounded* in procedural ones: the LLM
calls real dice and real stat-block math via MCP rather than inventing numbers.

## Current state

The dice roller MVP (issue #2) is implemented: a standalone `DmAssist.Dice` engine library
(tokenizer → parser → AST → evaluator, zero web-framework dependency), an ASP.NET Core API
(`DmAssist.Api`) exposing `/api/rolls`, and a Vite/React/TypeScript frontend (`web/`) with the
DM builder, expression field, advantage/disadvantage toggle, roll log, and Scroll theme. See
`docs/dice-roller/001-mvp-spec.md` for the contract.

Build / test / run:

```
dotnet build DmAssist.sln                 # whole solution
dotnet test                               # DmAssist.Dice.Tests + DmAssist.Api.Tests
dotnet run --project src/DmAssist.Api     # API on http://localhost:5178
cd web && npm install
npm run dev                               # Vite dev server, /api proxied to :5178
npm run build                             # tsc + vite build
npm run test                              # Vitest
npm run lint                              # eslint + stylelint (design-token enforcement)
```

The design surface lives in `FEATURE_LOG.md` (every named concept and its status).
Work not yet promoted to a GitHub issue lives in `TODOS.md`.

## Stack (decided)

| Layer | Choice |
|-------|--------|
| Backend | C# / ASP.NET Core |
| Persistence | SQLite via EF Core |
| MCP servers | C# (`ModelContextProtocol` SDK) |
| Frontend | React + TypeScript (Vite) |
| API boundary | JSON/HTTP, with streaming (SSE) for generative features |

**Why this split, so it doesn't get relitigated:** the owner has 12 years of C# and
limited Python; backend velocity dominates every ecosystem argument for a TS/Python
backend, and the C# MCP SDK removes the one hard blocker that used to exist.
The frontend is React rather than Blazor because (a) the owner already knows React and
does not know Blazor, and (b) the heaviest UI work — the geography graph and dungeon
rendering — is interactive canvas/SVG, where React's ecosystem is strongest and where
Blazor would be dropping to JS interop anyway.

Rule of thumb: **C# where the logic is, React where the pixels are.**

## Cross-cutting design constraints

These are load-bearing. Most features are blocked on them, so build them deliberately
rather than letting each feature grow its own version.

**LLM Provider Abstraction.** No feature code may bind to a vendor. One interface, with
adapters that either shell out to a headless CLI (`codex`, `claude -p`) via `Process` or
call an HTTP API. Codex headless is the near-term target; Claude Code headless and a
direct API key must be swappable without touching feature code.

**Provider Capability Registry.** The backends are *not* interchangeable — they differ in
streaming, tool use, structured output, context window, and cost. Model capabilities
explicitly per provider so orchestration can route each feature to a provider that can
actually do the job and degrade gracefully when one can't. Do not assume a capability is
present; ask the registry.

**Human-in-the-Loop Approval Gate.** Generative output is *proposed*, never auto-committed.
Narrative beats, NPC development, and monster upscaling all share one flow:
generate → DM reviews → accept / regenerate / edit → lock to canon. Build it once, reuse it.
If a feature writes LLM output straight into campaign state, that's a bug.

**MCP Tool Layer.** Procedural capabilities are exposed to the LLM as MCP tools so
generation is grounded. The dice roller is both a DM-facing feature and an LLM-callable tool.

**SRD Content Corpus.** The ingested SRD grounds both procedural and generative work.
Monster variants derive from real base stat blocks; items derive from real item rules.
The SRD is ingested as a *structured domain model* in SQLite, not a text blob — a monster's
AC is a number and its hit points are a dice expression the roll engine can evaluate. It is
also the single provenance-tagged home for everything stat-block-shaped: SRD records, derived
variants, and DM homebrew live in one schema, distinguished by `provenance` and `parent_id`
rather than by living in separate stores. Every record carries a `corpus_id` so a second
source can be added without a migration — but only SRD 5.2.1 is targeted. See
`docs/srd-content-corpus/vision.md`.

**Design Tokens (theming).** The UI must be fully re-skinnable by end users, so no component
may hardcode a color, font, or texture — everything resolves through design tokens. This
includes canvas/SVG surfaces (dungeon and map rendering), which read the same tokens as the
UI chrome rather than carrying their own palette. Cheap to honor from day one, expensive to
retrofit.

## Workflow (llm-academy)

This repo uses the llm-academy skill set. The pipeline is deliberate — follow it rather
than jumping straight to code:

- `/feature-spec` — interview-driven spec; produces a vision doc, an MVP spec, and GitHub issues.
- `/feature-log` — query `FEATURE_LOG.md`, the registry of every named concept and its status.
- `/noodle-on` — exploratory design proposals, saved to `noodles/`.
- `/feature-flow` — run a single GitHub issue end to end (BA triage → architect → implement → review → commit).
- `/interview-me`, `/ba-triage`, `/next-issue`, `/tech-debt-analysis` — supporting steps.

Where things live:
- `FEATURE_LOG.md` — what concepts exist and where they stand. **Not** a priority list.
- `TODOS.md` — backlog items not yet worth a GitHub issue.
- `noodles/` — design exploration, not commitments.
- `docs/<feature>/` — committed specs.
- GitHub issues — the implementation backlog.

`FEATURE_LOG.md` is maintained by the skills; prefer letting them update it over hand-editing.

Note: `.claude/skills/*` and `.claude/agents/*` are gitignored symlinks into a local
llm-academy clone. They will not be present in a fresh clone of this repo.

## Conventions

- Status vocabulary for `FEATURE_LOG.md` (`concept`, `defined`, `partially-live`, `live`, …)
  and conviction vocabulary (`must-have`, `probably-need`, `cool-if`) are defined in the
  `/feature-log` skill. Use them exactly.
- Distinguish *procedural* from *generative* when adding a feature — it determines whether
  it needs the approval gate and whether it belongs behind an MCP tool.
