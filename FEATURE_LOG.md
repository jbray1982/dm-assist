# Feature Log

Registry of every named feature, mechanic, or cross-cutting system in this
project's design surface. See the `/feature-log` skill for status/conviction
vocabulary and entry format.

Format: `- **[conviction | status]** **Name** — one-line description.`

## Foundations

- **[must-have | concept]** **LLM Provider Abstraction** — A single interface over swappable LLM backends (Codex headless now; Claude Code headless or a direct API key later) so feature code never binds to a vendor.
  Blocks: Provider Capability Registry, Narrative Beat, NPC Development, Monster Upscaling & Modification, Campaign Tracking, Session Summarization, Equipment & Artifact Creation, Outdoor Adventuring
  Surfaced: project kickoff, 2026-07-12.

- **[must-have | concept | blocked: LLM Provider Abstraction]** **Provider Capability Registry** — A declared capability descriptor per backend (streaming, tool use, structured output, context window, cost) so orchestration can route each feature to a capable provider and degrade gracefully when one cannot do the job.
  Surfaced: project kickoff, 2026-07-12.

- **[must-have | concept]** **MCP Tool Layer** — MCP server(s) exposing deterministic, procedural capabilities to the LLM so generative features call real math instead of hallucinating it.
  Blocks: Monster Upscaling & Modification, Equipment & Artifact Creation, Outdoor Adventuring
  Surfaced: project kickoff, 2026-07-12.

- **[must-have | concept]** **Human-in-the-Loop Approval Gate** — The shared generate → DM reviews → accept / regenerate / edit → lock-to-canon flow that every gated generative feature reuses rather than reimplements.
  Blocks: Narrative Beat, NPC Development, Monster Upscaling & Modification
  Surfaced: project kickoff, 2026-07-12.

- **[must-have | defined]** **SRD Content Corpus** — The SRD ingested as a fully structured domain model in SQLite (typed monsters, spells, items; rules as searchable prose) that grounds every procedural and generative feature, and the single provenance-tagged home for derived and homebrew stat blocks. Source: SRD 5.2.1 (2024 rules, CC-BY-4.0) via a vendored community markdown conversion; multi-corpus by architecture, single-corpus by intent.
  Blocks: Monster Upscaling & Modification, Equipment & Artifact Creation, Narrative Beat
  Surfaced: project kickoff, 2026-07-12. See: docs/srd-content-corpus/vision.md, docs/srd-content-corpus/001-mvp-spec.md, issues #3 (MVP), #4 (executable actions).

- **[must-have | concept]** **Local Web Cockpit** — The browser UI the DM actually runs a session from; every feature surfaces through it. Local-first, single user.
  Blocks: Dungeon Drawing, Geography Tracking, Theming System
  Surfaced: project kickoff, 2026-07-12.

- **[probably-need | concept | blocked: Local Web Cockpit]** **Theming System** — A design-token layer that lets the cockpit's whole look be swapped: "Scroll" (sepia, parchment) is the first theme, a red/black Ravenloft theme is the motivating second, and users must be able to author their own. Load-bearing part is the token architecture, not the themes — if components hardcode colors, user themes become impossible to retrofit. Dungeon and map rendering must read from the same tokens as the UI chrome. Includes an "ALL RED" conformance theme — a deliberately hideous theme that turns every token red, used to catch any component that skipped the token layer.
  Surfaced: project kickoff, 2026-07-12. Scroll theme first ships in docs/dice-roller/001-mvp-spec.md.

## Features

- **[must-have | defined]** **Dice Roller** — One roll engine behind three front doors (DM builder, stat-block click, MCP tool), one attributed log, every result auditable. Purely procedural; the first vertical slice, and the walking skeleton for the whole product. MVP covers the DM builder only.
  Surfaced: project kickoff, 2026-07-12. See: docs/dice-roller/vision.md, docs/dice-roller/001-mvp-spec.md.

- **[must-have | concept | blocked: Human-in-the-Loop Approval Gate]** **Narrative Beat** — Generative story-beat proposals the DM must accept before they lock into campaign canon.
  Surfaced: project kickoff, 2026-07-12.

- **[must-have | concept]** **Dungeon Drawing** — Algorithmic dungeon generation in the one-page-dungeon tradition, rendered in the browser. Procedural, not generative.
  Surfaced: project kickoff, 2026-07-12.

- **[must-have | concept]** **Geography Tracking** — A graph of location nodes and route edges persisted to SQLite; the spatial spine the rest of the campaign hangs off.
  Blocks: Outdoor Adventuring, Faction Tracking
  Surfaced: project kickoff, 2026-07-12.

- **[must-have | concept | blocked: SRD Content Corpus]** **Monster Upscaling & Modification** — Derive variants from SRD base stat blocks (beastly / shaman / archer goblin) with defensible CR math, gated for DM approval.
  Surfaced: project kickoff, 2026-07-12.

- **[must-have | concept | blocked: LLM Provider Abstraction]** **Campaign Tracking** — The world-state system of record: what has happened, which NPCs exist, which areas are defined, what the wider world looks like, and what the story goal is. Read back as planning context, not just written to.
  Blocks: Narrative Beat, NPC Development, Faction Tracking, Session Summarization
  Surfaced: project kickoff, 2026-07-12.

- **[must-have | concept | blocked: Human-in-the-Loop Approval Gate]** **NPC Development** — Generative NPC creation and deepening, gated for DM approval, filed into campaign canon on accept.
  Surfaced: project kickoff, 2026-07-12.

- **[probably-need | concept | blocked: Geography Tracking]** **Faction Tracking** — Thematically grouped monster/NPC factions bound to areas, with dynamic inter-faction relationships that feed narrative and populate the next encounter space.
  Surfaced: project kickoff, 2026-07-12.

- **[probably-need | concept | blocked: Campaign Tracking]** **Session Summarization** — Interviews the DM about what happened at the table and files the durable parts for later retrieval. May be absorbed into Campaign Tracking.
  Surfaced: project kickoff, 2026-07-12.

- **[probably-need | concept | blocked: SRD Content Corpus]** **Equipment & Artifact Creation** — Generate or roll items; procedural or generative depending on what is being made.
  Surfaced: project kickoff, 2026-07-12.

- **[probably-need | concept | blocked: Geography Tracking]** **Outdoor Adventuring** — Overland travel and wilderness play. Shape undecided — likely a mix of algorithmic generation and LLM narration; the split is an open design question.
  Surfaced: project kickoff, 2026-07-12.

- **[probably-need | concept | blocked: Dice Roller]** **Encounter Resolution** — Combat and encounter state (initiative, positions, HP, action economy), with optional LLM tactical suggestions for the DM. The first real consumer of the roll engine; explicitly *not* owned by the Dice Roller.
  Surfaced: dice-roller spec, 2026-07-12. See: docs/dice-roller/vision.md.

- **[probably-need | concept]** **Random Tables** — Roll on a table (d100 → outcome) and map the result. Dice-adjacent, but likely owned by whatever system owns the table rather than by the Dice Roller. Open question in the dice-roller vision.
  Surfaced: dice-roller spec, 2026-07-12. See: docs/dice-roller/vision.md.

- **[cool-if | concept | blocked: Local Web Cockpit]** **Player Portal** — A player-facing surface separate from the DM's cockpit. Currently hypothetical, but it is the reason the hidden/shown visibility flag exists in the roll model from day one.
  Surfaced: dice-roller spec, 2026-07-12. See: docs/dice-roller/vision.md.

- **[cool-if | concept]** **Autonomous DM Mode** — The north star: the assistant runs the game itself rather than assisting a human DM. Not a near-term build, but it is *why* procedural grounding and roll attribution are non-negotiable — an LLM that can roll off the books can never be trusted to run a fight.
  Surfaced: dice-roller spec, 2026-07-12. See: docs/dice-roller/vision.md.
