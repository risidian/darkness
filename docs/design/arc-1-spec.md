# Arc 1 — "Darkness: Revenge" — Canonical Spec

> **Status: APPROVED canonical spec** — reconciled with the FULL Ch1–10 prose + product-owner
> decisions (2026-07-18). Beats `beat_1..beat_10.json` are AUTHORED (first draft); the revisions
> below (per the locked decisions) are the pending content pass. See
> [arc-1-revision-brief.md](./arc-1-revision-brief.md) for the full prose reconciliation and
> [arc-content-model.md](./arc-content-model.md) for the technical model.
> **Note:** prose Chapters 4–10 = a separate **Arc 2** ("Castle Horror interior") — future spec.

## Characters & party
- **PC** — the player's created character (`CharacterGenScene`: name, class, appearance), addressed via the **`{playerName}` token**. **The protagonist**: inherits the amnesiac + "**Atriartous**" prophecy arc. *(Deliberate divergence from the prose, where Darius is that hero — product-owner confirmed 2026-07-18.)*
- **Darius** — story **companion, combat ally from beat 1 onward**; wakes alongside the PC. Party = **PC + Darius** through the arc.
- **Tywin** — **enemy at beat 8**, then **recruited as a permanent party ally at beat 9** (product-owner confirmed — a divergence from the prose, where Tywin departs before the castle). Party = PC + Darius + Tywin from beat 9 through the finale.
- **Dariartimous** — the **beat-4 cliff villain** ("Seeker of Darkness", ritual-caster/ship-sinker). *(Was mislabeled "Balgathor".)*
- **Beat-10 gate cast (introduced here, central to Arc 2):** **Kyarias** (female necromancer), **Arthoros** (Dragoon Knight), **Balgathor** (Daverak's gate champion who halts the fight and admits the party).

## Locked decisions (product owner, 2026-07-18)
1. **Protagonist = PC** (inherits the Atriartous/prophecy arc); Darius stays companion. Keep this divergence from the prose.
2. **Arc-1 ending = black-castle gate gauntlet** (replaces the invented beat-10 "undead army"): skeleton horde → **Kyarias** → **Arthoros** → **Balgathor** halts it and admits the party; Arc 1 ends at the castle threshold, setting up Arc 2.
3. **Tywin = permanent party ally** from beat 9 (kept despite the prose departure).
4. **Boss renames:** beat-4 villain **Balgathor → Dariartimous**; beat-9 demon **Araknos → Tauros** (bull-demon). Frees `Balgathor`/`Kyarias` for their correct Arc-2 use (gate champion / gate necromancer).
- (Earlier) Beat 3 split from beat_1; `{playerName}` token; localization token interpolation.

**Still-open smaller canon (backlog):** beat-2 "tavern" → Bria's Colosseum shelter (no tavern in prose); beat-7 "goblins" → bandits; beat-5 sorcerer unnamed/unwinnable vs game "Risidian"/winnable; Darius's black-greatsword signature weapon.

## The 10 beats

| # | id | Title | Summary | Combat (party vs) | Disposition |
|---|-----|-------|---------|-------------------|-------------|
| 1 | `beat_1` | The Awakening | PC + Darius wake on the Camelot shore; Old Man intro; Fight/Sneak roleplay branch (combat deferred to beat 3). | — (intro; first fight moved to beat 3) | **KEEP** structure; move its combat out to beat 3; reconcile the Fight/Sneak branch as narrative choice. |
| 2 | `beat_2` | The Tavern | Learn of taverns / world state; rest & setup at the Rusty Tankard. | — | **REORDER** from `beat_4` (SO 4→2) + **EXPAND** (currently a 1-step stub). |
| 3 | `beat_3` | First Blood | First real combat: the party vs 3 undead dogs. | 3 undead dogs | **SPLIT/AUTHOR** — content currently buried in beat_1's Fight branch; give it its own beat. |
| 4 | `beat_4` | The Dark Warrior | Cliff-face encounter; survive **5 rounds** vs the invincible **Dariartimous**, who then vanishes. | Dariartimous (`IsInvincible`, `SurvivalTurns: 5`) | authored; **REVISE**: rename villain Balgathor→**Dariartimous** (name+asset); backdrop → cliff. |
| 5 | `beat_5` | The Sorcerer | The sorcerer **Risidian** visits; winnable fight. | Risidian (Lvl 8) | **REORDER** from `beat_3` (SO 3→5). |
| 6 | `beat_6` | Lore of the Shadow | Exposition / worldbuilding beat (the arc's lore drop). | — | **AUTHOR NEW** — nothing exists today. |
| 7 | `beat_7` | Journey Begins | Ambush on the road: party vs **6 goblins**. | 6 goblins | **REORDER** from `beat_5` (SO 5→7) + **EXPAND** 2→6 goblins. |
| 8 | `beat_8` | The Knight | **Tywin** blocks the bridge; fought as an enemy. | Tywin (Lvl 6, enemy) | **REORDER** from `beat_6` (SO 6→8). |
| 9 | `beat_9` | Joint Forces | Tywin joins permanently; PC + Darius + **Tywin (ally)** vs the **Tauros** bull-demon. | Tauros (Lvl 10) — party of 3 | authored; **REVISE**: rename Araknos→**Tauros** (bull design). |
| 10 | `beat_10` | The Black Castle Gate | Party arrives at the castle: skeleton horde → **Kyarias** (necromancer) → **Arthoros** (Dragoon) → **Balgathor** halts it and admits them. Ends Arc 1 at the threshold; sets up Arc 2. | skeleton horde → Kyarias → Arthoros (party of 3) | authored draft is the wrong finale; **REPLACE** the "undead army/Kyarias-final-boss" with the gate gauntlet. |

## Pending revision pass (beats are authored; apply the decisions)
1. **Beat 4** — rename villain Balgathor → **Dariartimous** (dialogue speaker + `SpriteKey`/name); needs a Dariartimous asset or a temporary reuse (do NOT keep `bosses/Balgathor`).
2. **Beat 9** — rename Araknos → **Tauros**; render as the bull-demon.
3. **Beat 10** — **replace** the invented "undead army / Kyarias-final-boss" with the **black-castle gate gauntlet** (skeleton horde → Kyarias → Arthoros → Balgathor admits the party). Drop the `KazarthEndGame` boss. This correctly introduces the Arc-2 cast.
4. **Beat 6** — swap the placeholder villain sprites (Dariartimous/Daverak currently use `bosses/Balgathor`/`bosses/Kazarth`, which are distinct Arc-2 characters).
5. **Assets flagged for creation/remap** (product owner): Dariartimous, Daverak, Kyarias, Arthoros, Tauros sprites; `cliff`/`road`/`throne_room` backdrops.
6. **Still-open smaller canon** (see backlog): beat-2 tavern → Bria's shelter; beat-7 goblins → bandits; beat-5 sorcerer unnamed/unwinnable; Darius's black-greatsword weapon.

## Arc 2 (future spec)
Prose Chapters 4–10 = "Castle Horror interior": Balgathor, Kyarias, Kazarth (companion), Arthoros, Shazza (true-name-branding curse mechanic), Jezzurela, the Master, Covenant of Bellis, curse-medal economy. Substantial written material — green-lit for its own spec pass. See [arc-1-revision-brief.md](./arc-1-revision-brief.md) §glossary.
