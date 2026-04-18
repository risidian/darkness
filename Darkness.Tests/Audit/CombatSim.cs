using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Darkness.Core.Logic;
using Darkness.Core.Models;
using Xunit;
using Xunit.Abstractions;

namespace Darkness.Tests.Audit
{
    public class CombatSim
    {
        private readonly ITestOutputHelper _output;
        private readonly CombatEngine _engine = new();
        private readonly Random _random = new();

        public CombatSim(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void RunCombatSimulation()
        {
            var report = new StringBuilder();
            report.AppendLine("# Combat Simulation Report");
            report.AppendLine("| Level | Hit Rate | Avg Damage | Min Damage | Max Damage | Crit Rate |");
            report.AppendLine("|-------|----------|------------|------------|------------|-----------|");

            for (int level = 1; level <= 20; level++)
            {
                var stats = SimulateLevel(level);
                report.AppendLine($"| {level} | {stats.HitRate:P2} | {stats.AvgDamage:F2} | {stats.MinDamage} | {stats.MaxDamage} | {stats.CritRate:P2} |");
            }

            var reportPath = "C:\\Users\\Mayce\\Documents\\GitHub\\darkness\\docs\\superpowers\\audit\\report-agent-1.md";
            Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
            File.WriteAllText(reportPath, report.ToString());
            _output.WriteLine(report.ToString());
        }

        private SimResult SimulateLevel(int level)
        {
            var character = CreateCharacter(level);
            var enemy = CreateEnemy(level);
            var skill = new Skill { Name = "Basic Attack", DamageDice = "1d6", DamageMultiplier = 1.0f };

            int hits = 0;
            int crits = 0;
            long totalDamage = 0;
            int minDamage = int.MaxValue;
            int maxDamage = 0;
            int iterations = 1000;

            for (int i = 0; i < iterations; i++)
            {
                var result = _engine.CalculateDamage(character, enemy, skill);
                if (result.IsHit)
                {
                    hits++;
                    totalDamage += result.DamageDealt;
                    minDamage = Math.Min(minDamage, result.DamageDealt);
                    maxDamage = Math.Max(maxDamage, result.DamageDealt);
                    if (result.IsCriticalHit) crits++;
                }
            }

            return new SimResult
            {
                HitRate = (double)hits / iterations,
                CritRate = (double)crits / iterations,
                AvgDamage = hits > 0 ? (double)totalDamage / hits : 0,
                MinDamage = hits > 0 ? minDamage : 0,
                MaxDamage = maxDamage
            };
        }

        private Character CreateCharacter(int level)
        {
            var c = new Character
            {
                Level = level,
                BaseStrength = 10 + (level - 1),
                BaseDexterity = 10 + (level - 1),
                BaseConstitution = 10 + (level - 1),
                BaseIntelligence = 10,
                BaseWisdom = 10,
                BaseCharisma = 10
            };
            // Award 2 attribute points per level (19 levels = 38 points)
            // Distribute points: 19 to Strength, 19 to Dexterity
            c.BaseStrength += (level - 1);
            c.BaseDexterity += (level - 1);
            
            c.RecalculateDerivedStats();
            return c;
        }

        private Enemy CreateEnemy(int level)
        {
            // Scaling based on quest data:
            // L1: Atk 7, Def 4
            // L3: Atk 15, Def 8
            // L12: Atk 25, Def 15
            // L15: Atk 50, Def 20
            
            // Linear approximation for intermediate levels:
            int attack = 7 + (int)((level - 1) * 3.0); 
            int defense = 4 + (int)((level - 1) * 1.2);

            return new Enemy
            {
                Level = level,
                STR = 10 + level,
                DEX = 10 + level,
                CON = 10 + level,
                Attack = attack,
                Defense = defense,
                MaxHP = 30 + (level * 50),
                CurrentHP = 30 + (level * 50)
            };
        }

        private class SimResult
        {
            public double HitRate { get; set; }
            public double CritRate { get; set; }
            public double AvgDamage { get; set; }
            public int MinDamage { get; set; }
            public int MaxDamage { get; set; }
        }
    }
}
