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

    public List<Skill> GetAvailableSkills(Character character)
    {
        // New implementation: Use ActiveSkillSlots to determine active skills
        var skillCollection = _db.GetCollection<Skill>("skills");
        var activeSkills = character.ActiveSkillSlots
            .Where(id => id > 0)
            .Select(id => skillCollection.FindById(id))
            .Where(s => s != null)
            .ToList();
        
        return activeSkills!;
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
        var skills = new List<Skill>();
        weaponType ??= "None";
        offHandType ??= "None";
        shieldType ??= "None";

        // Helper to get skills for a single weapon
        List<Skill> GetBaseSkills(string type, bool isOffHand)
        {
            var baseSkills = new List<Skill>();
            if (type.Contains("Wand", System.StringComparison.OrdinalIgnoreCase))
            {
                baseSkills.Add(new Skill
                {
                    Name = "Arcane Bolt", Description = "A standard magical bolt. (1.1x Magic Dmg)", SkillType = "Magical",
                    DamageMultiplier = 1.1f, AssociatedAction = ActionType.Shoot
                });
                baseSkills.Add(new Skill
                {
                    Name = "Fireball", Description = "A powerful blast of fire. (1.5x Magic Dmg, -10 Accuracy)",
                    SkillType = "Magical", DamageMultiplier = 1.5f, AccuracyModifier = -10,
                    AssociatedAction = ActionType.Shoot
                });
            }
            else if (type.Contains("Bow", System.StringComparison.OrdinalIgnoreCase))
            {
                baseSkills.Add(new Skill
                {
                    Name = "Quick Shot", Description = "A fast arrow shot. (1.0x Dmg, +10 Accuracy)",
                    DamageMultiplier = 1.0f, AccuracyModifier = 10, AssociatedAction = ActionType.Shoot
                });
                baseSkills.Add(new Skill
                {
                    Name = "Snipe", Description = "A precise shot to a weak point. (1.3x Dmg, 20% Armor Pen)",
                    DamageMultiplier = 1.3f, ArmorPenetration = 0.2f, AssociatedAction = ActionType.Shoot
                });
            }
            else if (type.Contains("Dagger", System.StringComparison.OrdinalIgnoreCase))
            {
                baseSkills.Add(new Skill
                {
                    Name = isOffHand ? "Offhand Stab" : "Quick Stab", 
                    Description = "A lightning-fast strike. (1.0x Dmg, +20 Accuracy)",
                    DamageMultiplier = 1.0f, AccuracyModifier = 20, AssociatedAction = ActionType.Thrust
                });
                if (!isOffHand)
                {
                    baseSkills.Add(new Skill
                    {
                        Name = "Vitals", Description = "Targets a weak point in armor. (0.8x Dmg, 50% Armor Pen)",
                        DamageMultiplier = 0.8f, ArmorPenetration = 0.5f, AssociatedAction = ActionType.Thrust
                    });
                }
            }
            else if (type.Contains("Axe", System.StringComparison.OrdinalIgnoreCase))
            {
                baseSkills.Add(new Skill
                {
                    Name = "Cleave", Description = "A powerful sweeping strike. (1.2x Dmg, -10 Accuracy)",
                    DamageMultiplier = 1.2f, AccuracyModifier = -10, AssociatedAction = ActionType.Slash
                });
                baseSkills.Add(new Skill
                {
                    Name = "Crush", Description = "A heavy overhead blow. (1.3x Dmg, -15 Accuracy)",
                    DamageMultiplier = 1.3f, AccuracyModifier = -15, AssociatedAction = ActionType.Slash
                });
            }
            else if (type.Contains("Mace", System.StringComparison.OrdinalIgnoreCase))
            {
                baseSkills.Add(new Skill
                {
                    Name = "Smash", Description = "A heavy blunt strike. (1.1x Dmg, 20% Armor Pen)",
                    DamageMultiplier = 1.1f, ArmorPenetration = 0.2f, AssociatedAction = ActionType.Slash
                });
                baseSkills.Add(new Skill
                {
                    Name = "Stun", Description = "Attempts to daze the target. (0.9x Dmg, +10 Accuracy)",
                    DamageMultiplier = 0.9f, AccuracyModifier = 10, AssociatedAction = ActionType.Slash
                });
            }
            else if (type.Contains("Sword", System.StringComparison.OrdinalIgnoreCase))
            {
                baseSkills.Add(new Skill
                {
                    Name = "Slash", Description = "A powerful sweeping strike. (1.2x Dmg, -10 Accuracy)",
                    DamageMultiplier = 1.2f, AccuracyModifier = -10, AssociatedAction = ActionType.Slash
                });
                baseSkills.Add(new Skill
                {
                    Name = "Thrust", Description = "A precise armor-piercing jab. (0.9x Dmg, 30% Armor Pen)",
                    DamageMultiplier = 0.9f, ArmorPenetration = 0.3f, AssociatedAction = ActionType.Thrust
                });
            }
            
            return baseSkills;
        }

        // Add primary weapon skills
        skills.AddRange(GetBaseSkills(weaponType, false));

        // Add off-hand skills if applicable
        if (offHandType != "None" && offHandType != weaponType)
        {
            var offHandSkills = GetBaseSkills(offHandType, true);
            // Limit to 3 total active skills for now
            if (skills.Count < 3) skills.AddRange(offHandSkills);
        }

        // Default to punch/kick if still empty
        if (skills.Count == 0)
        {
            skills.Add(new Skill
            {
                Name = "Punch", Description = "A basic unarmed strike. (0.5x Dmg)", DamageMultiplier = 0.5f,
                AssociatedAction = ActionType.Punch
            });
            skills.Add(new Skill
            {
                Name = "Kick", Description = "A strong unarmed strike. (0.8x Dmg, -10 Accuracy)",
                DamageMultiplier = 0.8f, AccuracyModifier = -10, AssociatedAction = ActionType.Kick
            });
        }

        // Add defensive skill based on setup
        if (weaponType.Contains("Wand", System.StringComparison.OrdinalIgnoreCase))
        {
            skills.Add(new Skill
            {
                Name = "Mana Shield", Description = "Creates a barrier of pure energy. (40% Block)",
                SkillType = "Defensive", BlockReduction = 0.4f, AssociatedAction = ActionType.Block
            });
        }
        else if (weaponType.Contains("Bow", System.StringComparison.OrdinalIgnoreCase))
        {
            skills.Add(new Skill
            {
                Name = "Dodge", Description = "Prepares to evade incoming attacks. (20% Block)",
                SkillType = "Defensive", BlockReduction = 0.2f, AssociatedAction = ActionType.Block
            });
        }
        else if (weaponType.Contains("Dagger", System.StringComparison.OrdinalIgnoreCase) && offHandType == "None")
        {
            skills.Add(new Skill
            {
                Name = "Parry", Description = "Attempts to deflect an incoming blow. (25% Block)",
                SkillType = "Defensive", BlockReduction = 0.25f, AssociatedAction = ActionType.Block
            });
        }
        else
        {
            string blockName = shieldType != "None" ? "Shield Block" : (weaponType.Contains("Sword") ? "Deflect" : "Brace");
            float blockVal = shieldType != "None" ? 0.6f : 0.2f;
            string blockDesc = shieldType != "None"
                ? $"Uses shield to negate most damage. ({blockVal * 100}% Block)"
                : $"Braces for impact with the weapon. ({blockVal * 100}% Block)";

            skills.Add(new Skill
            {
                Name = blockName, Description = blockDesc, SkillType = "Defensive", BlockReduction = blockVal,
                AssociatedAction = ActionType.Block
            });
        }

        // Add skills from unlocked talents
        if (unlockedTalentIds != null && unlockedTalentIds.Count > 0)
        {
            var trees = _db.GetCollection<TalentTree>("talent_trees").FindAll();
            foreach (var tree in trees)
            {
                foreach (var node in tree.Nodes)
                {
                    if (unlockedTalentIds.Contains(node.Id) && !string.IsNullOrEmpty(node.Effect.Skill))
                    {
                        // Create a skill for the talent
                        var talentSkill = new Skill
                        {
                            Name = node.Effect.Skill,
                            Description = node.Description,
                            SkillType = "Magical", // Defaulting to Magical for talent-based skills
                            DamageMultiplier = 1.5f,
                            AssociatedAction = ActionType.Slash
                        };

                        // Specific logic for Holy Strike as an example
                        if (node.Effect.Skill == "Holy Strike")
                        {
                            talentSkill.Description = "A strike imbued with holy energy. (1.8x Dmg)";
                            talentSkill.DamageMultiplier = 1.8f;
                            talentSkill.ManaCost = 15;
                        }

                        skills.Add(talentSkill);
                    }
                }
            }
        }

        return skills;
    }
}
