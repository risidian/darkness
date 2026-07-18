# Design Spec — 16 Advanced-Subclass Talent Trees

> Authoring spec for fleshing out the 16 one-node subclass stubs in
> `Darkness.Godot/assets/data/talent-trees.json`. Each tree keeps its existing stub node as
> the root and adds 5 more (6 total) in a diamond (root → 2 branches → 2 mid → converged capstone).
> All `PointsRequired: 1`; passives `IsPassive: true`; skill-unlock nodes `IsPassive: false`.
> Magnitudes stay within the base trees' bands. Status: ready to author (see blockers §7).

## Schema constraints (respect these)
`TalentNode`: `Id, Name, Description, PointsRequired(=1), Row, Column, PrerequisiteNodeIds[], IsPassive, AutomaticallyUnlocked, Effect{Stat,Value | Skill}`. Effect is EITHER a `Stat`+`Value` passive OR a `Skill` unlock (name must match a `skills.json` Name). `PrerequisiteNodeIds` must reference ids **within the same tree**. Layout via `TalentLayoutHelper` on a 3-col grid (Mage/Cleric use col 3 as their base trees do).

**Functional stat keys** (read by `RecalculateDerivedStats`): Strength, Dexterity, Constitution, Intelligence, Wisdom, Charisma, MaxHP, Mana, Stamina, Speed, Attack, Accuracy, Evasion, Defense, MagicDefense, ArmorClass, CriticalChance, CriticalDamage, CriticalDefense.
**Flavor keys** (stored but NOT read yet): Armor, Block, HolyDamage, FireDamage, FrostDamage, Healing, Stealth — used only where a base tree already established them thematically.

**Real skills for `Effect.Skill`:** Arcane Bolt, Fireball, Quick Shot, Snipe, Quick Stab, Vitals, Cleave, Crush, Smash, Stun, Slash, Thrust, Punch, Kick, Mana Shield, Dodge, Parry, Shield Block, Deflect, Brace, Holy Strike, Offhand Stab.

## Trees (parent → subclass; keep existing root node; add the listed nodes)

### Knight
**Holy Knight** `holy_knight_tree` (T2): hk_faith(root, Wis+2) · hk_valor(Attack+2, 2/0←faith) · hk_smite(HolyDamage+5, 2/1←faith) · hk_aegis(Defense+2, 2/2←faith) · hk_holystrike(Skill "Holy Strike", 3/0←valor) · hk_paragon(Con+3, 4/1←smite).
**Crusader** `crusader_tree` (T3, Morality +1, excl Knight_EndGame): c_righteousness(root, Str+5) · c_zeal(Attack+5, 2/0) · c_holy_wrath(HolyDamage+10, 2/2) · c_crusade(CritChance+5, 3/0←zeal) · c_divine_shield(Defense+5, 3/2←holy_wrath) · c_champion(Str+10, 4/1←crusade,divine_shield).
**Oathbreaker** `oathbreaker_tree` (T3, Morality −1, excl Knight_EndGame): ob_darkness(root, Str+5) · ob_dread(Attack+5, 2/0) · ob_unholy(CritDamage+10, 2/2) · ob_terror(CritChance+5, 3/0←dread) · ob_leech(MaxHP+20, 3/2←unholy) · ob_deathknight(Str+10, 4/1←terror,leech).
**Kingsguard** `kingsguard_tree` (T3, excl Knight_EndGame): kg_loyalty(root, Con+5) · kg_bulwark(ArmorClass+5, 2/0) · kg_vigilance(Defense+5, 2/2) · kg_shieldwall(Block+10, 3/0←bulwark) · kg_resolve(MagicDefense+5, 3/2←vigilance) · kg_immortal(Con+10, 4/1←shieldwall,resolve).

### Warrior
**Berserker** `berserker_tree` (T2): b_bloodlust(root, Str+2) · b_frenzy(Attack+3, 2/0) · b_reckless(CritChance+3, 2/2) · b_bloodthirst(Con+2, 3/0←frenzy) · b_savagery(CritDamage+5, 3/2←reckless) · b_unbridled(Str+3, 4/1←bloodthirst,savagery).
**Warlord** `warlord_tree` (T3, excl Warrior_EndGame): wl_leadership(root, Con+5) · wl_tactician(Attack+5, 2/0) · wl_banner(MaxHP+25, 2/2) · wl_rally(Defense+5, 3/0←tactician) · wl_warcry(Skill "Stun", 3/2←banner) · wl_conqueror(Con+10, 4/1←rally,warcry).
**Juggernaut** `juggernaut_tree` (T3, excl Warrior_EndGame): jn_unstoppable(root, Str+10) · jn_momentum(Attack+5, 2/0) · jn_ironhide(ArmorClass+5, 2/2) · jn_crushing(CritDamage+10, 3/0←momentum) · jn_fortress(Con+5, 3/2←ironhide) · jn_titan(Str+10, 4/1←crushing,fortress).

### Mage (cols 1–3)
**Wizard** `wizard_tree` (T2): wiz_power(root@1/2, Int+2) · wiz_pyromancy(FireDamage+5, 2/1) · wiz_cryomancy(FrostDamage+5, 2/3) · wiz_focus(Mana+20, 3/1←pyro) · wiz_ward(MagicDefense+3, 3/3←cryo) · wiz_mastery(Int+3, 4/2←focus,ward).
**Archmage** `archmage_tree` (T3, excl Mage_EndGame): am_eternity(root, Int+5) · am_conflagration(FireDamage+10, 2/1) · am_absolute_zero(FrostDamage+10, 2/3) · am_manawell(Mana+40, 3/1←confl) · am_spellward(MagicDefense+5, 3/3←zero) · am_transcendence(Int+10, 4/2←manawell,spellward).
**Necromancer** `necromancer_tree` (T3, excl Mage_EndGame): nm_death(root, Wis+5) · nm_decay(MagicDefense+5, 2/1) · nm_soulharvest(Mana+30, 2/3) · nm_bonearmor(ArmorClass+5, 3/1←decay) · nm_lifedrain(MaxHP+30, 3/3←soulharvest) · nm_lichdom(Wis+10, 4/2←bonearmor,lifedrain).

### Rogue
**Assassin** `assassin_tree` (T2): as_lethality(root, Dex+2) · as_precision(Accuracy+10, 2/0) · as_deadly(CritChance+3, 2/2) · as_ambush(Skill "Offhand Stab", 3/0←precision) · as_venom(CritDamage+5, 3/2←deadly) · as_killer(Dex+3, 4/1←ambush,venom).
**Shadowblade** `shadowblade_tree` (T3, excl Rogue_EndGame): sb_shadows(root, Dex+5) · sb_swiftness(Speed+5, 2/0) · sb_shadowstrike(CritDamage+10, 2/2) · sb_evasion(Evasion+5, 3/0←swiftness) · sb_deathmark(CritChance+5, 3/2←shadowstrike) · sb_nightblade(Dex+10, 4/1←evasion,deathmark).
**Nightstalker** `nightstalker_tree` (T3, excl Rogue_EndGame): ns_night(root, Stealth+10) · ns_prowl(Evasion+5, 2/0) · ns_snipe(Skill "Snipe", 2/2) · ns_shadowstep(Speed+5, 3/0←prowl) · ns_ambush(CritDamage+10, 3/2←snipe) · ns_apex(Dex+10, 4/1←shadowstep,ambush).

### Cleric (cols 1–3)
**Priest** `priest_tree` (T2): p_spirituality(root@1/2, Wis+2) · p_mending(Healing+5, 2/1) · p_faithshield(MagicDefense+3, 2/3) · p_greaterheal(Healing+10, 3/1←mending) · p_sanctify(Mana+20, 3/3←faithshield) · p_saint(Wis+3, 4/2←greaterheal,sanctify).
**Oracle** `oracle_tree` (T3, excl Cleric_EndGame): o_vision(root, Wis+5) · o_foresight(Evasion+5, 2/1) · o_prophecy(MagicDefense+5, 2/3) · o_renewal(Healing+15, 3/1←foresight) · o_clarity(Mana+40, 3/3←prophecy) · o_ascension(Wis+10, 4/2←renewal,clarity).
**Inquisitor** `inquisitor_tree` (T3, excl Cleric_EndGame): in_zeal(root, Str+5) · in_smite(HolyDamage+10, 2/1) · in_judgment(Attack+5, 2/3) · in_holystrike(Skill "Holy Strike", 3/1←smite) · in_purge(CritChance+5, 3/3←judgment) · in_grandinquisitor(Str+10, 4/2←holystrike,purge).

## Blockers & notes for the implementer (§7)
1. **BLOCKER (pre-existing, fix while authoring):** tier-3 trees require `SpentInTree:<tier2>` = **20**, but a 6-node tier-2 tree offers ~6 points → tier-3 unreachable. **Fix: lower each tier-3 `SpentInTree:*` prerequisite from 20 → 5** in talent-trees.json (matches tier-2 gate style). Data-only edit.
2. **Flavor keys (Armor/Block/HolyDamage/Fire/Frost/Healing/Stealth) are not read by any formula yet** — confined to thematically-established trees. To make them mechanically live, wire them into damage/heal/defense formulas OR swap for functional keys. Out of scope for the authoring pass; log it.
3. **Holy Strike** skill declares `TalentRequirement: "holy_strike_node"`. The `hk_holystrike`/`in_holystrike` unlock nodes aren't named that — confirm how `TalentRequirement` is consumed (no reader found in Core; may be UI/unused). Rename a node id to `holy_strike_node` or relax the skill req if needed.
4. Necromancer/Priest/Oracle/Berserker/Warlord "active" fantasies are expressed as **passives only** (no matching skills exist). New active skills would need adding to skills.json first (+ possible engine support).
5. Magnitudes verified within base-tree bands; layout has ≤3 occupied columns per row (no collision displacement).
