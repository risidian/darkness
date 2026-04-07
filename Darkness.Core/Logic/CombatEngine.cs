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

        private int GetModifier(int stat)
        {
            return (int)Math.Floor((stat - 10) / 2.0);
        }

        private int RollDice(string diceStr)
        {
            if (string.IsNullOrWhiteSpace(diceStr)) return 0;
            var parts = diceStr.ToLower().Split('d');
            if (parts.Length != 2 || !int.TryParse(parts[0], out int numDice) ||
                !int.TryParse(parts[1], out int diceSides))
            {
                return 0; // Fallback
            }

            int total = 0;
            for (int i = 0; i < numDice; i++)
            {
                total += _random.Next(1, diceSides + 1);
            }

            return total;
        }

        public List<object> CalculateTurnOrder(List<Character> party, List<Enemy> enemies)
        {
            var participants = new List<(object Participant, int Initiative)>();

            foreach (var character in party)
            {
                int initiative = _random.Next(1, 21) + GetModifier(character.Dexterity);
                participants.Add((character, initiative));
            }

            foreach (var enemy in enemies)
            {
                int initiative = _random.Next(1, 21) + GetModifier(enemy.DEX);
                participants.Add((enemy, initiative));
            }

            return participants
                .OrderByDescending(p => p.Initiative)
                .ThenBy(p => p.Participant is Character ? 0 : 1)
                .ThenBy(p => (p.Participant as Character)?.Name ?? (p.Participant as Enemy)?.Name)
                .Select(p => p.Participant)
                .ToList();
        }

        public CombatResult CalculateDamage(Character attacker, Enemy defender, Skill? skill = null,
            ActionType action = ActionType.Standard, double? critRoll = null)
        {
            var result = new CombatResult();

            bool isMagical = skill?.SkillType == "Magical";
            int attackStat = isMagical ? attacker.Intelligence : attacker.Strength;
            int attackModifier = GetModifier(attackStat);
            result.AttackModifier = attackModifier;
            int targetAC = defender.Defense;
            
            int d20Roll = critRoll.HasValue ? (int)(critRoll.Value * 20) + 1 : _random.Next(1, 21);
            result.D20Roll = d20Roll;
            if (d20Roll == 20) 
                result.IsCriticalHit = true;
            if (d20Roll == 1) 
                result.IsCriticalMiss = true;

            int totalAttackBonus = attackModifier; // this was causing random behaviour with missing+ (skill?.AccuracyModifier ?? 0);

            if (result.IsCriticalHit) 
                result.IsHit = true;
            else if (result.IsCriticalMiss) 
                result.IsHit = false;
            else 
                result.IsHit = (d20Roll + totalAttackBonus) >= targetAC;
            result.TargetAC = targetAC;

            if (!result.IsHit)
            {
                //debugging
                //result.DamageDice = $"Not hit return early {(d20Roll + totalAttackBonus)} >= {targetAC} {attackModifier} {skill?.AccuracyModifier}";
                return result;
            }

            string diceStr = skill?.DamageDice ?? "1d4";

            int damageRoll = RollDice(diceStr);
            if (result.IsCriticalHit) 
                damageRoll += RollDice(diceStr);

            float dmgMult;
            dmgMult = skill?.DamageMultiplier ?? 1.0f;

            if (dmgMult < 1f)
                dmgMult = 1.0f; // Ensure multiplier is never zero to avoid nullifying damage
            int totalDamage = (int)((damageRoll + attackModifier + (skill?.BasePower ?? 0)) * dmgMult);

            if (defender.IsBlocking) 
                totalDamage = (int)(totalDamage * 0.5f);

            result.DamageDealt = Math.Max(1, totalDamage);
            result.DamageMultiplier = skill?.DamageMultiplier;
            result.DamageDice = diceStr;
            result.DamageRoll = damageRoll;
            return result;
        }

        public CombatResult CalculateDamage(Enemy attacker, Character defender, Skill? skill = null,
            ActionType action = ActionType.Standard, double? critRoll = null)
        {
            var result = new CombatResult();

            int attackModifier = GetModifier(attacker.STR);

            int targetAC = 10 + defender.ArmorClass + GetModifier(defender.Dexterity);

            int d20Roll = critRoll.HasValue ? (int)(critRoll.Value * 20) + 1 : _random.Next(1, 21);
            if (d20Roll == 20) result.IsCriticalHit = true;
            if (d20Roll == 1) result.IsCriticalMiss = true;

            int totalAttackBonus = attackModifier + attacker.Accuracy + (skill?.AccuracyModifier ?? 0);

            if (result.IsCriticalHit) result.IsHit = true;
            else if (result.IsCriticalMiss) result.IsHit = false;
            else result.IsHit = (d20Roll + totalAttackBonus) >= targetAC;

            if (!result.IsHit) return result;

            string diceStr = skill?.DamageDice ?? "1d6";

            int damageRoll = RollDice(diceStr);
            if (result.IsCriticalHit) damageRoll += RollDice(diceStr);

            int totalDamage = damageRoll + attackModifier + attacker.Attack + (skill?.BasePower ?? 0);

            if (defender.IsBlocking)
            {
                float reduction = 0.05f;
                if (defender.ShieldType != "None" && defender.ShieldType != "") reduction = 0.60f;
                else if (defender.WeaponType != "None" && defender.WeaponType != "") reduction = 0.20f;
                totalDamage = (int)(totalDamage * (1.0f - reduction));
            }

            result.DamageDealt = Math.Max(1, totalDamage);
            return result;
        }

        public CombatResult CalculateDamage(Character attacker, Character defender, Skill? skill = null,
            ActionType action = ActionType.Standard, double? critRoll = null)
        {
            var result = new CombatResult();

            bool isMagical = skill?.SkillType == "Magical";
            int attackStat = isMagical ? attacker.Intelligence : attacker.Strength;
            int attackModifier = GetModifier(attackStat);

            int targetAC = 10 + defender.ArmorClass + GetModifier(defender.Dexterity);

            int d20Roll = critRoll.HasValue ? (int)(critRoll.Value * 20) + 1 : _random.Next(1, 21);
            if (d20Roll == 20) result.IsCriticalHit = true;
            if (d20Roll == 1) result.IsCriticalMiss = true;

            int totalAttackBonus = attackModifier + (skill?.AccuracyModifier ?? 0);

            if (result.IsCriticalHit) result.IsHit = true;
            else if (result.IsCriticalMiss) result.IsHit = false;
            else result.IsHit = (d20Roll + totalAttackBonus) >= targetAC;

            if (!result.IsHit) return result;

            string diceStr = skill?.DamageDice ?? "1d4";

            int damageRoll = RollDice(diceStr);
            if (result.IsCriticalHit) damageRoll += RollDice(diceStr);

            float dmgMult = skill?.DamageMultiplier ?? 1.0f;
            int totalDamage = (int)((damageRoll + attackModifier + (skill?.BasePower ?? 0)) * dmgMult);

            if (defender.IsBlocking)
            {
                float reduction = 0.05f;
                if (defender.ShieldType != "None" && defender.ShieldType != "") reduction = 0.60f;
                else if (defender.WeaponType != "None" && defender.WeaponType != "") reduction = 0.20f;
                totalDamage = (int)(totalDamage * (1.0f - reduction));
            }

            result.DamageDealt = Math.Max(1, totalDamage);
            return result;
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