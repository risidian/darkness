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

        private CombatResult CalculateDamageInternal(
            int attackStat, int attackerAccuracy, int attackerAttackBonus,
            int targetAC, bool isMagical,
            bool defenderBlocking, string defenderShieldType, string defenderWeaponType,
            Skill? skill, double? critRoll)
        {
            var result = new CombatResult();
            int attackModifier = GetModifier(attackStat);
            result.AttackModifier = attackModifier;

            float armorPen = skill?.ArmorPenetration ?? 0f;
            int effectiveAC = (int)(targetAC * (1.0f - armorPen));

            int d20Roll = critRoll.HasValue ? (int)(critRoll.Value * 20) + 1 : _random.Next(1, 21);
            result.D20Roll = d20Roll;
            if (d20Roll == 20) result.IsCriticalHit = true;
            if (d20Roll == 1) result.IsCriticalMiss = true;

            int baseAttackRoll = d20Roll + attackModifier + attackerAccuracy;

            // AccuracyModifier is a percentage (+20 = +20%, -10 = -10%)
            int accuracyMod = skill?.AccuracyModifier ?? 0;
            int totalAttackRoll = accuracyMod != 0
                ? (int)(baseAttackRoll * (1.0f + accuracyMod / 100f))
                : baseAttackRoll;

            if (result.IsCriticalHit) result.IsHit = true;
            else if (result.IsCriticalMiss) result.IsHit = false;
            else result.IsHit = totalAttackRoll >= effectiveAC;
            result.TargetAC = targetAC;

            if (!result.IsHit) return result;

            string diceStr = skill?.DamageDice ?? "1d4";
            int damageRoll = RollDice(diceStr);
            if (result.IsCriticalHit) damageRoll += RollDice(diceStr);

            float dmgMult = skill?.DamageMultiplier ?? 1.0f;
            if (dmgMult < 0.1f) dmgMult = 0.1f;

            int totalDamage = (int)((damageRoll + attackModifier + attackerAttackBonus + (skill?.BasePower ?? 0)) * dmgMult);

            if (defenderBlocking)
            {
                float reduction = 0.05f;
                if (!string.IsNullOrEmpty(defenderShieldType) && defenderShieldType != "None") reduction = 0.60f;
                else if (!string.IsNullOrEmpty(defenderWeaponType) && defenderWeaponType != "None") reduction = 0.20f;
                totalDamage = (int)(totalDamage * (1.0f - reduction));
            }

            result.DamageDealt = Math.Max(1, totalDamage);
            result.DamageMultiplier = skill?.DamageMultiplier;
            result.DamageDice = diceStr;
            result.DamageRoll = damageRoll;
            return result;
        }

        public CombatResult CalculateDamage(Character attacker, Enemy defender, Skill? skill = null,
            ActionType action = ActionType.Standard, double? critRoll = null)
        {
            bool isMagical = skill?.SkillType == "Magical";
            int attackStat = isMagical ? attacker.Intelligence : attacker.Strength;
            return CalculateDamageInternal(attackStat, attacker.Accuracy, 0,
                defender.Defense, isMagical,
                defender.IsBlocking, "", "",
                skill, critRoll);
        }

        public CombatResult CalculateDamage(Enemy attacker, Character defender, Skill? skill = null,
            ActionType action = ActionType.Standard, double? critRoll = null)
        {
            int targetAC = 10 + defender.ArmorClass + GetModifier(defender.Dexterity);
            return CalculateDamageInternal(attacker.STR, attacker.Accuracy, attacker.Attack,
                targetAC, false,
                defender.IsBlocking, defender.ShieldType ?? "", defender.WeaponType ?? "",
                skill, critRoll);
        }

        public CombatResult CalculateDamage(Character attacker, Character defender, Skill? skill = null,
            ActionType action = ActionType.Standard, double? critRoll = null)
        {
            bool isMagical = skill?.SkillType == "Magical";
            int attackStat = isMagical ? attacker.Intelligence : attacker.Strength;
            int targetAC = 10 + defender.ArmorClass + GetModifier(defender.Dexterity);
            return CalculateDamageInternal(attackStat, attacker.Accuracy, 0,
                targetAC, isMagical,
                defender.IsBlocking, defender.ShieldType ?? "", defender.WeaponType ?? "",
                skill, critRoll);
        }

        public void HandleTurnStart(Character character)
        {
            if (character == null) return;
            
            // Regenerate Stamina: CON / 2
            int staminaRegen = Math.Max(1, character.Constitution / 2);
            character.Stamina = Math.Min(character.MaxStamina, character.Stamina + staminaRegen);
            
            // Regenerate Mana: WIS / 2
            int manaRegen = Math.Max(1, character.Wisdom / 2);
            character.Mana = Math.Min(character.MaxMana, character.Mana + manaRegen);
            
            character.IsBlocking = false;
        }

        public void HandleTurnStart(Enemy enemy)
        {
            if (enemy == null) return;

            int maxStamina = enemy.CON * 5;
            int staminaRegen = Math.Max(1, enemy.CON / 2);
            enemy.Stamina = Math.Min(maxStamina, enemy.Stamina + staminaRegen);

            int maxMana = enemy.WIS * 5;
            int manaRegen = Math.Max(1, enemy.WIS / 2);
            enemy.Mana = Math.Min(maxMana, enemy.Mana + manaRegen);

            enemy.IsBlocking = false;
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
            // Roll must exceed resistance to apply effect (higher wisdom = harder to affect)
            int resistance = Math.Clamp(target.Wisdom, 0, 95);
            return _random.Next(1, 101) > resistance;
        }

        public bool CheckStatusEffect(Enemy target, StatusEffect effect)
        {
            int resistance = Math.Clamp(target.WIS, 0, 95);
            return _random.Next(1, 101) > resistance;
        }
    }
}
