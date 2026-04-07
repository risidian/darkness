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
            var attacker = new Character { Strength = 10 };
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
            var attacker = new Character { Strength = 30 };
            var defender = new Enemy { Defense = 1 };

            var result = _engine.CalculateDamage(attacker, defender, critRoll: 0.0); // Maps to 1

            Assert.False(result.IsHit);
            Assert.True(result.IsCriticalMiss);
            Assert.Equal(0, result.DamageDealt);
        }

        [Fact]
        public void CalculateTurnOrder_ReturnsCorrectCount()
        {
            var c1 = new Character { Name = "C1", Dexterity = 20 };
            var e1 = new Enemy { Name = "E1", DEX = 10 };

            var order = _engine.CalculateTurnOrder(new List<Character> { c1 }, new List<Enemy> { e1 });

            Assert.Equal(2, order.Count);
        }
    }
}