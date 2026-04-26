using System.Collections.Generic;
using Darkness.Core.Logic;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Logic
{
    public class CombatEngineTests
    {
        private readonly CombatEngine _engine = new();

        [Fact]
        public void CalculateDamage_CriticalHit_AlwaysHits()
        {
            var attacker = new Character { BaseStrength = 10 };
            var defender = new Enemy { Defense = 100 };

            var result = _engine.CalculateDamage(attacker, defender, critRoll: 0.99); // Maps to 20

            Assert.True(result.IsHit);
            Assert.True(result.IsCriticalHit);
            Assert.False(result.IsCriticalMiss);
            Assert.True(result.DamageDealt > 0);
        }

        [Fact]
        public void CalculateDamage_CriticalMiss_AlwaysMisses()
        {
            var attacker = new Character { BaseStrength = 30 };
            var defender = new Enemy { Defense = 1 };

            var result = _engine.CalculateDamage(attacker, defender, critRoll: 0.0); // Maps to 1

            Assert.False(result.IsHit);
            Assert.True(result.IsCriticalMiss);
            Assert.Equal(0, result.DamageDealt);
        }

        [Fact]
        public void CalculateTurnOrder_ReturnsCorrectCount()
        {
            var c1 = new Character { Name = "C1", BaseDexterity = 20 };
            var e1 = new Enemy { Name = "E1", DEX = 10 };

            var order = _engine.CalculateTurnOrder(new List<Character> { c1 }, new List<Enemy> { e1 });

            Assert.Equal(2, order.Count);
        }

        // === AccuracyModifier (percentage-based) ===
        [Fact]
        public void CalculateDamage_CharVsEnemy_AccuracyModifierAppliedAsPercentage()
        {
            var attacker = new Character { BaseStrength = 10 };
            var defender = new Enemy { Defense = 13 };
            var skill = new Skill { AccuracyModifier = 20, DamageDice = "1d4" };
            // baseRoll = d20(11) + statMod(0) + accuracy(0) = 11
            // +20% accuracy → 11 * 1.20 = 13.2 → 13 >= 13 → hit
            var result = _engine.CalculateDamage(attacker, defender, skill, critRoll: 0.5);
            Assert.True(result.IsHit);
        }

        [Fact]
        public void CalculateDamage_CharVsEnemy_NegativeAccuracyIsPercentageNotFlat()
        {
            var attacker = new Character { BaseStrength = 10 };
            var defender = new Enemy { Defense = 8 };
            var skill = new Skill { AccuracyModifier = -10, DamageDice = "1d4" };
            // baseRoll = d20(11) + statMod(0) = 11
            // If flat: 11 + (-10) = 1 < 8 → miss (WRONG)
            // If percentage: 11 * 0.90 = 9.9 → 9 >= 8 → hit (CORRECT)
            var result = _engine.CalculateDamage(attacker, defender, skill, critRoll: 0.5);
            Assert.True(result.IsHit); // Percentage-based, not flat
        }

        // === ArmorPenetration ===
        [Fact]
        public void CalculateDamage_CharVsEnemy_ArmorPenetrationReducesAC()
        {
            var attacker = new Character { BaseStrength = 10 };
            var defender = new Enemy { Defense = 20 };
            var skill = new Skill { ArmorPenetration = 0.5f, DamageDice = "1d4" };
            // 50% pen → effective AC = 10. d20=11+0=11 >= 10
            var result = _engine.CalculateDamage(attacker, defender, skill, critRoll: 0.5);
            Assert.True(result.IsHit);
        }

        // === Sub-1.0 DamageMultiplier ===
        [Fact]
        public void CalculateDamage_CharVsEnemy_SubOneDamageMultiplierAllowed()
        {
            var attacker = new Character { BaseStrength = 10 };
            var defender = new Enemy { Defense = 1 };
            var skill = new Skill { DamageMultiplier = 0.5f, DamageDice = "1d4" };
            var result = _engine.CalculateDamage(attacker, defender, skill, critRoll: 0.99);
            Assert.True(result.IsHit);
            Assert.True(result.DamageDealt >= 1);
        }

        // === Enemy-vs-Character ===
        [Fact]
        public void CalculateDamage_EnemyVsChar_CriticalHitHits()
        {
            var attacker = new Enemy { STR = 10, Accuracy = 0, Attack = 5 };
            var defender = new Character { BaseDexterity = 10, ArmorClass = 100 };
            var result = _engine.CalculateDamage(attacker, defender, critRoll: 0.99);
            Assert.True(result.IsHit);
            Assert.True(result.IsCriticalHit);
            Assert.True(result.DamageDealt > 0);
        }

        [Fact]
        public void CalculateDamage_EnemyVsChar_CriticalMissMisses()
        {
            var attacker = new Enemy { STR = 30, Accuracy = 50, Attack = 50 };
            var defender = new Character { BaseDexterity = 10, ArmorClass = 0 };
            var result = _engine.CalculateDamage(attacker, defender, critRoll: 0.0);
            Assert.False(result.IsHit);
            Assert.True(result.IsCriticalMiss);
        }

        [Fact]
        public void CalculateDamage_EnemyVsChar_ShieldBlockReducesDamage()
        {
            var attacker = new Enemy { STR = 20, Accuracy = 0, Attack = 100 };
            var noBlock = new Character { BaseDexterity = 10, ArmorClass = 0, IsBlocking = false };
            var withShield = new Character { BaseDexterity = 10, ArmorClass = 0, IsBlocking = true, ShieldType = "Tower Shield" };
            var noBlockResult = _engine.CalculateDamage(attacker, noBlock, critRoll: 0.99);
            var shieldResult = _engine.CalculateDamage(attacker, withShield, critRoll: 0.99);
            Assert.True(shieldResult.DamageDealt < noBlockResult.DamageDealt);
        }

        // === Character-vs-Character ===
        [Fact]
        public void CalculateDamage_CharVsChar_MagicalUsesIntelligence()
        {
            var attacker = new Character { BaseIntelligence = 30, BaseStrength = 1 };
            var defender = new Character { BaseDexterity = 10, ArmorClass = 0 };
            var spell = new Skill { SkillType = "Magical", DamageDice = "1d4" };
            // d20=11 + GetModifier(30)=10 = 21 >= AC=10
            var result = _engine.CalculateDamage(attacker, defender, spell, critRoll: 0.5);
            Assert.True(result.IsHit);
        }

        [Fact]
        public void CalculateDamage_CharVsChar_CriticalHitHits()
        {
            var attacker = new Character { BaseStrength = 10 };
            var defender = new Character { BaseDexterity = 10, ArmorClass = 100 };
            var result = _engine.CalculateDamage(attacker, defender, critRoll: 0.99);
            Assert.True(result.IsHit);
            Assert.True(result.IsCriticalHit);
        }

        // === ApplySkillCosts ===
        [Fact]
        public void ApplySkillCosts_Character_DeductsMana()
        {
            var character = new Character { Mana = 50 };
            var skill = new Skill { ManaCost = 15 };
            _engine.ApplySkillCosts(character, skill);
            Assert.Equal(35, character.Mana);
        }

        [Fact]
        public void ApplySkillCosts_Character_ClampsToZero()
        {
            var character = new Character { Mana = 5 };
            var skill = new Skill { ManaCost = 20 };
            _engine.ApplySkillCosts(character, skill);
            Assert.Equal(0, character.Mana);
        }

        [Fact]
        public void ApplySkillCosts_Enemy_DeductsResources()
        {
            var enemy = new Enemy { Mana = 100, Stamina = 100 };
            var skill = new Skill { ManaCost = 25, StaminaCost = 10 };
            _engine.ApplySkillCosts(enemy, skill);
            Assert.Equal(75, enemy.Mana);
            Assert.Equal(90, enemy.Stamina);
        }

        [Fact]
        public void ApplySkillCosts_NullInputs_DoesNotThrow()
        {
            _engine.ApplySkillCosts((Character)null!, null!);
            // Should not throw
        }
    }
}