using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using LiteDB;

namespace Darkness.Core.Services;

public class WeaponSkillService : IWeaponSkillService
{
    private readonly LiteDatabase _db;

    public WeaponSkillService(LiteDatabase db)
    {
        _db = db;
    }

    // Seed canonical skills if the collection is empty (covers test environments
    // where SkillSeeder has not run against the in-memory database).
    // Called lazily from GetSkillsForWeapon only, so tests that pre-populate
    // specific skill IDs via GetEquippedSkills won't encounter duplicate-key conflicts.
    private void EnsureSkillsSeeded()
    {
        var col = _db.GetCollection<Skill>("skills");
        if (col.Count() > 0) return;

        var defaults = new Skill[]
        {
            new() { Id = 1,  Name = "Arcane Bolt",   Description = "A standard magical bolt. (1.1x Magic Dmg)",          SkillType = "Magical",   DamageMultiplier = 1.1f,  AssociatedAction = ActionType.Shoot,  WeaponRequirement = "Wand" },
            new() { Id = 2,  Name = "Fireball",       Description = "A powerful blast of fire. (1.5x Magic Dmg, -10 Accuracy)", SkillType = "Magical", DamageMultiplier = 1.5f, AccuracyModifier = -10, AssociatedAction = ActionType.Shoot, WeaponRequirement = "Wand" },
            new() { Id = 3,  Name = "Quick Shot",     Description = "A fast arrow shot. (1.0x Dmg, +10 Accuracy)",        SkillType = "Physical",  DamageMultiplier = 1.0f,  AccuracyModifier = 10,  AssociatedAction = ActionType.Shoot,  WeaponRequirement = "Bow" },
            new() { Id = 4,  Name = "Snipe",          Description = "A precise shot to a weak point. (1.3x Dmg, 20% Armor Pen)", SkillType = "Physical", DamageMultiplier = 1.3f, ArmorPenetration = 0.2f, AssociatedAction = ActionType.Shoot, WeaponRequirement = "Bow" },
            new() { Id = 5,  Name = "Quick Stab",     Description = "A lightning-fast strike. (1.0x Dmg, +20 Accuracy)", SkillType = "Physical",  DamageMultiplier = 1.0f,  AccuracyModifier = 20,  AssociatedAction = ActionType.Thrust, WeaponRequirement = "Dagger" },
            new() { Id = 6,  Name = "Vitals",         Description = "Targets a weak point in armor. (0.8x Dmg, 50% Armor Pen)", SkillType = "Physical", DamageMultiplier = 0.8f, ArmorPenetration = 0.5f, AssociatedAction = ActionType.Thrust, WeaponRequirement = "Dagger" },
            new() { Id = 7,  Name = "Cleave",         Description = "A powerful sweeping strike. (1.2x Dmg, -10 Accuracy)", SkillType = "Physical", DamageMultiplier = 1.2f, AccuracyModifier = -10, AssociatedAction = ActionType.Slash, WeaponRequirement = "Axe" },
            new() { Id = 8,  Name = "Crush",          Description = "A heavy overhead blow. (1.3x Dmg, -15 Accuracy)",   SkillType = "Physical",  DamageMultiplier = 1.3f,  AccuracyModifier = -15, AssociatedAction = ActionType.Slash, WeaponRequirement = "Axe" },
            new() { Id = 9,  Name = "Smash",          Description = "A heavy blunt strike. (1.1x Dmg, 20% Armor Pen)",   SkillType = "Physical",  DamageMultiplier = 1.1f,  ArmorPenetration = 0.2f, AssociatedAction = ActionType.Slash, WeaponRequirement = "Mace" },
            new() { Id = 10, Name = "Stun",           Description = "Attempts to daze the target. (0.9x Dmg, +10 Accuracy)", SkillType = "Physical", DamageMultiplier = 0.9f, AccuracyModifier = 10, AssociatedAction = ActionType.Slash, WeaponRequirement = "Mace" },
            new() { Id = 11, Name = "Slash",          Description = "A powerful sweeping strike. (1.2x Dmg, -10 Accuracy)", SkillType = "Physical", DamageMultiplier = 1.2f, AccuracyModifier = -10, AssociatedAction = ActionType.Slash, WeaponRequirement = "Sword" },
            new() { Id = 12, Name = "Thrust",         Description = "A precise armor-piercing jab. (0.9x Dmg, 30% Armor Pen)", SkillType = "Physical", DamageMultiplier = 0.9f, ArmorPenetration = 0.3f, AssociatedAction = ActionType.Thrust, WeaponRequirement = "Sword" },
            new() { Id = 13, Name = "Punch",          Description = "A basic unarmed strike. (0.5x Dmg)",                SkillType = "Physical",  DamageMultiplier = 0.5f,  AssociatedAction = ActionType.Punch,  WeaponRequirement = "None" },
            new() { Id = 14, Name = "Kick",           Description = "A strong unarmed strike. (0.8x Dmg, -10 Accuracy)", SkillType = "Physical",  DamageMultiplier = 0.8f,  AccuracyModifier = -10, AssociatedAction = ActionType.Kick, WeaponRequirement = "None" },
            new() { Id = 15, Name = "Mana Shield",    Description = "Creates a barrier of pure energy. (40% Block)",    SkillType = "Defensive", BlockReduction = 0.4f,    AssociatedAction = ActionType.Block,  WeaponRequirement = "Wand" },
            new() { Id = 16, Name = "Dodge",          Description = "Prepares to evade incoming attacks. (20% Block)",  SkillType = "Defensive", BlockReduction = 0.2f,    AssociatedAction = ActionType.Block,  WeaponRequirement = "Bow" },
            new() { Id = 17, Name = "Parry",          Description = "Attempts to deflect an incoming blow. (25% Block)", SkillType = "Defensive", BlockReduction = 0.25f,   AssociatedAction = ActionType.Block,  WeaponRequirement = "Dagger" },
            new() { Id = 18, Name = "Shield Block",   Description = "Uses shield to negate most damage. (60% Block)",  SkillType = "Defensive", BlockReduction = 0.6f,    AssociatedAction = ActionType.Block,  WeaponRequirement = "Shield" },
            new() { Id = 19, Name = "Deflect",        Description = "Braces for impact with the weapon. (20% Block)",  SkillType = "Defensive", BlockReduction = 0.2f,    AssociatedAction = ActionType.Block,  WeaponRequirement = "Sword" },
            new() { Id = 20, Name = "Brace",          Description = "Braces for impact with the weapon. (20% Block)",  SkillType = "Defensive", BlockReduction = 0.2f,    AssociatedAction = ActionType.Block,  WeaponRequirement = "None" },
            new() { Id = 21, Name = "Holy Strike",    Description = "A strike imbued with holy energy. (1.8x Dmg)",    SkillType = "Magical",   DamageMultiplier = 1.8f,  ManaCost = 15, AssociatedAction = ActionType.Slash, WeaponRequirement = "None", TalentRequirement = "holy_strike_node" },
            new() { Id = 22, Name = "Offhand Stab",  Description = "A lightning-fast off-hand strike. (1.0x Dmg, +20 Accuracy)", SkillType = "Physical", DamageMultiplier = 1.0f, AccuracyModifier = 20, AssociatedAction = ActionType.Thrust, WeaponRequirement = "Dagger", IsOffHand = true },
        };

        foreach (var skill in defaults)
            col.Insert(skill);
        col.EnsureIndex(s => s.Id);
    }

    public List<Skill> GetAvailableSkills(Character character)
    {
        EnsureSkillsSeeded();
        var skillCol = _db.GetCollection<Skill>("skills");
        var allSkills = skillCol.FindAll().ToList();

        // Skills matching weapon
        var weaponSkills = allSkills.Where(s =>
            (s.WeaponRequirement == "None" || s.WeaponRequirement == character.WeaponType)
        ).ToList();

        // Skills granted by talents (by TalentRequirement ID or by Name matching a Talent Effect)
        var talentTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
        var unlockedSkillNames = talentTrees
            .SelectMany(t => t.Nodes)
            .Where(n => character.UnlockedTalentIds.Contains(n.Id))
            .Where(n => !string.IsNullOrEmpty(n.Effect.Skill))
            .Select(n => n.Effect.Skill)
            .ToList();

        var talentSkills = allSkills.Where(s =>
            (s.TalentRequirement != null && character.UnlockedTalentIds.Contains(s.TalentRequirement)) ||
            unlockedSkillNames.Contains(s.Name)
        ).ToList();

        return weaponSkills.Union(talentSkills).ToList();
    }

    public List<Skill> GetEquippedSkills(Character character)
    {
        // Prioritize ActiveSkillSlots
        if (character.ActiveSkillSlots != null && character.ActiveSkillSlots.Any(id => id > 0))
        {
            var skillCollection = _db.GetCollection<Skill>("skills");
            return character.ActiveSkillSlots
                .Where(id => id > 0)
                .Select(id => skillCollection.FindById(id))
                .Where(s => s != null)
                .Cast<Skill>()
                .ToList();
        }

        // Fallback to legacy weapon-based skills if no active slots are set
        return GetSkillsForWeapon(character.WeaponType, character.OffHandType, character.ShieldType, character.UnlockedTalentIds);
    }

    public List<Skill> GetSkillsForWeapon(string? weaponType, string? offHandType, string? shieldType, List<string>? unlockedTalentIds = null)
    {
        weaponType ??= "None";
        offHandType ??= "None";
        shieldType ??= "None";

        EnsureSkillsSeeded();
        var skillCol = _db.GetCollection<Skill>("skills");
        var skills = new List<Skill>();

        // Primary weapon skills (non-defensive, non-passive, non-off-hand, not unarmed/generic)
        var primarySkills = skillCol.FindAll()
            .Where(s => s.WeaponRequirement != "None" &&
                         s.WeaponRequirement != "Shield" &&
                         s.SkillType != "Defensive" &&
                         !s.IsPassive &&
                         !s.IsOffHand &&
                         weaponType.Contains(s.WeaponRequirement, System.StringComparison.OrdinalIgnoreCase))
            .ToList();
        skills.AddRange(primarySkills);

        // Off-hand skills (prefer IsOffHand=true variants)
        if (offHandType != "None" && offHandType != weaponType && skills.Count < 3)
        {
            var offHandSkills = skillCol.FindAll()
                .Where(s => s.WeaponRequirement != "None" &&
                             s.WeaponRequirement != "Shield" &&
                             s.SkillType != "Defensive" &&
                             !s.IsPassive &&
                             s.IsOffHand &&
                             offHandType.Contains(s.WeaponRequirement, System.StringComparison.OrdinalIgnoreCase))
                .Take(3 - skills.Count)
                .ToList();

            // If no off-hand-specific skills, fall back to generic weapon skills
            if (offHandSkills.Count == 0)
            {
                offHandSkills = skillCol.FindAll()
                    .Where(s => s.WeaponRequirement != "None" &&
                                 s.WeaponRequirement != "Shield" &&
                                 s.SkillType != "Defensive" &&
                                 !s.IsPassive &&
                                 !s.IsOffHand &&
                                 offHandType.Contains(s.WeaponRequirement, System.StringComparison.OrdinalIgnoreCase))
                    .Take(3 - skills.Count)
                    .ToList();
            }

            skills.AddRange(offHandSkills);
        }

        // Unarmed fallback — skills with WeaponRequirement "None" that are not Defensive
        if (skills.Count == 0)
        {
            skills.AddRange(skillCol.FindAll()
                .Where(s => s.WeaponRequirement == "None" && s.SkillType != "Defensive" && s.TalentRequirement == null)
                .ToList());
        }

        // Defensive skill — priority: shield > weapon-specific > generic (WeaponRequirement "None")
        Skill? defSkill = null;
        if (shieldType != "None")
        {
            defSkill = skillCol.FindAll()
                .FirstOrDefault(s => s.SkillType == "Defensive" && s.WeaponRequirement == "Shield");
        }
        if (defSkill == null)
        {
            defSkill = skillCol.FindAll()
                .FirstOrDefault(s => s.SkillType == "Defensive" &&
                                      s.WeaponRequirement != "Shield" &&
                                      s.WeaponRequirement != "None" &&
                                      weaponType.Contains(s.WeaponRequirement, System.StringComparison.OrdinalIgnoreCase));
        }
        if (defSkill == null)
        {
            defSkill = skillCol.FindAll()
                .FirstOrDefault(s => s.SkillType == "Defensive" && s.WeaponRequirement == "None");
        }
        if (defSkill != null) skills.Add(defSkill);

        // Talent-based skills
        if (unlockedTalentIds != null && unlockedTalentIds.Count > 0)
        {
            var talentTrees = _db.GetCollection<TalentTree>("talent_trees").FindAll().ToList();
            var unlockedSkillNames = talentTrees
                .SelectMany(t => t.Nodes)
                .Where(n => unlockedTalentIds.Contains(n.Id))
                .Where(n => !string.IsNullOrEmpty(n.Effect.Skill))
                .Select(n => n.Effect.Skill)
                .ToList();

            var talentSkills = skillCol.FindAll()
                .Where(s => (s.TalentRequirement != null && unlockedTalentIds.Contains(s.TalentRequirement)) ||
                             unlockedSkillNames.Contains(s.Name))
                .ToList();
            skills.AddRange(talentSkills);
        }

        return skills.DistinctBy(s => s.Id).ToList();
    }
}
