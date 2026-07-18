# Arc 1 Authoring Brief — "Darkness: Reforged"

> Source prose: `darkness-story/Story_Chapters_1-3.txt` (Ch.1 Darkness, Ch.2 Consequence, Ch.3 Journey).
> Structure: [arc-1-spec.md](./arc-1-spec.md). Target: `Darkness.Godot/assets/data/quests/beat_*.json`.
> **Read the "Resolved canon & decisions" block below before authoring — it overrides the prose where the game design differs.**

## Resolved canon & decisions (manager calls, 2026-07-18 — flagged for product-owner veto)
- **Protagonist:** the player-created **PC** (`{playerName}` token) is the arc lead and inherits the prose-protagonist's hero arc — amnesiac who washes ashore, subject of Daverak's prophecy, true-named **Atriartous** (a reveal seeded now, independent of the player's chosen display name; paid off in a later arc). *(DEFAULT — the prose made Darius the sole hero; the game design makes the PC lead. Veto if the reveal should attach to Darius instead.)*
- **Darius:** recast as a **fellow survivor + permanent companion/ally** who washes ashore with the PC. Keep his prose voice (near-silent; acts, then speaks in three words).
- **Villains (distinct characters):** **Daverak** = arch-villain, throne-master (beat 6 lore). **Dariartimous** = Daverak's field agent, "Seeker of Darkness" (the cliff duel + shipwreck). **Both ≠ Darius** (product-owner confirmed).
- **In-game boss names follow EXISTING ASSETS** to avoid churn, with prose-lore names recorded as annotations:
  - Beat 4 cliff boss = **Balgathor** (existing asset/JSON). *Lore = the prose's "Dariartimous."* FLAG: renaming Balgathor→Dariartimous would preserve the intended **Daverak/Dariartimous/Atriartous/Darius** name-web reveal — product-owner call.
  - Beat 9 demon = **Araknos** (existing name), **rendered as the prose's Tauros bull-demon** design (13ft, greataxe, steel-turning hide). FLAG: reconcile spider-vs-bull.
  - Beat 10 final = **Kyarias**. FLAG: sprite is `bosses/KazarthEndGame` — name/asset mismatch to reconcile.
- **NPCs from prose to use:** **Bria** (brusque caretaker — fold into beat 1/2 in place of/with the generic "Old Man"), **Burgh** (smith — gifts the swords after beat 4).

## Character & lore glossary
| Name | Role | Description |
|---|---|---|
| Daverak | Arch-villain | Throne-figure in the black castle; inhuman scale, void-helm, two-forked ember staff; archaic register ("Thy loyalty draws nigh its proving"). Holds the prophecy. |
| Dariartimous | Villain (field agent) | "Seeker of Darkness." Void-helm, black carapace armor. Performs the opening pentagram ritual; sinks the PC's ship (origin of amnesia); the cliff duel (= in-game Balgathor). |
| Atriartous | Prophesied hero (true-name) | The one who at his eighteenth year can "sunder" Daverak. → the PC's true name (default). |
| PC / `{playerName}` | Protagonist (player-created) | Arc lead; amnesiac; inherits the prose hero arc. |
| Darius | Companion / permanent ally | Co-survivor on the shore; near-silent, decisive; fights beside the PC all arc. |
| Bria | NPC (caretaker) | Broad-shouldered woman at the Colosseum ruins; broth, bandages, brusque maternal care; hidden tear at farewell. |
| Burgh | NPC (smith) | Bria's companion; retrieves & gifts the two swords after the cliff fight. |
| Risidian | Boss (sorcerer) | Wide-brimmed hat, scorched blue-threaded robes, silver-white eyes; reality-warping (erase/rewind stone, freeze time); calm, condescending; leaves by choice. Beat 5, Lvl 8 winnable. |
| Tywin | Companion (recruited) | 16, youngest Knight of the Round Table; rapier + buckler, scar through left eyebrow; dry gallows-gallantry ("my good sir", "Prefer not to die alone"). Enemy at beat 8, ally from beat 9. |
| Araknos | Boss (demon) | Beat 9, Lvl 10. Render as the prose Tauros: 13ft bull-man, curved horns, ember eyes, greataxe, hide that turns steel. |
| Kyarias | Final boss | Beat 10, Lvl 15; lord of the undead army; serves Daverak's dominion. Not in prose (invent). |
| Hell hounds | Mob | Muscle over burning sinew, ember eyes, smoke-breath; dissolve to ash on death. Basis for beat 3's "undead dogs." |
| Camelot | Place | Tiered stone city on the plain; Tywin's home; the arc's arrival point. |
| Shore of Camelot | Place | Wreck-strewn beach where the PC (and Darius) wash ashore among the drowned crew. |
| Colosseum ruins | Place | Pale mossy arches "like the ribs of something long dead"; Bria/Burgh's village; the sorcerer encounter. |
| Cliffside | Place | Where the world ends over the sea; the opening ritual and the Dariartimous/Balgathor duel. |
| Black castle | Place (lore) | Obsidian fortress raised by the ritual; Daverak's throne room. |

## Per-beat authoring guidance

### Beat 1 — The Awakening
- Backdrop `camelot_shore`; grey sky, wreckage, drowned crew. PC + Darius wake. Fold **Bria** in (replace/augment the generic Old Man).
- Prose (rich): amnesia — salt/copper taste, no name, "a hollow where something should have been"; pull toward the Colosseum arches.
- Invent: Darius as a second survivor beside the PC; the Fight/Sneak framing (narrative branch only — combat deferred to beat 3), carry MoralityImpact (+5/−5) as current JSON.
- Lines: (internal) "No name. No memory of the sea. Only salt and copper." · Darius: "You're breathing. Good. That makes two of us the sea didn't want." · Bria: "The Shore of Camelot. You came in with the wreckage, {playerName} — and the dead."
- Combat: none. Branch: Fight/Sneak → converge to beat 2.

### Beat 2 — The Tavern
- Backdrop `tavern` (Rusty Tankard). Prose has no tavern — closest is Bria's dwelling; recommend folding Bria in as host. **Mostly bridge content.**
- Prose (thin): brusque care; world-state unease ("the dusk coming wrong," fog rolling in).
- Lines: Keeper/Bria: "You've the look of the drowned about you — sit before you fall." · "The shadows are bolder every night. Folk who walk the roads don't come back the same." · Darius: "We rest. We eat. Then we find whatever did this."
- Combat: none. Good spot for a rest/heal mechanic; light exposition seeds beat 6.

### Beat 3 — First Blood
- Backdrop shore/riverbed (`camelot_shore_2`), continuing beat 1's path. PC + Darius vs **3 undead dogs / hell hounds**.
- Prose: hell hounds — "muscle over burning sinew, ember eyes, smoke curling"; dissolve to ash on death.
- Lines: Darius: "Something's out there. It's not afraid of us." · "Smoke and embers — put them down before they circle." · (on kill) "Ash. They just… come apart."
- Combat: PC + Darius vs 3 hell hounds (SpriteKey `hound`, ~Lvl 1–2). No branch.

### Beat 4 — The Dark Warrior
- Backdrop **cliff** (fix `dark_castle`→cliff); fog, scorched stone. PC + Darius vs **Balgathor** (`IsInvincible`, `SurvivalTurns: 5`, vanishes into a void). *(= prose Dariartimous.)*
- Prose (rich): "Do you remember me, boy?"; the name-claim; "I am the Seeker of Darkness"; outmatched melee; "Do you feel it — what you were meant to be?"; grapple-and-kick; villain swallowed by a folding void; Burgh gifts the swords.
- Lines: Villain: "Do you remember me?" / "Do you feel it — what you were meant to be?" / "You are not ready. Not yet." · PC/Darius: "I don't remember you. I don't need to." · Burgh: "I heard steel out there. Keep the blades — you'll need them."
- Combat: survive 5 rounds vs Balgathor; boss vanishes. No branch.

### Beat 5 — The Sorcerer
- Backdrop Colosseum ruins (consider changing from `dark_castle`). PC + Darius vs **Risidian** (Lvl 8, winnable).
- Prose (rich): reality-cracking wizard; "Twice you've snuck up on me, lad." / "What do you seek, boy?" / "You won't find it here. Move on."
- Invent: winnable framing (prose is unwinnable; treat rewind as flavor, or "earn his respect, he departs").
- Lines: Risidian: "Twice now you've crept up on me. Bold. Or stupid." / "You wish to harm me? Whatever for?" · PC/Darius: "Power. Enough to destroy the Black Knight." · Risidian: "That? You won't find it here. Move on."
- Combat: PC + Darius vs Risidian. Optional morality choice (plead for the town vs demand power).

### Beat 6 — Lore of the Shadow
- Backdrop vision framing — storm cliff + black-castle throne room (`dark_castle`/`throne_room`). Villain intros: **Daverak**, **Dariartimous**.
- Prose (richest — Ch.1 cold open): the 5-weapon pentagram ritual (meteoric hammer, flame sword, moonlit spear, zweihänder, bone-daggers) raising the castle; Daverak's prophecy decree (Atriartous, eighteenth year, "see that he is undone"); the shipwreck that drowns the PC's ship — the amnesia origin.
- Invent: delivery vehicle (dream/vision/relic). Prophecy is about **the PC (Atriartous)** per default.
- Lines: Daverak: "Thy loyalty draws nigh its proving." / "Only one can sunder me… see that he is undone." · Dariartimous: "It shall be done, my lord." · Narration: "Five weapons torn from the dark. A seal carved in ruin. The castle woke — and the world knew something terrible had begun."
- Combat: none.

### Beat 7 — Journey Begins
- Backdrop `forest`/road south — bleached hills, skeletal trees. PC + Darius vs **6 goblins** (expand from 2). Optional Scout NPC.
- Prose (partial): the road south; "Cliffs, beasts, bandits… world's feeling thin." No goblins specifically.
- Lines: Scout: "Don't go on — the woods are crawling. They've set a trap." · Darius: "Six of them. They think numbers are enough." / "The world's feeling thin. Everything hungry crawls out of it."
- Combat: PC + Darius vs 6 goblins (raiders/archers). Optional heed-the-scout branch.

### Beat 8 — The Knight
- Backdrop cliff road / bridge (`road`). PC + Darius vs **Tywin** (Lvl 6 enemy, proving fight — not a grudge).
- Prose (partial/divergent): the Tywin meeting is **peaceful** in prose; the enemy fight is invented. Keep it a *proving* fight so beat-9 recruitment reads naturally.
- Lines: Tywin: "Halt. These cliffs swallow strangers. Prove you're not one more thing crawling out of the thin places." / (mid-fight) "Good. Most don't last this long." / (yielding) "Enough — you've made your point, my good sir."
- Combat: PC + Darius vs Tywin (non-lethal framing). Optional talk-down vs fight-through (converge).

### Beat 9 — Joint Forces
- Backdrop scorched hills (consider changing from `cave`). PC + Darius + **Tywin (ally)** vs **Araknos/Tauros** (Lvl 10).
- Prose (rich — best set-piece): the Tauros co-op kill — hide turns steel, Darius for the head / Tywin the legs; "NOW! KILL IT!"; "My good sirrrrr!"; axe hurled off the cliff; "That was messy." / "Still glad you didn't want to die alone?" / "…For now."
- Lines: Tywin (joining): "Two swords are better than one, my good sir. Prefer not to die alone." / "Its hide turns steel — go for the joints!" · Darius: "The head. Get me to the head."
- Combat: party of 3 vs Araknos (Lvl 10). Add Tywin as party ally (stats + `bosses/Tywin` sprite). No branch.

### Beat 10 — The Undead Army
- Backdrop `battlefield`/`throne_room`. PC + Darius + Tywin vs **Kyarias (Lvl 15) + 5 Skeletons + 5 Zombies** (merge current beat_8+beat_9; reroster).
- Prose: none (beyond Ch.3) — invent Kyarias's identity/motive/army; anchor to Daverak's dominion.
- Lines: Kyarias: "The little ant has reached the throne. All games end." / "Every soul that falls joins my ranks." · Tywin: "That's a great many of them, my good sir." · Darius: "Then we make it a great many fewer."
- Combat: party of 3 vs Kyarias + 5 Skeleton + 5 Zombie. Reconcile Kyarias/KazarthEndGame asset.

## Tone guide
- **Terse, fragmented, vertical** — one-line paragraphs, sentence fragments; let single words land.
- **Visceral dark-fantasy sensory detail** — storm, sulfur, ash, brine, copper, burning sinew; imply dread.
- **Two registers:** villains elevated/cold (Daverak's archaic thee/thy; Dariartimous "measured, empty"; Risidian calm-curious); humans grounded/wry (Tywin's gallows-gallantry; Bria's brusque care).
- **Restraint & silence as motifs** — emotion implied, not stated; understatement over melodrama; Darius near-silent (acts, then three words).
