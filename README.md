# dm-assist

A Dungeon Master's assistant for D&D 5e, grounded in the 5e SRD — specifically **SRD 5.2.1**
(the 2024 rules, released under CC-BY-4.0). It runs locally in a browser and blends three kinds
of capability: **procedural** (deterministic math and algorithms — dice, dungeon generation, CR
math), **generative** (LLM-authored content — narrative beats, NPCs, monster variants), and
**persistent** (a campaign world-state that is read back as planning context, not just written
to). The design bet is that generative features must be grounded in procedural ones: the LLM
calls real dice and real stat-block math rather than inventing numbers.

## Current features

- **Dice roller (MVP)** — a DM-facing roller with a builder (count / `D` / sides / `±` /
  modifier, driven by a number pad) two-way bound to a typed expression field, an
  advantage/disadvantage toggle, an optional reason input, sound on roll, and a session-scoped
  roll log. Results show every individual die face, with dice dropped by a keep/drop selector
  struck through. See `docs/dice-roller/001-mvp-spec.md`.

## Stack

| Layer | Choice |
|-------|--------|
| Backend | C# / ASP.NET Core |
| Persistence | SQLite via EF Core (not yet used — the dice roller MVP is in-memory only) |
| Frontend | React + TypeScript (Vite) |
| API boundary | JSON/HTTP |

## Build / test / run

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

## Project layout

```
src/
  DmAssist.Dice/       # standalone dice-expression engine (no web dependencies)
  DmAssist.Api/        # ASP.NET Core minimal API, exposes /api/rolls
tests/
  DmAssist.Dice.Tests/
  DmAssist.Api.Tests/
web/                   # Vite + React + TypeScript frontend
docs/<feature>/        # committed specs (vision + MVP spec per feature)
FEATURE_LOG.md         # registry of every named concept and its status
TODOS.md               # backlog items not yet worth a GitHub issue
```

## Documentation

- `CLAUDE.md` — architecture, cross-cutting design constraints, and workflow guidance.
- `docs/dice-roller/` — vision and MVP spec for the dice roller.
- `docs/srd-content-corpus/` — vision and MVP spec for the SRD corpus (specced, not yet built).
- `FEATURE_LOG.md` — what concepts exist and where they stand.
