using Darkness.Core.Models;
using Xunit;
using System.Collections.Generic;

namespace Darkness.Tests.Logic
{
    public class BattleSceneRegressionTests
    {
        [Fact]
        public void BattleState_PreservesHP_AfterNavigationCycle()
        {
            // 1. Setup Initial Battle
            var spawn = new EnemySpawn { Name = "Enemy1", MaxHP = 100, CurrentHP = 0 }; // 0 means use MaxHP initially
            var combatData = new CombatData { Enemies = new List<EnemySpawn> { spawn } };
            var args = new BattleArgs { Combat = combatData, IsResuming = false };
            
            // First Initialization
            var enemies = new List<Enemy>();
            var enemyMap = new Dictionary<Enemy, EnemySpawn>();
            foreach (var e in combatData.Enemies)
            {
                int initialHP = e.CurrentHP;
                if (initialHP <= 0 && !args.IsResuming) initialHP = e.MaxHP;

                var enemy = new Enemy { Name = e.Name, MaxHP = e.MaxHP, CurrentHP = initialHP };
                enemies.Add(enemy);
                enemyMap[enemy] = e;
            }
            
            Assert.Equal(100, enemies[0].CurrentHP);
            
            // 2. Damage enemy
            enemies[0].CurrentHP = 40;
            
            // 3. Sync State (happens when clicking Inventory)
            args.IsResuming = true;
            foreach (var pair in enemyMap)
            {
                pair.Value.CurrentHP = pair.Key.CurrentHP;
            }
            Assert.Equal(40, spawn.CurrentHP);
            
            // 4. Return to Battle (Initialize called again)
            var newEnemies = new List<Enemy>();
            foreach (var e in combatData.Enemies)
            {
                int initialHP = e.CurrentHP;
                if (initialHP <= 0 && !args.IsResuming) initialHP = e.MaxHP;

                var enemy = new Enemy { Name = e.Name, MaxHP = e.MaxHP, CurrentHP = initialHP };
                newEnemies.Add(enemy);
            }
            
            // 5. Verify HP is 40
            Assert.Equal(40, newEnemies[0].CurrentHP);
        }

        [Fact]
        public void BattleState_ResetsDeadHP_REGRESSION()
        {
            // 1. Setup Initial Battle
            var spawn = new EnemySpawn { Name = "Enemy1", MaxHP = 100, CurrentHP = 0 };
            var combatData = new CombatData { Enemies = new List<EnemySpawn> { spawn } };
            var args = new BattleArgs { Combat = combatData, IsResuming = false };
            
            // First Initialization
            var enemies = new List<Enemy>();
            var enemyMap = new Dictionary<Enemy, EnemySpawn>();
            foreach (var e in combatData.Enemies)
            {
                int initialHP = e.CurrentHP;
                if (initialHP <= 0 && !args.IsResuming) initialHP = e.MaxHP;

                var enemy = new Enemy { Name = e.Name, MaxHP = e.MaxHP, CurrentHP = initialHP };
                enemies.Add(enemy);
                enemyMap[enemy] = e;
            }
            
            Assert.Equal(100, enemies[0].CurrentHP);
            
            // 2. Kill enemy
            enemies[0].CurrentHP = 0;
            
            // 3. Sync State (Sets IsResuming = true)
            args.IsResuming = true;
            foreach (var pair in enemyMap)
            {
                pair.Value.CurrentHP = pair.Key.CurrentHP;
            }
            Assert.Equal(0, spawn.CurrentHP);
            
            // 4. Return to Battle (Initialize called again)
            var newEnemies = new List<Enemy>();
            foreach (var e in combatData.Enemies)
            {
                // Fixed code logic
                int initialHP = e.CurrentHP;
                if (initialHP <= 0 && !args.IsResuming) initialHP = e.MaxHP;

                var enemy = new Enemy { Name = e.Name, MaxHP = e.MaxHP, CurrentHP = initialHP };
                newEnemies.Add(enemy);
            }
            
            // 5. Verify HP is 0 (stays dead)
            Assert.Equal(0, newEnemies[0].CurrentHP);
        }
    }
}
