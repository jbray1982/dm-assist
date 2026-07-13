# SRD Content Corpus — Vision

## Vision

The corpus is the assistant's ground truth. Every feature that claims to be *grounded* —
monster upscaling with defensible CR math, stat-block rolls, item generation that derives
from real item rules — is grounded *in something*, and that something is this: the D&D 5e
System Reference Document, ingested as a fully structured domain model rather than a pile
of searchable text. A monster is not a paragraph that mentions an AC; it is a record whose
AC is a number, whose hit points are a dice expression the roll engine can evaluate, and
whose attacks will eventually be objects the engine can execute.

The target source is **SRD 5.2.1** — the 2024-rules revision, released under Creative
Commons Attribution 4.0. CC-BY-4.0 is irrevocable, which makes the corpus legally durable,
and it permits transformation, which makes structured ingestion legitimate rather than
gray-area scraping. The corpus is *multi-corpus by architecture but single-corpus by
intent*: every record belongs to a named corpus, so a future source (SRD 5.1, a
third-party CC ruleset) is an additive ingest rather than a schema migration — but no
second corpus is on the roadmap until a concrete use case demands one.

The second load-bearing idea: the corpus is not read-only reference data. It is the single
home for everything stat-block-shaped. An upscaled goblin shaman approved through the
HITL gate is a first-class monster with `provenance: derived` and a `parent_id` pointing
at the SRD goblin; a DM's homebrew horror is a first-class monster with
`provenance: homebrew`. One schema, one query surface — provenance is a filter, not a wall.

## User Experience

The DM opens the compendium and it feels like the Monster Manual with a search box:
browse monsters by CR and type, spells by level and class, filter, click through to a
stat block rendered in the cockpit's theme. From the stat block, the dice roller's second
front door opens — click the goblin's scimitar and the roll happens with the goblin's
own attack bonus, attributed and logged like every other roll.

The LLM sees the same corpus through MCP tools: typed lookups (`get_monster`,
`search_spells`) when it knows what it wants, and semantic/full-text search over rules
prose when it doesn't ("how does underwater combat work?"). Generation follows its nose
through real rules instead of inventing plausible ones.

Derived and homebrew content sits beside SRD content everywhere — same search, same stat
block view, same MCP tools — distinguishable when the DM cares, invisible when they don't.

## Mechanics & Systems

- **Structured domain model in SQLite.** Monsters, spells, items, and rules sections as
  typed EF Core entities. Numbers are numbers, dice are dice-grammar expressions, prose
  is prose. This is the first persistent store in the product.
- **Corpus identity.** Every record carries a `corpus_id`. The `Corpus` table owns the
  source's name, version, license, and required attribution text. Ships with exactly one
  row: `srd-5.2.1`.
- **Provenance.** `source | derived | homebrew` on every record, plus a nullable
  `parent_id` for derivation chains. Only `source` is written by ingestion; the other two
  are reserved for Monster Upscaling and homebrew authoring.
- **Ingestion pipeline, not a one-off.** A re-runnable C# tool parses a pinned,
  repo-vendored snapshot of a community CC-BY-4.0 markdown conversion of SRD 5.2.1
  (the official PDF is spot-check ground truth only — PDF extraction is a tar pit).
  Idempotent re-ingest handles errata releases.
- **Two consumption modes.** Typed queries for procedural math and structured lookups;
  chunked, searchable rules prose (FTS or embeddings — undecided) for generative
  grounding. Structure where math computes, text where language lives.
- **MCP exposure.** Corpus tools join the MCP Tool Layer so the LLM queries the same
  engine-visible truth the DM sees. No privileged path, mirroring the dice roller's rule.
- **Executable stat blocks.** Attacks parsed into typed objects (to-hit, reach, damage
  dice, damage type) the roll engine can execute — the bridge between the corpus and the
  dice roller's stat-block front door, and the substrate for Monster Upscaling's CR math.
- **Attribution.** CC-BY-4.0 requires visible attribution; the UI renders the corpus's
  attribution text wherever corpus content is displayed.
- **Theming.** The compendium and stat block views render entirely through design tokens,
  like everything else.

## Open Questions

- Which community markdown conversion to pin, and at what commit? Candidates exist
  (e.g. downfallx/dnd-5e-srd-markdown, complete per its README); the choice needs a
  fidelity spot-check against the official PDF before the MVP ingests it.
- Rules-prose search: SQLite FTS5 is cheap and local; embeddings are better at "what I
  mean, not what I typed" but drag in a model dependency. Which, and when?
- How does a campaign declare its grounding corpus once more than one exists? (Deferred
  with the second corpus itself.)
- Do spells and items share a generic "record" shape or get their own tables? Leaning
  per-type tables — the whole point of the corpus is that types are real.
- Where exactly does attribution live in the UI so it is honest but not obnoxious?
- Does homebrew authoring get its own feature surface, or arrive as a side effect of
  Monster Upscaling's edit path?

## Possible Next Directions

Deliberately unranked — chosen after the MVP is in hand, not before.

- **Executable actions.** Parse attacks into typed objects; wire stat-block-click to the
  roll engine. Proves the corpus grounds real math — the product's central bet — and is
  the direct prerequisite for Monster Upscaling. *(Chosen as the next iteration at spec
  time; see the next-iteration issue.)*
- **MCP lookup tools.** `get_monster` / `search_monsters` as the first corpus MCP tools.
- **Rules prose + search.** Chunked rules chapters with FTS/semantic search.
- **Spells and items.** Same pipeline, new types; feeds Equipment & Artifact Creation.
- **Compendium browser.** The full browse/filter experience over the minimal search view.

## Out of Scope

- **Non-SRD copyrighted content.** No Monster Manual, no adventures, no imports of
  content WotC did not release under CC. The corpus's legal cleanliness is a feature.
- **A second corpus.** The architecture supports it; the roadmap does not target it.
- **Automatic homebrew import formats** (VTT exports, Homebrewery markdown, etc.).
- **Player-facing compendium.** The cockpit is the DM's; a player surface is the Player
  Portal's problem if it ever exists.
- **Editing SRD records.** `source` provenance is immutable; changes happen by deriving.
