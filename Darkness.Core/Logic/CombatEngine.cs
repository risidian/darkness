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

        public int CalculateDamage(Character attacker, Enemy defender, Skill? skill = null, double? critRoll = null)
        {
            int baseAttack = attacker.Strength; // Using Strength as base attack for Character
            int power = skill?.BasePower ?? 0;
            int totalAttack = baseAttack + power;

            int damage = 0;
            if (totalAttack + defender.Defense > 0)
            {
                long squaredAttack = (long)totalAttack * totalAttack;
                damage = (int)(squaredAttack / (totalAttack + defender.Defense));
            }

            if (totalAttack > 0 && damage == 0)
            {
                damage = 1;
            }

            // Critical hit calculation (Base 5% as Luck is not present)
            double roll = critRoll ?? _random.NextDouble();
            bool isCritical = roll < 0.05;
            if (isCritical)
            {
                damage = (int)(damage * 1.5);
            }

            // Ensure damage is at least 0
            damage = Math.Max(0, damage);

            return damage;
        }

        public int CalculateDamage(Enemy attacker, Character defender, Skill? skill = null, double? critRoll = null)
        {
            int baseAttack = attacker.Attack;
            int power = skill?.BasePower ?? 0;
            int totalAttack = baseAttack + power;

            int damage = 0;
            if (totalAttack + defender.Defense > 0)
            {
                long squaredAttack = (long)totalAttack * totalAttack;
                damage = (int)(squaredAttack / (totalAttack + defender.Defense));
            }

            if (totalAttack > 0 && damage == 0)
            {
                damage = 1;
            }

            // Critical hit calculation (Base 5% as Luck is not present)
            double roll = critRoll ?? _random.NextDouble();
            bool isCritical = roll < 0.05;
            if (isCritical)
            {
                damage = (int)(damage * 1.5);
            }

            // Ensure damage is at least 0
            damage = Math.Max(0, damage);

            return damage;
        }

        public int CalculateDamage(Character attacker, Character defender, Skill? skill = null, double? critRoll = null)
        {
            int baseAttack = attacker.Strength;
            int power = skill?.BasePower ?? 0;
            int totalAttack = baseAttack + power;

            int damage = 0;
            if (totalAttack + defender.Defense > 0)
            {
                long squaredAttack = (long)totalAttack * totalAttack;
                damage = (int)(squaredAttack / (totalAttack + defender.Defense));
            }

            if (totalAttack > 0 && damage == 0)
            {
                damage = 1;
            }

            // Critical hit calculation (Base 5% as Luck is not present)
            double roll = critRoll ?? _random.NextDouble();
            bool isCritical = roll < 0.05;
            if (isCritical)
            {
                damage = (int)(damage * 1.5);
            }

            // Ensure damage is at least 0
            damage = Math.Max(0, damage);

            return damage;
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
            // Resistance logic: Base chance could be influenced by Wisdom or something similar.
            // For now, let's use a simple base chance modified by Magnitude or similar.
            // Using a simple logic: if random (0-100) > target.Wisdom (resistance factor), effect is applied.
            int resistance = target.Wisdom;
            int roll = _random.Next(1, 101);
            
            return roll > resistance;
        }

        public bool CheckStatusEffect(Enemy target, StatusEffect effect)
        {
            int resistance = target.WIS;
            int roll = _random.Next(1, 101);
            
            return roll > resistance;
        }
    }
}
