# SRD Content Corpus — 001 MVP Spec

## Goal

Stand up the corpus as real infrastructure: a re-runnable ingestion pipeline that parses
a CC-BY-4.0 markdown conversion of SRD 5.2.1 into a structured SQLite database, and a
minimal cockpit surface — search box plus rendered stat block — that lets the DM find any
SRD monster and eyeball the data quality. This is the product's first persistent store
(SQLite + EF Core arrive here) and the proof of the structured-domain-model bet on its
hardest content type. Success condition: every monster in the source is queryable as
typed data, and a DM can search "goblin" and read a correct, themed stat block two
clicks later.

## In Scope

- A vendored snapshot of a community CC-BY-4.0 markdown conversion of SRD 5.2.1,
  committed under `data/srd-5.2.1/` with an `ATTRIBUTION.md` (source repo, commit hash,
  license chain), after a fidelity spot-check against the official PDF.
- A C# ingestion tool (`src/DmAssist.Ingestion`, console app) that parses every monster
  stat block from the snapshot into SQLite via EF Core. Idempotent: re-running replaces
  the corpus's records rather than duplicating them.
- Monster records with **core typed fields** (see Data Model); traits, actions, bonus
  actions, reactions, and legendary actions stored as ordered, named markdown text
  sections.
- `Corpus` / provenance / `parent_id` plumbing, populated with the only values the MVP
  produces (`srd-5.2.1` / `source` / `null`).
- Query API: search monsters by name, fetch one monster with its sections.
- Cockpit UI: a monster search page (debounced search-as-you-type, results showing name,
  CR, size/type) and a stat block detail view, both rendered through design tokens.
- CC-BY attribution text rendered on the corpus UI surface.
- EF Core migrations established as the schema-change mechanism from the first table.

## Out of Scope (for MVP)

- **Attack parsing / executable actions.** Actions are formatted text. Parsing them into
  typed objects the roll engine can execute is the next iteration, and the sections are
  stored to make that a pure addition.
- **Spells, items, rules prose.** Monsters only. The pipeline is shaped so a new content
  type is a new parser plus new tables, not a rework.
- **MCP tools.** Consistent with the dice roller MVP: the query layer the MCP server
  will wrap is built here; the wrapping is not.
- **Semantic or full-text search.** Name search only. FTS/embeddings arrive with rules
  prose.
- **Compendium browse/filter.** No CR sliders, no type facets — search and detail only.
- **`derived` / `homebrew` writes.** The enum values and `parent_id` exist; nothing
  writes them until Monster Upscaling.
- **Stat-block-click rolling.** The stat block view is read-only. (Next iteration.)
- **A second corpus.** One `Corpus` row exists.

## Data Model

First persistent schema in the product. SQLite via EF Core; migrations from day one.

```csharp
record Corpus(
    string Id,                    // "srd-5.2.1" — slug, primary key
    string Name,                  // "D&D 5e SRD 5.2.1"
    string Version,               // "5.2.1"
    string License,               // "CC-BY-4.0"
    string AttributionText,       // full required attribution, rendered verbatim in UI
    string SourceUrl,
    DateTimeOffset IngestedAt);

enum Provenance { Source, Derived, Homebrew }   // only Source is written in MVP

class Monster
{
    Guid Id;
    string CorpusId;              // FK → Corpus
    string Slug;                  // stable natural key; unique per corpus; idempotency anchor
    Provenance Provenance;        // Source in MVP
    Guid? ParentId;               // null in MVP; Monster Upscaling's hook

    string Name;
    string Size;                  // "Tiny".."Gargantuan"
    string TypeLine;              // verbatim: "Medium Humanoid (Goblinoid), Chaotic Neutral"
    string Type;                  // parsed principal type: "humanoid"
    string Alignment;

    int ArmorClass;
    string? ArmorNote;            // "natural armor", "shield" — null when bare
    int HitPointsAverage;
    string HitPointsFormula;      // "2d6+2" — MUST parse under the dice grammar
    int? InitiativeBonus;         // 2024 stat blocks carry Initiative
    string SpeedText;             // "30 ft., fly 60 ft." — structured speed deferred

    int Str; int Dex; int Con; int Int; int Wis; int Cha;
    string? SavesJson;            // {"dex": 4, ...} — only entries the block lists
    string? SkillsJson;           // {"stealth": 6, ...}

    string? Vulnerabilities;      // text in MVP
    string? Resistances;
    string? Immunities;           // 2024 merges damage + condition immunities; keep verbatim
    string? Senses;
    string? Languages;
    string? Gear;                 // 2024 stat blocks may carry a Gear line

    decimal ChallengeRating;      // 0.125 for "1/8" — sortable
    string ChallengeRatingText;   // "1/8" — displayable
    int Xp;

    List<MonsterSection> Sections;
}

enum SectionKind { Trait, Action, BonusAction, Reaction, LegendaryAction }

class MonsterSection
{
    Guid Id;
    Guid MonsterId;
    SectionKind Kind;
    int Order;                    // preserves source ordering within kind
    string Name;                  // "Multiattack", "Scimitar", "Nimble Escape"
    string Text;                  // markdown, verbatim from source
}
```

`HitPointsFormula` parsing under the dice grammar is a hard requirement — it is the first
place the corpus and the dice engine touch, and it catches ingestion garbage cheaply.

## Behaviors

**Ingestion**
- `dotnet run --project src/DmAssist.Ingestion -- --source data/srd-5.2.1 --db <path>`
  parses every monster in the snapshot and writes it inside one transaction.
- Idempotent by `(CorpusId, Slug)`: re-running deletes and rewrites the corpus's monsters.
  `IngestedAt` updates.
- **All-or-nothing, fail loud.** Any stat block that fails to parse or validate aborts the
  run with a report naming each failing monster and why, and a nonzero exit code. No
  silent drops — a monster missing from the corpus is worse than a failed ingest.
- Validation per monster: all core typed fields present and in range (abilities 1–30,
  AC > 0, HP > 0), `HitPointsFormula` parses under the dice grammar,
  `ChallengeRating`/`Xp` consistent with the CR table, at least a name and type.
- End-of-run summary: monsters written, sections written, count compared against a
  configured expected count (source claims ~400; exact number pinned once the snapshot
  is chosen).

**Edge cases the parser must survive** (test fixtures for each):
- Fractional CR: `1/8`, `1/4`, `1/2` → 0.125 / 0.25 / 0.5 with text preserved.
- Swarms: "Medium Swarm of Tiny Beasts" — `Size` is the swarm's size, `TypeLine` verbatim.
- AC with a parenthetical note; HP formula with and without a modifier (`2d8`, `9d8+18`).
- Multiple movement modes in `SpeedText`.
- Legendary actions, spellcasting traits, and multi-paragraph sections (kept as markdown).
- Unicode debris from the PDF lineage (em dashes, minus vs hyphen, smart quotes) —
  normalized deliberately, not accidentally.

**Query API**
- `GET /api/monsters?search=gob` — case-insensitive substring match on `Name`; returns
  id, name, CR text, size, type. Empty search returns an empty list (not the whole
  corpus); results ordered by name.
- `GET /api/monsters/{id}` — full record with sections, ordered by `Kind` then `Order`.
- Unknown id → 404.

**UI**
- Search page: debounced input, result rows (name, CR, size/type), click-through to
  detail. No results state says so plainly.
- Stat block view: renders the classic stat block shape — name, type line, AC/HP/speed,
  ability table, the optional lines only when present, then sections grouped under
  Traits / Actions / Bonus Actions / Reactions / Legendary Actions headings in source
  order. Section markdown renders formatted.
- Corpus attribution text visible on the corpus surface (persistent footer on the
  search/detail pages).
- Every visual property resolves through design tokens; the ALL RED conformance theme
  must catch nothing here.

## Integration Points

- **SQLite + EF Core enter the product.** `DmAssist.Api` gains its first `DbContext`;
  migrations become part of the workflow. Later persistent features (Campaign Tracking,
  Geography) build on the pattern established here.
- **Dice grammar as validator.** Ingestion references the `DmAssist.Dice` parser to
  validate `HitPointsFormula` — corpus and engine meet for the first time, read-only.
- **`web/` cockpit.** The monster search and stat block views join the existing Vite
  app beside the dice roller, consuming the same design-token layer and Scroll theme.
- **HTTP boundary.** Same JSON/HTTP conventions as `/api/rolls`. No streaming — this is
  procedural content.

## Extension Hooks

Wired now, cheap now, miserable to retrofit:

- **`CorpusId` on every record.** A second corpus is an ingest plus a `Corpus` row —
  no migration. Nothing in the MVP may assume a single corpus except UI copy.
- **`Provenance` + `ParentId`.** Monster Upscaling writes `Derived` records through the
  approval gate; homebrew writes `Homebrew`. The query surface already returns them the
  day they exist.
- **Sections stored verbatim.** The next iteration parses `Action` sections into typed
  attacks; the markdown text is never discarded, so parsing is additive and reversible.
- **Per-content-type pipeline.** The ingestion tool's structure (snapshot → parser →
  validator → writer) is per-type; spells and items are new parsers into new tables,
  reusing corpus/provenance plumbing.
- **Search endpoint shape.** `?search=` is the first query parameter, not the last —
  CR/type filters extend the same endpoint rather than spawning a second one.
- **Ingestion as a referenced library.** The parser lives in a class library the console
  app wraps, so tests (and any future re-ingest-from-UI affordance) drive it directly.

## Acceptance Criteria

- [ ] `data/srd-5.2.1/` contains the pinned markdown snapshot and `ATTRIBUTION.md`
      naming the source repo, commit, and CC-BY-4.0 attribution chain.
- [ ] Running the ingestion tool against the snapshot succeeds and writes every monster
      in the source (exact count pinned and asserted).
- [ ] Re-running ingestion produces the same database state — no duplicates.
- [ ] A deliberately corrupted stat block aborts the whole ingest with a report naming
      the monster, and leaves the database unchanged.
- [ ] Every ingested `HitPointsFormula` parses under the dice grammar; a test proves the
      validator rejects garbage (`"9 hit points"`).
- [ ] Fractional CRs store as correct decimals with display text preserved.
- [ ] Spot-check fixtures: a swarm, a legendary monster, a spellcaster, and a CR 1/8
      monster all round-trip with correct typed fields and ordered sections.
- [ ] `GET /api/monsters?search=gob` returns goblin-family monsters; empty search
      returns an empty list; unknown id returns 404.
- [ ] The DM can type in the search box and click through to a rendered stat block
      showing every populated field and all sections under the right headings.
- [ ] Attribution text is visible on the corpus UI surface.
- [ ] All corpus UI renders through design tokens (ALL RED theme finds nothing).
- [ ] Every record carries `CorpusId = "srd-5.2.1"`, `Provenance = Source`,
      `ParentId = null`.
- [ ] `dotnet build`, `dotnet test`, and the frontend build all succeed; parser edge-case
      fixtures are part of the test suite.
