# Darkness: Reforged — Road to Market (v1)

> **"Darkness: Reforged" is a working title** — final store name TBD before launch (Phase 7). App id currently `com.risidian.darkness`.

**Target for v1** (confirmed with product owner, 2026-07-17):
- **Platform:** Android (Google Play). No iOS/Steam/Web for v1.
- **Monetization:** Free-to-play, **convenience-only at this stage** — optional rewarded ads grant materials; IAP sells materials/currency to skip the grind. No directly-purchasable power, **no loot boxes/gacha**.
- **Online:** **Accounts + cloud saves** (no real-time PVP). Game must stay fully playable offline and sync when online.
- **Scope:** **Full Arc 1 commercial release** (Story-Flow Arc 1 = **beats 1–10**, realised to shippable depth), targeting **~8–10 hours of playtime**. The story is already written/designed well beyond the repo — Story-Flow.md specs all 10 beats of Arc 1, and Arcs 2–10 ("Castle of Horror" … "Daverak") are titled and written. **The content model must be built to expand** so those arcs slot in as post-launch updates without rework — v1 ships Arc 1, the architecture holds the whole saga.
- **Audience / rating:** targeting **Teen (13+)** — dark fantasy RPG.
- **Localization:** **English-only for v1**, but the content model is built **i18n-ready** (externalized text keys) so translations can be added without rework.

Effort sizes are relative (S / M / L / XL), not calendar dates — dates depend on team velocity. Ask the product owner for capacity to timebox.

---

## Where we are today (grounded snapshot)

| Dimension | State | Verdict |
|-----------|-------|---------|
| **Engine & code** | Clean, low-debt; all 24 scenes DI-wired; combat/quest/talent/skill/leveling/encounter systems functional | ✅ **Solid** — playable vertical slice |
| **Story content** | Arc 1 (10 beats) is **written/designed** in Story-Flow.md + beyond the repo, but only ~21 thin steps are authored into JSON (mostly 2-step dialogue→combat; only beat_1 branches; beat_4 a stub). Beat 10 (Kyarias undead-army finale) not yet a full chapter. | 🟠 **Designed, under-authored** — work is authoring throughput + an expandable model, not greenfield writing |
| **RPG data** | 22 skills, 54 talent nodes (16 subclasses are 1-node stubs), 29 items, 12 recipes, 20 levels, 16 enemies, 8 arenas | 🟠 **Thin–adequate** |
| **Art assets** | 4,759 LPC sprite PNGs (full wardrobe + 12 boss sprites) but only **21 equipment sheet_definitions wired in** | 🟡 **Rich art, thin wiring** |
| **Audio** | Zero audio files, zero audio code (volume settings exist but nothing plays) | 🔴 **Absent** |
| **Backend** | WebAPI orphaned: only `ValuesController`, no auth, no migrations, plaintext password field, hardcoded personal connection string, referenced by nothing | 🔴 **Stub / net-new** |
| **Monetization** | No IAP, no ads SDK, no economy hardening, no analytics | 🔴 **Missing** |
| **Release/ops** | Android debug-only export, empty keystores, no signed AAB, no adaptive icons, no store assets, no build/test CI, no crash reporting | 🔴 **Missing** |
| **Test coverage** | Broad on Core/services; **Godot scene layer + input untested** (5 excluded test files incl. BattleScene); 41 `async void` fire-and-forget handlers | 🟠 **Partial + instability risk** |
| **Repo hygiene** | Committed logs/`.bak`/`.jar`/binlog/generated sprite sheets; weak `.gitignore` | 🔴 **Poor** |

**One-line summary:** strong engine and a full art library sitting on top of a thin, offline prototype. The story is *written* but only lightly *authored* into the data model. The three long poles are **authoring the designed story into an expandable content model**, **the entire online + monetization backend (net-new)**, and **audio (from zero)**.

---

## Critical path & parallelism

```
Phase 0 (Foundation)  ──►  Phase 1 (Backend/Accounts)  ──►  Phase 2 (Monetization)  ──►  Phase 7 (Release)  ──►  Phase 8 (Launch)
      │
      └── run in parallel (data-driven, offline) ──►  Phase 3 (Story), Phase 4 (Systems depth), Phase 5 (Audio), Phase 6 (Polish)
```
Content and audio streams don't depend on the backend and should run concurrently with Phases 1–2. Release (7) needs 1, 2, and the content/audio/polish streams complete.

---

## Phase 0 — Foundation & Stabilisation  *(size: M — do first)*  — ✅ MOSTLY DONE (2026-07-17)
**Done:** repo hygiene + `.gitignore` (`c94bd980d`); GitHub Actions CI building + testing on push (`c7b7c4a1f`); dead MonoGame test files removed + `SkillFilter` extracted to Core; repo-wide CRLF→LF normalization + `.gitattributes` (`fa20498ff`); flaky LiteDB `BsonMapper.Global` race fixed — CI now *reliably* green, 14 consecutive clean runs (`8a7f33db9`).
**Deferred (lower-priority robustness, batch later):** the `async void` fire-and-forget stability audit; a scene-layer (Godot) test harness for BattleScene/WorldScene; minor — `CombatSim` writes to a tracked doc on every run (should target a gitignored path).

Everything downstream rides on a stable, tested, automatable base.
- **Repo hygiene:** extend `.gitignore` (`bin/`, `obj/`, `*.binlog`, `*.bak`, capture logs, generated sheets); purge committed debris (`exception.txt*`, `godot_*_captured*.txt`, `classes.jar`, `msbuild.binlog`, `Story-Flow.md.bak`, quest `.json.bak`); move `GeneratedSpriteSheets/` + `SpriteSheets/` out of the repo or under a build-output path.
- **CI:** GitHub Actions to `dotnet build` + `dotnet test Darkness.Tests -p:ParallelizeTestCollections=false` on every push/PR; gate merges on green. (Today the only workflow is the Gemini auto-code bot — no build/test pipeline.)
- **Test debt:** port/rewrite the 5 excluded, MonoGame-era tests to Godot (`BattleScene`, `SceneDisposal`, `Input`, `Skills`, `SpriteCompositor`); stand up a scene-layer test harness — BattleScene (1,299 lines) and WorldScene (1,105 lines) are the most complex and least covered.
- **Stability:** audit the 41 `async void` fire-and-forget handlers; add exception surfacing so unobserved async failures stop being silent crashes.
- **Exit criteria:** clean tree, green CI on push, excluded tests re-enabled, no silent async failures.

## Phase 1 — Backend, Accounts & Cloud Saves  *(size: L–XL — long pole, net-new)*
**Direction: Firebase** (see Decisions). This collapses most of the original build-it-yourself scope — auth, hosting, and storage are managed.
- **Retire the WebAPI scaffold:** the ASP.NET Core `Darkness.WebAPI` (orphaned, plaintext `Users.Password`, no migrations) is dropped rather than built out. If PlayFab is chosen instead, revisit.
- **Auth:** Firebase Authentication for registration/login/reset (email + Google sign-in). No plaintext passwords — Firebase handles credential storage.
- **Cloud-save sync:** serialise the LiteDB save to a **versioned blob** in Firebase Storage/Firestore, keyed by account; upload on save, download on login, last-write-wins with a version stamp + conflict prompt. **Offline-first:** the game keeps working fully offline and syncs on reconnect.
- **Client:** a `ICloudSaveService` / `IAuthService` layer in Core behind interfaces (fits the existing DI pattern), with the Firebase SDK wired in the Godot host; graceful offline degradation.
- **Config/environments:** Firebase project(s) for dev/prod; secrets out of source (drop the hardcoded `COMPUTER\DARKNESS` connection string).
- **Exit criteria:** a player can register, log in, and have saves round-trip across a reinstall; offline play unaffected.

## Phase 2 — Monetization Stack  *(size: L — depends on Phase 1)*
- **Economy design:** define soft/hard currency and materials; make materials the ad/IAP target per the chosen model; design sinks/sources so IAP is convenience, not pay-to-win.
- **Google Play Billing:** IAP for materials/currency; **server-authoritative receipt validation** (needs Phase 1 backend).
- **Rewarded ads:** AdMob (mediation enabled) rewarded-video granting materials; strictly optional, no forced interstitials that harm the experience.
- **Analytics:** Firebase Analytics events/funnels/retention (needed to tune the economy) — none exists today; comes largely for free once the Firebase SDK from Phase 1 is in.
- **Exit criteria:** validated test purchases, rewarded-ad grants materials, economy dashboards live, balance pass done.

## Phase 3 — Story Content & Expandable Narrative Model (Arc 1, beats 1–10)  *(size: XL — biggest content lift; parallel)*  — ✅ FIRST DRAFT DONE (2026-07-18)
**Done:** expandable `Arc` model + seeders + arc queries + i18n-ready `LocalizationService` (Phases B/C); Arc 1 authored as `beat_1..beat_10.json` + `arcs/arc_1.json` from the `darkness-story` prose (PC-lead + Darius/Tywin allies); graph validation hardened (`QuestGraphAudit` now asserts, new `ArcGraphAudit`). Canon + per-beat brief in `docs/design/arc-1-authoring-brief.md`.
**Pending product owner:** content review of the beats; canon calls (Araknos bull-vs-spider, Kyarias/KazarthEndGame, Balgathor→Dariartimous, confirm PC-inherits-hero-arc); missing backdrop/sprite art (`cliff`/`road`/`throne_room` + enemy placeholders).
**Deferred:** i18n key-extraction tool (`LocExtract`) — content is inline English (fallback-safe) until translation is pursued.

The story is written; this phase is about **an authoring pipeline that scales to the whole saga** and then filling Arc 1 to shippable depth.
- **Expandable content model (do first in this phase):** formalise arc-level organisation on top of the existing data-driven quest chains — an `Arc` grouping (id, title, sort order, prerequisites) so Arcs 1–10 (and beyond) slot in by adding data, not code. The engine already supports this shape (`QuestChain` has `Prerequisites`, `SortOrder`, `IsMainStory`); make it explicit and arc-aware so v1 ships Arc 1 while the model holds all 10 arcs.
- **Authoring pipeline & tooling:** a repeatable path (schema + validation + the `QuestGraphAudit`/`QuestJsonRoundTrip` tests) so a writer/agent can add a beat as JSON and have it validated automatically. Throughput, not design, is the constraint — the writing exists.
- **Author Arc 1 to depth (~8–10 hours):** expand each of beats 1–10 from ~2 steps into full chapters — multi-step dialogue, meaningful branches + morality consequences, varied encounters, side content, rewards. Today only beat_1 branches and beat_4 is a one-line stub; the beat-10 Kyarias undead-army finale needs completing. At 8–10 hours this is the single largest v1 workstream — budget accordingly and consider side-quests/exploration beats between main beats to reach the target without padding the critical path.
- **Bosses:** wire the **12 existing boss sprites** (Tywin, Araknos, Kyarias, etc. — several already named in Story-Flow) into their climactic beats.
- **Exit criteria:** Arc 1 (all 10 beats) playable end-to-end with branches, bosses, and a satisfying arc; arc/chain model proven expandable by stubbing Arc 2's shell from data alone; QuestGraphAudit green; target playtime met.

## Phase 4 — Systems Depth  *(size: L — high ROI; parallel)*  — 🔶 STARTED (2026-07-18)
**Done:** weapon wardrobe wired (defs 5→24: swords/staves/ranged) + `SheetDefinitionAudit` (asset paths fail CI); combat **party model** `PartyBattleController` extracted to Core + tested (multi-party, Darius/Tywin allies), `CombatData.Allies` added. **Godot BattleScene multi-party/ally wiring is NOT done** — blocked on the GdUnit4 harness one-time setup so it's verifiable (see `docs/design/gdunit4-setup.md`).
**Remaining:** armor/legs/off-hand wardrobe batches; talent subclass trees; item/recipe/skill expansion + class tagging; enemy variety; balance pass.
- **Wardrobe wiring (quick win / highest ROI):** the art is already drawn — expand `sheet_definitions` from 21 pieces toward the full library (9+ sword types, 4 ranged, ~6 staves, extra armor/legs/feet). Mostly JSON authoring; huge perceived depth for the effort.
- **Talents:** flesh out the 16 stub subclass trees (currently 1 node each) into real trees with prerequisites and effects.
- **Items/recipes/skills:** grow the item catalogue (29 → shippable), add recipes, tag skills by class (all `ClassRequirement` are null today).
- **Enemies:** define more encounters from existing sprites; balance pass using the `CombatSim` audit.
- **Exit criteria:** full equippable wardrobe, complete class/subclass progression, balanced combat across all 20 levels.

## Phase 5 — Audio  *(size: L — from zero; parallel)*
- Build an audio system (music manager + SFX buses); the Settings scene already exposes Master/Music/SFX volume, so the hooks exist.
- Source/produce area music, combat stingers, UI and ambient SFX; wire into all scenes.
- **Exit criteria:** every scene has music + SFX, respecting the volume settings.

## Phase 6 — UX, Polish & Game Feel  *(size: L; parallel, tail-end)*
- Onboarding/tutorial, VFX/juice, transitions, save/load UX, touch-control polish, accessibility, localisation scaffolding.
- Performance profiling on representative Android devices (QA/performance agent); fix the BattleScene heal hardcode, WorldScene collision shortcut, StudyScene stat display.
- **Exit criteria:** smooth first-time experience, stable frame rate on target devices, no known UX blockers.

## Phase 7 — Release Readiness (Play Store)  *(size: M–L; after 1, 2, and content streams)*
- Signed **release AAB** + keystore management (today: debug-only export, empty keystores); adaptive icons + splash; store listing (screenshots, feature graphic, copy).
- **Compliance:** privacy policy (required for accounts + ads + analytics), Play data-safety form, age rating, COPPA/GDPR-K review (ads + accounts may attract minors).
- **Crash reporting** (Firebase Crashlytics) + production analytics wired — same Firebase SDK from Phase 1.
- Play Console setup; closed → open testing tracks; soft launch in limited regions.
- **Exit criteria:** app passes Play review on a closed track, crash-free rate acceptable, compliance complete.

## Phase 8 — Launch & Post-Launch  *(size: ongoing)*
- Soft launch → global launch; monitoring + hotfix pipeline; live-ops (the `login-calendar` already exists — make it server-driven).
- Content cadence: ship Story-Flow **Arcs 2–10** ("Castle of Horror" → "Daverak") as post-launch updates — pure data drops into the expandable arc model built in Phase 3, no engine work.

---

## Quick wins (high value, low effort — pull forward)
1. **Wire the drawn wardrobe** (Phase 4) — art exists; JSON-only work; big depth gain.
2. **Repo hygiene + CI** (Phase 0) — small effort, unblocks safe iteration for the whole team.
3. **Expand encounters/deathmatch** from existing sprites — more content, no new art.

## Biggest risks
1. **Backend from scratch** (accounts, cloud saves, receipt validation, hosting) — net-new and security-sensitive (plaintext passwords today). Underestimating this sinks the timeline.
2. **Story authoring throughput** — the narrative is written, but authoring Arc 1's 10 beats to shippable depth (and keeping the model clean enough for Arcs 2–10) is a sustained effort, not a data tweak. Risk is throughput + an expandable pipeline, not blank-page design.
3. **Monetization compliance** — ads + IAP + accounts for a fantasy RPG that may attract minors triggers privacy/age-rating obligations that must be designed in early, not bolted on.

## Decisions (locked) & open items

**Locked:**
- ✅ **Cloud-save model:** **full-save-blob sync.** The LiteDB save is serialised and stored as a versioned blob per account; offline-first, cheap, no server-authoritative game state. (Product owner, 2026-07-17.)
- ✅ **Backend stack:** **Firebase** — Auth (accounts), Cloud Storage/Firestore (save blobs), Analytics, Crashlytics, and Remote Config in one Google-native SDK. **Retire the ASP.NET Core WebAPI scaffold** (orphaned, plaintext passwords, no migrations — the highest-lift path). Rationale: chosen over PlayFab on **platform longevity** (Firebase is a core Google product; PlayFab's roadmap under Microsoft is less certain), **ecosystem fit** (native with Play Billing + AdMob + unified analytics), and **right-sized scope** (accounts + cloud saves is Firebase's sweet spot — PlayFab's game-server/economy/LiveOps features are largely unused here). IAP receipt validation is a small Cloud Function rather than a reason to adopt PlayFab. Client access stays behind a Core interface so it's swappable. *(Confirmed by product owner, 2026-07-17.)*
- ✅ **Ad network:** **AdMob with mediation enabled.** One SDK, Firebase-native, starts single-network and scales to multi-network fill/eCPM without re-architecting. Keeps ads + billing + analytics all in the Google ecosystem (simpler compliance).
- ✅ **Monetization boundary:** **convenience-only at this stage** — no directly-purchasable power, no loot boxes/gacha. (Revisit only if the model changes post-launch.)
- ✅ **Localization:** English-only v1; content model built i18n-ready (externalized text keys) so translations add without rework.
- ✅ **Content depth:** Arc 1 authored to **~8–10 hours** of playtime. This makes story-authoring throughput the dominant v1 effort (see Phase 3 + Risks).
- ✅ **Age rating:** target **Teen (13+)** — drives content limits, AdMob child-directed/consent config, and the Play data-safety form.
- ✅ **Audio sourcing:** **free / self-made** assets (royalty-free/CC or self-produced) to keep cost near zero.
- ✅ **Ownership & payouts:** Firebase project, Play Console, and AdMob all bound to **risidiangames@gmail.com**.
- ✅ **Launch approach:** standard staged rollout (closed beta → soft launch → global) accepted.

**Still open:**
- **Store economy specifics (numbers):** currency names, IAP tiers/price points, ad-reward rates within the convenience-only boundary above (needed for Phase 2 balancing — I can draft a proposed economy for sign-off).
- **Final store title:** "Darkness: Reforged" is a working title; confirm the real name + trademark sanity check before Phase 7.
- **Team velocity:** needed to convert phase sizes into dates.
