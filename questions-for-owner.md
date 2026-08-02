# Questions for Product Owner — Darkness: Reforged

> Jeff's running list of questions/decisions needed. Check this file when you're back.
> Last updated: 2026-08-02 20:17 UTC+8

---

## Blocking Nothing (proceeding on defaults)

### Sprite Sheets
- [ ] The `generate_spritesheet.py` script currently tiles a SINGLE frame across all LPC animation slots (static placeholder). Real frame-by-frame animation requires either: (a) manual art, (b) a more sophisticated AI pipeline, or (c) accepting static enemies for v1. **Default: static for now.**

### EquipmentSpriteId
- [ ] This field on `Item` is completely dead code — not consumed by any service or renderer. Plan to remove it entirely. **Objection?**

### Content Expansion
- [ ] Added ~25 new items, ~12 new skills, ~10 new recipes with class tagging. All placeholder names/stats. **Review the JSON files when convenient** — especially class assignments (which classes can use which weapons/skills).

### Narrative Canon (carried from BACKLOG.md)
- [ ] **Beat 9 hero lead** — dialogue has PC attacking legs to "buy Darius the opening" for the Tauros kill. Darius acts as primary striker. Intended, or should PC land the killing blow?
- [ ] **Sorcerer (beat 5)** — prose: unnamed, unwinnable. Game: "Risidian", winnable. Align?
- [ ] **Darius's weapon** — adopt black greatsword from prose?
- [ ] **Beat-2 tavern → Bria's Colosseum shelter?** Beat-7 goblins → bandits?
- [ ] **Araknos design** — bull (prose) vs spider (art)?
- [ ] **Defeat rule** — whole party down = defeat, or PC death = instant game over?

### Economy (Phase 2)
- [ ] Currency names, IAP tiers, ad-reward rates — need your input before Phase 2 starts.

---

## Answered / Done
*(Will move items here as they're resolved)*
