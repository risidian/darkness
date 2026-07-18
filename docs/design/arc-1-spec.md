# Arc 1 — "Darkness: Revenge" — Canonical Spec

> **Status: APPROVED canonical spec** (reconciled with the product owner, 2026-07-18).
> Supersedes the drifted `Story-Flow.md` Arc 1 section and the mis-ordered `beat_*` JSON.
> This is the authoring source of truth. See [arc-content-model.md](./arc-content-model.md)
> for the technical model this maps onto.

## Characters & party
- **PC** — the player's created character (`CharacterGenScene`: player-chosen name, class, appearance). Addressed in dialogue via a **`{playerName}` token** (never hardcoded). This is the protagonist the player controls.
- **Darius** — named story **companion, combat ally from beat 1 onward.** Wakes up alongside the PC on the shore. Party is **PC + Darius** for the whole arc. (Story-Flow's "Darius vs X" = shorthand for the PC-led party.)
- **Tywin** — **enemy at beat 8** (blocks the bridge), then **recruited as a party ally at beat 9** ("Joint Forces"). Party becomes PC + Darius + Tywin from beat 9.

## Locked decisions
- Beat 3 (first combat) is **split into its own beat** (out of beat_1's fight branch).
- Beat 10 **merges** the current `beat_8` + `beat_9` into one climactic battle.
- **Spec wins** over the current implementation on rosters/backdrops/allies (6 goblins, 5+5 undead, cliff backdrop, Tywin ally, etc.).
- PC name via `{playerName}` token; localization string system supports token interpolation.

## The 10 beats

| # | id | Title | Summary | Combat (party vs) | Disposition |
|---|-----|-------|---------|-------------------|-------------|
| 1 | `beat_1` | The Awakening | PC + Darius wake on the Camelot shore; Old Man intro; Fight/Sneak roleplay branch (combat deferred to beat 3). | — (intro; first fight moved to beat 3) | **KEEP** structure; move its combat out to beat 3; reconcile the Fight/Sneak branch as narrative choice. |
| 2 | `beat_2` | The Tavern | Learn of taverns / world state; rest & setup at the Rusty Tankard. | — | **REORDER** from `beat_4` (SO 4→2) + **EXPAND** (currently a 1-step stub). |
| 3 | `beat_3` | First Blood | First real combat: the party vs 3 undead dogs. | 3 undead dogs | **SPLIT/AUTHOR** — content currently buried in beat_1's Fight branch; give it its own beat. |
| 4 | `beat_4` | The Dark Warrior | Cliff-face encounter; survive **5 rounds** vs the invincible **Balgathor**, who then vanishes. | Balgathor (`IsInvincible`, `SurvivalTurns: 5`) | **REORDER** from `beat_2` (SO 2→4); **fix backdrop** `dark_castle` → cliff. |
| 5 | `beat_5` | The Sorcerer | The sorcerer **Risidian** visits; winnable fight. | Risidian (Lvl 8) | **REORDER** from `beat_3` (SO 3→5). |
| 6 | `beat_6` | Lore of the Shadow | Exposition / worldbuilding beat (the arc's lore drop). | — | **AUTHOR NEW** — nothing exists today. |
| 7 | `beat_7` | Journey Begins | Ambush on the road: party vs **6 goblins**. | 6 goblins | **REORDER** from `beat_5` (SO 5→7) + **EXPAND** 2→6 goblins. |
| 8 | `beat_8` | The Knight | **Tywin** blocks the bridge; fought as an enemy. | Tywin (Lvl 6, enemy) | **REORDER** from `beat_6` (SO 6→8). |
| 9 | `beat_9` | Joint Forces | Tywin joins; PC + Darius + **Tywin (ally)** vs the **Araknos Demon**. | Araknos (Lvl 10) — party of 3 | **REORDER** from `beat_7` (SO 7→9) + **ADD Tywin as party ally**. |
| 10 | `beat_10` | The Undead Army | Final Arc-1 battle: party vs **Kyarias + 5 Skeletons + 5 Zombies**. | Kyarias (Lvl 15) + 5 Skeleton + 5 Zombie | **MERGE** `beat_8_undead_army` + `beat_9_kyarias_final`; **reroster** to spec. |

## Work summary
- **Reorder only (content OK):** beats 4, 5, 8 (from current beat_2/3/6).
- **Reorder + expand:** beat 2 (stub→full), beat 7 (2→6 goblins), beat 9 (add Tywin ally).
- **Author new:** beat 3 (dogs, split from beat_1), beat 6 (lore).
- **Merge + reroster:** beat 10 (from beat_8+beat_9).
- **Cross-cutting:** introduce `{playerName}` token; make every battle a party battle (PC + Darius, +Tywin from beat 9); define Darius (and Tywin) as ally characters (stats/sprite) in the ally/party system; thread Darius into dialogue as a named companion speaker.

## Open item
- **Fuller written source:** the product owner has more Arc-1 story written than Story-Flow's one-liners. Beats 2, 3, and 6 (and expansions) should be authored from that material when provided; otherwise drafted from this spec for correction.
