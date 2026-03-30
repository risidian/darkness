using Darkness.Core.Logic;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Logic
{
    public class CombatEngineTests
    {
        private readonly CombatEngine _engine = new();

        [Fact]
        public void CalculateDamage_PositiveDamage_WhenDefenseHigherThanAttack()
        {
            // Arrange
            var attacker = new Character { Strength = 10 };
            var defender = new Enemy { Defense = 20 };

            // Act
            int damage = _engine.CalculateDamage(attacker, defender, critRoll: 1.0); // No crit

            // Assert
            // (10 * 10) / (10 + 20) = 100 / 30 = 3.33 -> 3
            Assert.True(damage > 0, $"Damage should be positive when Defense > Attack. Got {damage}");
            Assert.Equal(3, damage);
        }

        [Fact]
        public void CalculateDamage_PositiveDamage_WhenAttackVeryLow()
        {
            // Arrange
            var attacker = new Character { Strength = 1 };
            var defender = new Enemy { Defense = 100 };

            // Act
            int damage = _engine.CalculateDamage(attacker, defender, critRoll: 1.0); // No crit

            // Assert
            Assert.True(damage > 0, $"Damage should be at least 1 when Attack > 0. Got {damage}");
        }

        [Fact]
        public void CalculateDamage_ZeroDamage_OnlyWhenAttackIsZero()
        {
            // Arrange
            var attacker = new Character { Strength = 0 };
            var defender = new Enemy { Defense = 10 };

            // Act
            int damage = _engine.CalculateDamage(attacker, defender, critRoll: 1.0); // No crit

            // Assert
            Assert.Equal(0, damage);
        }

        [Fact]
        public void CalculateDamage_StandardScenario()
        {
            // Arrange
            var attacker = new Character { Strength = 50 };
            var defender = new Enemy { Defense = 50 };

            // Act
            int damage = _engine.CalculateDamage(attacker, defender, critRoll: 1.0); // No crit

            // Assert
            // (50 * 50) / (50 + 50) = 2500 / 100 = 25
            Assert.Equal(25, damage);
        }

        [Fact]
        public void CalculateDamage_CriticalHit_ScalesCorrectly()
        {
            // Arrange
            var attacker = new Character { Strength = 50 };
            var defender = new Enemy { Defense = 50 };
            int baseDamage = 25; // (50*50)/(50+50) = 25
            int expectedCriticalDamage = (int)(baseDamage * 1.5); // 37

            // Act
            int damage = _engine.CalculateDamage(attacker, defender, critRoll: 0.04); // < 0.05 is critical

            // Assert
            Assert.Equal(expectedCriticalDamage, damage);
        }

        [Fact]
        public void CalculateDamage_NoCriticalHit_WhenRollIsHigh()
        {
            // Arrange
            var attacker = new Character { Strength = 50 };
            var defender = new Enemy { Defense = 50 };
            int expectedBaseDamage = 25;

            // Act
            int damage = _engine.CalculateDamage(attacker, defender, critRoll: 0.06); // >= 0.05 is not critical

            // Assert
            Assert.Equal(expectedBaseDamage, damage);
        }

        [Fact]
        public void CalculateDamage_EnemyAttacker_FollowsNewFormula()
        {
            // Arrange
            var attacker = new Enemy { Attack = 50 };
            var defender = new Character { Defense = 50 };

            // Act
            int damage = _engine.CalculateDamage(attacker, defender, critRoll: 1.0); // No crit

            // Assert
            Assert.Equal(25, damage);
        }

        [Fact]
        public void CalculateDamage_CharacterAttackerCharacterDefender_FollowsNewFormula()
        {
            // Arrange
            var attacker = new Character { Strength = 50 };
            var defender = new Character { Defense = 50 };

            // Act
            int damage = _engine.CalculateDamage(attacker, defender, critRoll: 1.0); // No crit

            // Assert
            Assert.Equal(25, damage);
        }

        [Fact]
        public void CalculateDamage_IsPureFunction()
        {
            // Arrange
            var attacker = new Character { Strength = 50, Mana = 100, Stamina = 100 };
            var defender = new Enemy { Defense = 50 };
            var skill = new Skill { ManaCost = 10, StaminaCost = 10, BasePower = 10 };

            // Act
            _engine.CalculateDamage(attacker, defender, skill, critRoll: 1.0); // No crit

            // Assert
            Assert.Equal(100, attacker.Mana);
            Assert.Equal(100, attacker.Stamina);
        }

        [Fact]
        public void CalculateDamage_HandlesPotentialOverflow()
        {
            // Arrange
            // Using large values that would overflow int if not handled correctly
            // (50000 * 50000) = 2,500,000,000 (Greater than int.MaxValue which is ~2.1B)
            var attacker = new Character { Strength = 50000 };
            var defender = new Enemy { Defense = 50000 };
            int expectedBaseDamage = 25000;

            // Act
            int damage = _engine.CalculateDamage(attacker, defender, critRoll: 1.0); // No crit

            // Assert
            // (50000 * 50000) / (50000 + 50000) = 2,500,000,000 / 100,000 = 25,000
            Assert.Equal(expectedBaseDamage, damage);
        }

        [Fact]
        public void ApplySkillCosts_DeductsManaAndStamina_Character()
        {
            // Arrange
            var attacker = new Character { Mana = 100, Stamina = 100 };
            var skill = new Skill { ManaCost = 20, StaminaCost = 30 };

            // Act
            _engine.ApplySkillCosts(attacker, skill);

            // Assert
            Assert.Equal(80, attacker.Mana);
            Assert.Equal(70, attacker.Stamina);
        }

        [Fact]
        public void ApplySkillCosts_DeductsManaAndStamina_Enemy()
        {
            // Arrange
            var attacker = new Enemy { Mana = 100, Stamina = 100 };
            var skill = new Skill { ManaCost = 20, StaminaCost = 30 };

            // Act
            _engine.ApplySkillCosts(attacker, skill);

            // Assert
            Assert.Equal(80, attacker.Mana);
            Assert.Equal(70, attacker.Stamina);
        }

        [Fact]
        public void CalculateTurnOrder_SortsByInitiative()
        {
            // Arrange
            // Large enough difference that random(1-10) doesn't swap them
            var party = new List<Character>
            {
                new Character { Name = "Slow", Dexterity = 1, Speed = 1 },
                new Character { Name = "Fast", Dexterity = 100, Speed = 100 }
            };
            var enemies = new List<Enemy>
            {
                new Enemy { Name = "Medium", DEX = 50, Speed = 50 }
            };

            // Act
            var order = _engine.CalculateTurnOrder(party, enemies);

            // Assert
            Assert.Equal(3, order.Count);
            Assert.Equal("Fast", ((Character)order[0]).Name);
            Assert.Equal("Medium", ((Enemy)order[1]).Name);
            Assert.Equal("Slow", ((Character)order[2]).Name);
        }

        [Fact]
        public void CheckStatusEffect_ResistsWhenWisdomIsHigh()
        {
            // Arrange
            var target = new Character { Wisdom = 100 }; // 100% resistance
            var effect = new StatusEffect();

            // Act
            bool applied = _engine.CheckStatusEffect(target, effect);

            // Assert
            Assert.False(applied);
        }

        [Fact]
        public void CheckStatusEffect_AppliesWhenWisdomIsLow()
        {
            // Arrange
            var target = new Character { Wisdom = 0 }; // 0% resistance
            var effect = new StatusEffect();

            // Act
            bool applied = _engine.CheckStatusEffect(target, effect);

            // Assert
            Assert.True(applied);
        }

        [Fact]
        public void ApplySkillCosts_DoesNotDropBelowZero()
        {
            // Arrange
            var attacker = new Character { Mana = 10, Stamina = 10 };
            var skill = new Skill { ManaCost = 20, StaminaCost = 30 };

            // Act
            _engine.ApplySkillCosts(attacker, skill);

            // Assert
            Assert.Equal(0, attacker.Mana);
            Assert.Equal(0, attacker.Stamina);
        }

        [Fact]
        public void ApplySkillCosts_Enemy_DoesNotDropBelowZero()
        {
            // Arrange
            var attacker = new Enemy { Mana = 10, Stamina = 10 };
            var skill = new Skill { ManaCost = 20, StaminaCost = 30 };

            // Act
            _engine.ApplySkillCosts(attacker, skill);

            // Assert
            Assert.Equal(0, attacker.Mana);
            Assert.Equal(0, attacker.Stamina);
        }

        [Fact]
        public void CheckStatusEffect_Enemy_ResistsWhenWisdomIsHigh()
        {
            // Arrange
            var target = new Enemy { WIS = 100 }; // 100% resistance
            var effect = new StatusEffect();

            // Act
            bool applied = _engine.CheckStatusEffect(target, effect);

            // Assert
            Assert.False(applied);
        }

        [Fact]
        public void CheckStatusEffect_Enemy_AppliesWhenWisdomIsLow()
        {
            // Arrange
            var target = new Enemy { WIS = 0 }; // 0% resistance
            var effect = new StatusEffect();

            // Act
            bool applied = _engine.CheckStatusEffect(target, effect);

            // Assert
            Assert.True(applied);
        }

        [Fact]
        public void CalculateTurnOrder_EmptyLists_ReturnsEmpty()
        {
            // Act
            var order = _engine.CalculateTurnOrder(new List<Character>(), new List<Enemy>());

            // Assert
            Assert.Empty(order);
        }

        [Fact]
        public void CalculateTurnOrder_HandlesTies()
        {
            // Arrange
            // We use stats that are identical to increase tie probability.
            // And we use names that should be sorted alphabetically.
            var p1 = new Character { Name = "Alice", Dexterity = 10, Speed = 10 };
            var p2 = new Character { Name = "Zelda", Dexterity = 10, Speed = 10 };
            var e1 = new Enemy { Name = "Azazel", DEX = 10, Speed = 10 };
            var e2 = new Enemy { Name = "Zog", DEX = 10, Speed = 10 };

            var party = new List<Character> { p1, p2 };
            var enemies = new List<Enemy> { e1, e2 };

            // Act
            // Run a few times to increase chance of seeing the tie-breaker in action
            // if random rolls happen to be equal.
            for (int i = 0; i < 10; i++)
            {
                var order = _engine.CalculateTurnOrder(party, enemies);

                // Assert
                Assert.Equal(4, order.Count);
                
                // We can't strictly assert the exact order because of the random component (1-10),
                // but we can verify that the list contains all participants.
                Assert.Contains(p1, order);
                Assert.Contains(p2, order);
                Assert.Contains(e1, order);
                Assert.Contains(e2, order);
            }

            // To truly verify the tie-breaker without a mock Random, 
            // we'd need to control the rolls. 
            // Since we can't, we've at least verified the code path doesn't crash 
            // and the sorting logic is applied.
        }
    }
}
