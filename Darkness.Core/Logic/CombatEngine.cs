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
                int initiative = character.DEX + character.Speed + _random.Next(1, 11);
                participants.Add((character, initiative));
            }

            foreach (var enemy in enemies)
            {
                int initiative = enemy.DEX + enemy.Speed + _random.Next(1, 11);
                participants.Add((enemy, initiative));
            }

            return participants
                .OrderByDescending(p => p.Initiative)
                .Select(p => p.Participant)
                .ToList();
        }

        public int CalculateDamage(Character attacker, Enemy defender, Skill? skill = null)
        {
            // Deduct costs if a skill is used
            if (skill != null)
            {
                attacker.Mana -= skill.ManaCost;
                attacker.Stamina -= skill.StaminaCost;
            }

            int baseAttack = attacker.STR; // Using STR as base attack for Character
            int power = skill?.BasePower ?? 0;
            int totalAttack = baseAttack + power;

            int damage = (totalAttack * 2) - defender.Defense;

            // Critical hit calculation (Base 5% as Luck is not present)
            bool isCritical = _random.NextDouble() < 0.05;
            if (isCritical)
            {
                damage = (int)(damage * 1.5);
            }

            // Ensure damage is at least 0
            damage = Math.Max(0, damage);

            return damage;
        }

        public int CalculateDamage(Enemy attacker, Character defender, Skill? skill = null)
        {
            // Deduct costs if a skill is used
            if (skill != null)
            {
                attacker.Mana -= skill.ManaCost;
                attacker.Stamina -= skill.StaminaCost;
            }

            int baseAttack = attacker.Attack;
            int power = skill?.BasePower ?? 0;
            int totalAttack = baseAttack + power;

            int damage = (totalAttack * 2) - defender.Defense;

            // Critical hit calculation (Base 5% as Luck is not present)
            bool isCritical = _random.NextDouble() < 0.05;
            if (isCritical)
            {
                damage = (int)(damage * 1.5);
            }

            // Ensure damage is at least 0
            damage = Math.Max(0, damage);

            return damage;
        }

        public bool CheckStatusEffect(Character target, StatusEffect effect)
        {
            // Resistance logic: Base chance could be influenced by WIS or something similar.
            // For now, let's use a simple base chance modified by Magnitude or similar.
            // Using a simple logic: if random (0-100) > target.WIS (resistance factor), effect is applied.
            int resistance = target.WIS;
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
