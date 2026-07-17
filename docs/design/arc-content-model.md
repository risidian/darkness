# Design Proposal: Expandable, i18n-Ready Narrative/Content Model

> **Status: PROPOSAL — pending product-owner approval.** Do not implement until the
> content reconciliation (see Risks §1) is resolved and the approach is signed off.
> Target: ship Arc 1 (10 beats, ~8–10h) in English now; Arcs 2–10 as drop-in data;
> translations addable without re-authoring. See [ROADMAP.md](../ROADMAP.md) Phase 3.

## Manager's recommended decisions on the open questions
- **Catalog storage:** in-memory dictionary (small, read-only) — not a LiteDB collection.
- **i18n ownership:** domain-owned (in `Darkness.Core`, validated by `Darkness.Tests`) — not Godot's built-in `tr()`.
- **Item/Skill name keys:** add the nullable `NameKey`/`DescriptionKey` fields now for readiness, but defer actually keying item/skill names until translation is pursued (English-only v1).
- **Key stability:** derive keys from `{chainId}/{stepId}`; freeze ids once translation begins; provide an explicit-key override for volatile/shared copy.
- **Locale switching:** restart-to-apply for v1 (no live hot-swap).
- **Content reconciliation (Story-Flow vs JSON):** BLOCKING — needs product-owner input before build.

---

## 0. What exists today

**Content graph (data-driven, JSON → LiteDB).** A quest is a `QuestChain` (`Id`, `Title`, `IsMainStory`, `SortOrder`, `Prerequisites: List<string>`, `Steps`). Each `QuestStep` (`Id`, `Type` enum {Dialogue, Combat, Location, Branch, Travel, Stealth}, `NextStepId`, optional `Dialogue`/`Combat`/`Location`/`Branch`/`Visuals`, `AutoTransition`, `Requirements: List<BranchCondition>`, `Rewards: List<QuestReward>`). Branches are `BranchData.Options: List<BranchOption>` where each option has `Text`, `NextStepId`, `MoralityImpact`, optional `Conditions`.

**Seeding.** `Global.cs` calls `new QuestSeeder(fs).Seed(db)`. `QuestSeeder` reads every `assets/data/quests/*.json`, deserializes into `QuestChain` (case-insensitive + `JsonStringEnumConverter`), `Upsert`s into `quest_chains` keyed by `Id`, and deletes orphans. JSON files are the source of truth; the DB is a rebuilt cache.

**Runtime.** `QuestService.GetAvailableChains` filters by completed/blocked and `Prerequisites.All(p => completedIds.Contains(p))`, ordered by `SortOrder`. `IsMainStoryComplete` checks all `IsMainStory` chains completed. `ConditionEvaluator` supports `morality`, `class`, `has_item` (by `Item.Name`), `quest_completed` (chain id), `world_flag`.

**Player-facing text is 100% inline English today.** `QuestChain.Title`, `DialogueData.Speaker`/`Lines`, `BranchOption.Text` are raw English. `WorldScene.cs` reads them directly. Item/Skill display names double as identity + FK (`Item.Name` is both label and reward/`has_item` key) — matters for i18n (§2.4).

**Validation.** `QuestJsonRoundTripTests` asserts every file deserializes. `QuestGraphAudit` walks chains checking dangling `NextStepId`/branch targets/empty branches — **but only writes a report; it never asserts, so a broken graph passes CI** (gap to fix).

**Story scope discrepancy (flagged).** `Story-Flow.md`: Arc 1 = 10 beats; Arcs 2–10 titled only. JSON has only 9 beat files whose titles/order do not match the doc (e.g. doc beat 2 = taverns, JSON `beat_2` = dark_warrior; doc's 10th "Undead Army" maps ambiguously to `beat_8`/`beat_9`). Needs product-owner reconciliation before content lock.

---

## 1. Arc-level model (layered on existing QuestChain)

### 1.1 New type `Arc` — additive, no breaking change
```csharp
// Darkness.Core/Models/Arc.cs
public class Arc
{
    public string Id { get; set; } = string.Empty;          // "arc_1"
    public string TitleKey { get; set; } = string.Empty;    // loc key -> "Darkness: Revenge"
    public int Order { get; set; }                           // 1..10, arc sequence
    public List<string> Prerequisites { get; set; } = new(); // arc ids (DAG)
    public List<string> MainChainIds { get; set; } = new();  // critical-path beats, in order
    public List<string> SideChainIds { get; set; } = new();  // optional content (see §4)
}
```
`Arc` references chains **by id** rather than nesting — a beat joins an arc by being listed in `MainChainIds`, no restructuring of existing per-beat JSON. Arc→Chain→Step is a reference graph, not physical nesting.

### 1.2 One additive field on `QuestChain`
```csharp
public string? ArcId { get; set; }   // nullable; null = un-arced (e.g. exploration_demo)
```
Nullable ⇒ every current JSON still deserializes unchanged. Authoritative membership is the Arc's lists; `ArcId` is a denormalized convenience; an audit asserts the two agree.

### 1.3 New seeder + collection
`ArcSeeder` mirrors `QuestSeeder`: read `assets/data/arcs/*.json` → `arcs` collection, delete orphans, `EnsureIndex(a => a.Id)`. Registered in `Global.cs` just before `QuestSeeder`.

### 1.4 Arc-aware availability (additive to `IQuestService`)
```csharp
List<Arc> GetAvailableArcs(Character character);
bool IsArcComplete(Character character, string arcId);   // all MainChainIds completed
Arc? GetCurrentArc(Character character);
```
Side chains never block arc completion. Existing chain gating unchanged.

### 1.5 Arcs 2–10 become drop-in JSON
Drop in `arcs/arc_N.json` + N `quests/beat_*.json` (ids listed in the arc, `Prerequisites` chaining beats, `ArcId: "arc_N"`). Both seeders sync on launch. No engine code, no recompilation. Cross-arc wiring reuses the existing `Prerequisites` DAG pattern.

---

## 2. i18n-readiness

### 2.1 Principle
Separate *identity* (stable key) from *display* (localized string). Non-display ids (`Id`, `NextStepId`, `ArcId`, `world_flag`, `SpriteKey`, item-name FKs) stay as-is.

### 2.2 Authoring ergonomics: write English inline, extract keys with a tool
Authors keep writing raw English inline (as today). A `LocExtract` tool (console app in a new `Darkness.Tools` project, or a test-gated generator) derives a **deterministic key** per string from its structural path:
- `quest.{chainId}.title`, `quest.{chainId}.{stepId}.speaker`, `quest.{chainId}.{stepId}.line.{index}`, `quest.{chainId}.{stepId}.opt.{optionIndex}.text`

It emits/merges `assets/data/i18n/en/quests.json = { key: "English string" }`. Inline English stays in the quest JSON as the default-locale fallback. Optional explicit key override (`TextKey`/`LineKeys`) for reused copy; absent ⇒ derived key.

### 2.3 Storage + lookup
- Disk: `assets/data/i18n/{locale}/{quests,arcs,items,skills}.json`. English only for v1.
- Loaded into an in-memory dictionary cached in the service (recommended; catalogs are small).
```csharp
public interface ILocalizationService
{
    string Resolve(string key, string? fallback = null);   // fallback = inline English
    string Locale { get; }
    void SetLocale(string locale);
}
```
`WorldScene` resolves `line = _loc.Resolve(DeriveLineKey(chain, step, i), step.Dialogue.Lines[i])`. Inline English as fallback ⇒ game renders correctly even with no catalog (de-risks rollout).

### 2.4 Item/Skill names
Do not rename `Item.Name`/`Skill.Name` (identity + FK). Add optional `NameKey`/`DescriptionKey`; display sites resolve `NameKey` with `Name` fallback; reward/condition matching keeps using `Name`. Zero migration.

### 2.5 Fallback chain
`Resolve(locale.key)` → `en.key` → inline `fallback` → key literal (QA sentinel). Missing keys logged once.

---

## 3. Authoring + validation pipeline
- **A. Promote `QuestGraphAudit` to a failing test** (currently only writes a report): assert all `NextStepId`/branch targets resolve, no empty branches, valid operators, reachability from `Steps[0]`, no soft-lock terminal path.
- **B. New `ArcGraphAudit`:** arc↔chain id consistency (bidirectional), `Arc`/`QuestChain` prerequisites form a DAG (topo-sort, no cycles, ids exist), unique `SortOrder`/`Order`, all `IsMainStory` chains reachable via some arc.
- **C. New `LocalizationAudit`:** every derived/override key exists in `en`; reverse orphan check (warning); `en` complete, other locales report coverage %.
- **D. Round-trip** extended to `Arc`.
- **Author loop:** drop JSON → run `LocExtract` → `dotnet test --filter Category=Audit` → green = wired, non-dangling, DAG-valid, fully localizable. Also delete the `.bak` files in the content dir.

---

## 4. Reaching 8–10 hours without bloating the critical path
- **Critical path = 10 main beats** (`IsMainStory: true`, `MainChainIds`); keep tight; completion depends only on these.
- **Side/optional chains** (`IsMainStory: false`, `SideChainIds`) gated by `Prerequisites` on a main beat — where most of the 8–10h lives; excluded from completion.
- **Exploration/hub beats** (`Type: Travel`/`Location` + `Visuals.Zones`, as in `exploration_demo.json`) as connective tissue.
- **Repeatable encounters** via existing `EncounterTable`/`DeathmatchEncounter`, kept out of `quest_chains`.
- Rough budget: 10 beats @ ~15–25 min ≈ 3–4h spine; 6–8 side chains + 2–3 hubs + encounters ≈ 4–6h. Playtime scales by adding data, never by lengthening the spine.

---

## 5. Migration / rollout (phased, low-risk, back-compat)
- **Phase A — Reconcile content (product owner):** resolve Story-Flow vs JSON beat mismatch; confirm canonical Arc 1 beat list; author beat 10; purge `.bak`. *No code.*
- **Phase B — Additive model:** add `Arc.cs`, `QuestChain.ArcId?`, optional key fields; build `ILocalizationService`/`LocalizationSeeder` + `LocExtract`. Resolver returns inline-English fallback ⇒ identical runtime behavior, zero visible change.
- **Phase C — Seeders + service:** register `ArcSeeder`/`LocalizationSeeder`; add arc methods; author `arcs/arc_1.json`.
- **Phase D — Migrate content:** run `LocExtract` → `en/*.json`; stamp `ArcId` on beats; point display sites at `Resolve(key, fallback)`.
- **Phase E — Validation hardening:** land the promoted `QuestGraphAudit` + `ArcGraphAudit` + `LocalizationAudit` + Arc round-trip; gate CI on `Category=Audit`.
- **Phase F — Arc 2 as proof:** add Arc 2 purely as data to prove no engine work needed.

### Risks & open questions for the product owner
1. **Content reconciliation (BLOCKING):** which is canonical — `Story-Flow.md` (10 beats, one ordering) or the 9 JSON files (different ordering/titles)? Beat 10 must be authored.
2. Key stability vs step renames — id-freeze after translation, or explicit-key override for volatile copy?
3. Catalog storage — in-memory (recommended) vs LiteDB collection?
4. Item/Skill i18n scope for v1 — defer keying (recommended) vs do now?
5. Domain-owned i18n (recommended) vs Godot-native `tr()`?
6. Runtime locale switch — restart-to-apply (recommended) vs live.

### Critical files for implementation
- `Darkness.Core/Models/QuestChain.cs` (add `ArcId?`; new `Arc.cs`)
- `Darkness.Core/Services/QuestSeeder.cs` (template for `ArcSeeder` + `LocalizationSeeder`)
- `Darkness.Core/Services/QuestService.cs` (arc methods; add to `IQuestService`)
- `Darkness.Godot/src/Game/WorldScene.cs` (route text through `ILocalizationService.Resolve`)
- `Darkness.Tests/Audit/QuestGraphAudit.cs` (promote to asserting; basis for new audits)
