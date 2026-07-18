# Darkness: Reforged — Action Items & Open Questions

> Live backlog maintained by the lead (Jeff) while executing autonomously. Two halves:
> **Open Questions** need the product owner; **Action Items** are the execution queue.
> Strategy/phasing lives in [ROADMAP.md](./ROADMAP.md); this is the running to-do + decision log.
> Last updated: 2026-07-18.

---

## 🟠 Open Questions / Decisions (need product owner — non-blocking; I proceed on documented defaults)

### 🔴 Bugs (found by the headless boot smoke test — fixing now)
- [ ] **SkillSeeder seeds 0 skills** — missing `JsonStringEnumConverter`; `skills.json` string enums throw, `catch` swallows, skills collection empty. Add the converter + regression test.
- [ ] **AppearanceSeeder duplicates `class_defaults` every boot** — `ClassDefault.Id=0` → `Upsert` inserts new rows each run. Fix upsert-by-name (or clear+insert) + regression test.

### Narrative canon (Arc 1) — reconciled with full Ch1–10 prose; see `docs/design/arc-1-revision-brief.md`
**DECIDED (product owner, 2026-07-18) — drive the pending Arc-1 revision pass:**
- [x] **Protagonist** — keep PC as lead (inherits the amnesiac/Atriartous prophecy arc); Darius stays companion. (Divergence from prose, confirmed.)
- [x] **Arc-1 ending** — re-end at the **black-castle gate gauntlet** (skeleton horde → Kyarias → Arthoros → Balgathor admits the party); replaces the invented undead-army finale; sets up Arc 2.
- [x] **Tywin** — keep as a **permanent party ally** from beat 9 (divergence from prose departure).
- [x] **Boss names** — Balgathor→**Dariartimous** (beat-4 cliff villain); Araknos→**Tauros** (bull).
- [x] **Arc 2 green-lit** — Ch4–10 "Castle Horror interior" gets its own spec pass later.

**Still open (smaller):**
- [ ] **Sorcerer (beat 5)** — prose: unnamed, unwinnable, leaves by choice. Game: "Risidian", winnable. Align or keep?
- [ ] **Weapon** — adopt Darius's black greatsword (prose) as his signature?
- [ ] **Beat-2 tavern → Bria's Colosseum shelter** (no tavern in prose); **beat-7 goblins → bandits**.
- [ ] **Source authority** — PDF numbering broken (no "Ch3", two "Ch4"s; Journey compressed) vs the fuller older Ch1–3 txt. Which wins where they differ?

**Earlier canon items (still open):**
- [ ] **Araknos design** — prose says a **bull/Tauros** demon; existing art is a hissing spider. Which is canon? *(Default in draft: bull, name kept "Araknos".)*
- [ ] **Kyarias vs `KazarthEndGame` sprite** — final boss named Kyarias but uses the `bosses/KazarthEndGame` asset. Rename boss, or re-point to a Kyarias asset?
- [ ] **Balgathor → Dariartimous?** — the cliff-duel villain is `Balgathor` in-game but is the prose's "Dariartimous, Seeker of Darkness." Rename to preserve the Daverak/Dariartimous/Atriartous/Darius name-web reveal?
- [ ] **Protagonist arc (confirm the default)** — PC (player-created) inherits the amnesiac-hero + Atriartous-prophecy arc; Darius is the companion. *(Assumed; the story's spine — flag if wrong.)*
- [ ] **Content review** — skim `Darkness.Godot/assets/data/quests/beat_1..beat_10.json`; it's your story, adjust anything off.
- [ ] **Dialogue structure** — a few beats pack two speakers into one bubble via inline `Darius:`/`Tywin:` prefixes. Keep, or split into one-speaker-per-step?
- [ ] **Defeat rule** — battles now end in defeat only when the WHOLE party is down (the PC can fall if Darius/Tywin survive, and they fight on). Confirm, or make **PC death = immediate game over**?

### Monetization / economy (needed for Phase 2)
- [ ] **Economy numbers** — currency names (soft/hard), IAP tiers & price points, rewarded-ad material grants + daily cap. *(I can draft a proposal for sign-off.)*

### Assets (needed for visual completeness)
- [ ] **Missing backdrops** — `cliff` (beats 4/9), `road` (beat 8), `throne_room` (beats 6/10) have no art → placeholder color. Create art, or approve remap to existing (`camelot_shore`/`dark_castle`/`forest`/`tavern`)?
- [ ] **Enemy sprites** — goblins/skeletons/zombies + Daverak use placeholder sheets. Create, or accept placeholders for v1?

### Ops / release (needed for Phases 1/7)
- [ ] **Final store title** — "Darkness: Reforged" is a working title; confirm + trademark sanity check.
- [ ] **Team velocity** — to convert roadmap phase-sizes into a dated schedule.
- [ ] **Firebase/Play/AdMob accounts** — to be created under `risidiangames@gmail.com` when Phase 1 starts (Play Console = $25 one-time).

### Tech (I resolve empirically; recording verdicts)
- [ ] **gdUnit4 on .NET 10** — does `gdUnit4.api` build on net10, or does the scene-test project need net9? *(Being determined now.)*

---

## 🟢 Action Items — execution queue (me, autonomous)

### Done
- [x] **GdUnit4 headless local test loop working** — `scripts/run-godot-tests.sh`, net10.0, 2/2 proven (`845f04919`). I can now compile + scene-test Godot code locally instead of pushing to CI.

### Done (recent)
- [x] **`BattleScene` multi-party/ally wiring** (`3596eca82`) — `PartyFactory` + `PartyBattleController`; ally-AI turns; compile-verified locally. In-editor visual QA pending.

### In flight
- [ ] Talent subclass trees — authoring the 16 trees from `docs/design/talent-subclass-trees.md` (+ tier-3 gate fix).

### Next
- [ ] **Extend test harness to real game scenes** — load scenes needing the `/root/Global` autoload (DI + LiteDB + seeders) so BattleScene etc. are scene-testable headless. Deferred in favor of extract-to-Core + compile-check; revisit if scene behavior needs direct coverage.
- [ ] Flip `godot-tests.yml` CI trigger to push/PR once the `gdUnit4-action` CI path is validated (local loop is the primary; CI is a bonus).
- [ ] Add a GdUnit4 scene test for the party/ally battle flow (needs the game-scene harness above).

### Content & systems depth (Phase 4)
- [ ] Wardrobe batches: armor/legs/off-hand staves/shields (weapons done).
- [ ] Talent subclass trees — flesh out the 16 one-node stubs. **Design ready** → `docs/design/talent-subclass-trees.md`. Includes a pre-existing fix: lower tier-3 `SpentInTree` gate 20→5 (else subclass end-game trees are unreachable). Flavor stat keys (HolyDamage/Healing/etc.) still not wired into formulas — separate item.
- [ ] Items/recipes/skills expansion + tag skills by class (all `ClassRequirement` null today).
- [ ] Enemy variety + combat balance pass (leverage `CombatSim`).
- [ ] Author real content for placeholder-heavy beats once art/canon land.

### Narrative model / i18n
- [ ] `LocExtract` tool — extract inline English → keyed `assets/data/i18n/en/*.json`; switch content to keys (translation-ready). Add `LocalizationAudit`.
- [ ] Wire `WorldScene`/`BattleScene` display sites through `ILocalizationService.Resolve(key, fallback, tokens)` incl. `{playerName}`.

### Foundation cleanup (deferred from Phase 0)
- [ ] `async void` fire-and-forget audit in `Darkness.Godot/src` (surface unobserved exceptions).
- [ ] Codify the local Godot build env (the `.godot`→/tmp symlink) into a committed dev script/README so it's reproducible.

### Backend / monetization (Phases 1–2, after economy decisions)
- [ ] Firebase Auth + cloud-save blob sync (offline-first) behind Core interfaces; retire the WebAPI scaffold.
- [ ] Play Billing IAP + server receipt validation; AdMob rewarded ads; Firebase Analytics/Crashlytics.

### Release (Phase 7)
- [ ] Signed release AAB, adaptive icons, store listing, privacy policy, data-safety, closed→open beta.

---

## ✅ Recently done (see ROADMAP.md + git log for detail)
Phase 0 (CI, hygiene, LF, flaky-fix) · all v1 product decisions · expandable Arc model + i18n service · Arc 1 authored + graph audits · `PartyBattleController` + `CombatData.Allies` · weapon wardrobe (5→24) + `SheetDefinitionAudit` · local Godot build + headless run proven.
