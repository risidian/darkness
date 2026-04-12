using System.Collections.Generic;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services;

public class WeaponSkillService : IWeaponSkillService
{
    public List<Skill> GetSkillsForWeapon(string? weaponType, string? offHandType, string? shieldType)
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
                    DamageMultiplier = 1.1f, AssociatedAction = ActionType.Cast
                });
                baseSkills.Add(new Skill
                {
                    Name = "Fireball", Description = "A powerful blast of fire. (1.5x Magic Dmg, -10 Accuracy)",
                    SkillType = "Magical", DamageMultiplier = 1.5f, AccuracyModifier = -10,
                    AssociatedAction = ActionType.Cast
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

        return skills;
    }
}
