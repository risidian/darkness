using System;
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Logic
{
    public class CombatEngine : ICombatService
    {
        private readonly Random _random = new();

        public List<object> CalculateTurnOrder(List<Character> party, List<Enemy> enemies)
        {
            var participants = new List<(object Participant, int Initiative)>();

            foreach (var character in party)
            {
                int initiative = character.Dexterity + character.Speed + _random.Next(1, 11);
                participants.Add((character, initiative));
            }

            foreach (var enemy in enemies)
            {
                int initiative = enemy.DEX + enemy.Speed + _random.Next(1, 11);
                participants.Add((enemy, initiative));
            }

            return participants
                .OrderByDescending(p => p.Initiative)
                .ThenBy(p => p.Participant is Character ? 0 : 1)
                .ThenBy(p => (p.Participant as Character)?.Name ?? (p.Participant as Enemy)?.Name)
                .Select(p => p.Participant)
                .ToList();
        }

        public int CalculateDamage(Character attacker, Enemy defender, Skill? skill = null, ActionType action = ActionType.Standard, double? critRoll = null)
        {
            float dmgMult = skill?.DamageMultiplier ?? 1.0f;
            float armorPen = skill?.ArmorPenetration ?? 0.0f;
            int accMod = skill?.AccuracyModifier ?? 0;

            // Determine base attack stat (Strength for Physical, Intelligence for Magical)
            int baseAtk = (skill?.SkillType == "Magical") ? attacker.Intelligence : attacker.Strength;
            int totalAttack = (int)((baseAtk + (skill?.BasePower ?? 0)) * dmgMult);
            
            // Defense ignores portion based on armor penetration
            int targetDef = (int)(defender.Defense * (1.0f - armorPen));

            int damage = 0;
            if (totalAttack + targetDef > 0)
            {
                long squaredAttack = (long)totalAttack * totalAttack;
                damage = (int)(squaredAttack / (totalAttack + targetDef));
            }

            if (totalAttack > 0 && damage == 0) damage = 1;

            // Critical hit calculation
            double roll = critRoll ?? _random.NextDouble();
            if (roll < 0.05) damage = (int)(damage * 1.5);

            // Target Block reduction
            if (defender.IsBlocking)
            {
                damage = (int)(damage * 0.5); 
            }

            return Math.Max(0, damage);
        }

        public int CalculateDamage(Enemy attacker, Character defender, Skill? skill = null, ActionType action = ActionType.Standard, double? critRoll = null)
        {
            int totalAttack = attacker.Attack + (skill?.BasePower ?? 0);
            int totalDefense = defender.Defense;

            int damage = 0;
            if (totalAttack + totalDefense > 0)
            {
                long squaredAttack = (long)totalAttack * totalAttack;
                damage = (int)(squaredAttack / (totalAttack + totalDefense));
            }

            if (totalAttack > 0 && damage == 0) damage = 1;

            // Critical hit
            double roll = critRoll ?? _random.NextDouble();
            if (roll < 0.05) damage = (int)(damage * 1.5);

            // Target Block reduction for Player
            if (defender.IsBlocking)
            {
                float reduction = 0.05f; // Bare handed
                if (defender.ShieldType != "None" && defender.ShieldType != "") reduction = 0.60f; 
                else if (defender.WeaponType != "None" && defender.WeaponType != "") reduction = 0.20f;
                
                damage = (int)(damage * (1.0f - reduction));
            }

            return Math.Max(0, damage);
        }

        public int CalculateDamage(Character attacker, Character defender, Skill? skill = null, ActionType action = ActionType.Standard, double? critRoll = null)
        {
            float dmgMult = skill?.DamageMultiplier ?? 1.0f;
            float armorPen = skill?.ArmorPenetration ?? 0.0f;

            int baseAtk = (skill?.SkillType == "Magical") ? attacker.Intelligence : attacker.Strength;
            int totalAttack = (int)((baseAtk + (skill?.BasePower ?? 0)) * dmgMult);
            int totalDefense = (int)(defender.Defense * (1.0f - armorPen));

            int damage = (totalAttack + totalDefense > 0) ? (int)(((long)totalAttack * totalAttack) / (totalAttack + totalDefense)) : 0;
            if (totalAttack > 0 && damage == 0) damage = 1;

            if ((critRoll ?? _random.NextDouble()) < 0.05) damage = (int)(damage * 1.5);

            if (defender.IsBlocking)
            {
                float reduction = (defender.ShieldType != "None" && defender.ShieldType != "") ? 0.60f : ((defender.WeaponType != "None" && defender.WeaponType != "") ? 0.20f : 0.05f);
                damage = (int)(damage * (1.0f - reduction));
            }

            return Math.Max(0, damage);
        }

        public void ApplySkillCosts(Character attacker, Skill skill)
        {
            if (attacker == null || skill == null) return;
            attacker.Mana = Math.Max(0, attacker.Mana - skill.ManaCost);
            attacker.Stamina = Math.Max(0, attacker.Stamina - skill.StaminaCost);
        }

        public void ApplySkillCosts(Enemy attacker, Skill skill)
        {
            if (attacker == null || skill == null) return;
            attacker.Mana = Math.Max(0, attacker.Mana - skill.ManaCost);
            attacker.Stamina = Math.Max(0, attacker.Stamina - skill.StaminaCost);
        }

        public bool CheckStatusEffect(Character target, StatusEffect effect)
        {
            int resistance = target.Wisdom;
            return _random.Next(1, 101) > resistance;
        }

        public bool CheckStatusEffect(Enemy target, StatusEffect effect)
        {
            int resistance = target.WIS;
            return _random.Next(1, 101) > resistance;
        }
    }
}
