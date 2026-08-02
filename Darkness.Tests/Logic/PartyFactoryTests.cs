using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Logic;
using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Logic;

public class PartyFactoryTests
{
    private static Character Pc(string name = "Hero", int hp = 30) =>
        new Character { Name = name, MaxHP = hp, CurrentHP = hp };

    private static EnemySpawn Ally(string name, string sprite, int hp = 40, int atk = 8, int def = 4, int spd = 6) =>
        new EnemySpawn { Name = name, SpriteKey = sprite, MaxHP = hp, Attack = atk, Defense = def, Speed = spd };

    // --- Behaviour-neutral: no allies ---

    [Fact]
    public void BuildParty_NoAllies_ReturnsSinglePcParty()
    {
        var pc = Pc();
        var combat = new CombatData { Enemies = { new EnemySpawn { Name = "Goblin", MaxHP = 15 } } };

        var result = PartyFactory.BuildParty(pc, combat);

        Assert.Single(result.Party);
        Assert.Same(pc, result.Party[0]);
        Assert.Empty(result.AllySpriteKeys);
    }

    [Fact]
    public void BuildParty_NullCombat_ReturnsSinglePcParty()
    {
        var pc = Pc();

        var result = PartyFactory.BuildParty(pc, null);

        Assert.Single(result.Party);
        Assert.Same(pc, result.Party[0]);
        Assert.Empty(result.AllySpriteKeys);
    }

    [Fact]
    public void BuildParty_DefaultsPcHp_WhenUnset()
    {
        var pc = new Character { Name = "Hero" }; // MaxHP/CurrentHP == 0

        var result = PartyFactory.BuildParty(pc, new CombatData());

        Assert.Equal(100, result.Party[0].MaxHP);
        Assert.Equal(100, result.Party[0].CurrentHP);
        Assert.False(result.Party[0].IsBlocking);
    }

    // --- Allies present ---

    [Fact]
    public void BuildParty_WithAllies_IncludesAlliesInOrderAfterPc()
    {
        var pc = Pc();
        var combat = new CombatData
        {
            Allies =
            {
                Ally("Darius", "bosses/Darius"),
                Ally("Tywin", "bosses/Tywin"),
            },
        };

        var result = PartyFactory.BuildParty(pc, combat);

        Assert.Equal(3, result.Party.Count);
        Assert.Same(pc, result.Party[0]);
        Assert.Equal("Darius", result.Party[1].Name);
        Assert.Equal("Tywin", result.Party[2].Name);
    }

    [Fact]
    public void BuildParty_MapsAllyStatsAndSpriteKey()
    {
        var pc = Pc();
        var combat = new CombatData
        {
            Allies = { Ally("Darius", "bosses/Darius", hp: 55, atk: 12, def: 5, spd: 7) },
        };

        var result = PartyFactory.BuildParty(pc, combat);
        var darius = result.Party[1];

        Assert.Equal("Darius", darius.Name);
        Assert.Equal(55, darius.MaxHP);
        Assert.Equal(55, darius.CurrentHP);
        Assert.Equal(12, darius.Attack);
        Assert.Equal(5, darius.Defense);
        Assert.Equal(5, darius.ArmorClass);
        Assert.Equal(7, darius.Speed);
        Assert.Equal(12, darius.BaseStrength);
        Assert.Equal(7, darius.BaseDexterity);
        // Sprite key preserved for rendering (Character carries no sprite key of its own).
        Assert.True(result.AllySpriteKeys.ContainsKey(darius));
        Assert.Equal("bosses/Darius", result.AllySpriteKeys[darius]);
    }

    [Fact]
    public void CreateAlly_UsesMaxHp_WhenCurrentHpUnset()
    {
        var ally = PartyFactory.CreateAlly(new EnemySpawn { Name = "Darius", MaxHP = 40, CurrentHP = 0 });

        Assert.Equal(40, ally.MaxHP);
        Assert.Equal(40, ally.CurrentHP);
    }

    [Fact]
    public void CreateAlly_PreservesPersistedCurrentHp()
    {
        var ally = PartyFactory.CreateAlly(new EnemySpawn { Name = "Darius", MaxHP = 40, CurrentHP = 18 });

        Assert.Equal(40, ally.MaxHP);
        Assert.Equal(18, ally.CurrentHP);
    }

    // --- Enemy mapping ---

    [Fact]
    public void CreateEnemy_UsesMaxHp_AtBattleStart()
    {
        var enemy = PartyFactory.CreateEnemy(new EnemySpawn { Name = "Orc", MaxHP = 60, CurrentHP = 0 }, isResuming: false);

        Assert.Equal(60, enemy.CurrentHP);
    }

    [Fact]
    public void CreateEnemy_UsesCurrentHp_WhenResuming()
    {
        var enemy = PartyFactory.CreateEnemy(new EnemySpawn { Name = "Orc", MaxHP = 60, CurrentHP = 12 }, isResuming: true);

        Assert.Equal(12, enemy.CurrentHP);
    }

    [Fact]
    public void CreateEnemy_AppliesStatFallbacks()
    {
        var enemy = PartyFactory.CreateEnemy(new EnemySpawn { Name = "Orc", MaxHP = 60 }, isResuming: false);

        Assert.Equal(10, enemy.Attack);   // default when 0
        Assert.Equal(5, enemy.Defense);   // default when 0
        Assert.Equal(10, enemy.Speed);    // default when 0
        Assert.Equal(80, enemy.Accuracy); // default when 0
        Assert.Equal("hound", enemy.SpriteKey); // EnemySpawn.SpriteKey defaults to "hound"
    }

    // --- Integration with the humble-object controller ---

    [Fact]
    public void BuiltParty_FeedsController_PcPlayerControlled_AlliesAreAi()
    {
        var pc = Pc();
        var combat = new CombatData
        {
            Enemies = { new EnemySpawn { Name = "Tauros", MaxHP = 120 } },
            Allies = { Ally("Darius", "bosses/Darius"), Ally("Tywin", "bosses/Tywin") },
        };

        var built = PartyFactory.BuildParty(pc, combat);
        var enemies = PartyFactory.BuildEnemies(combat, isResuming: false);
        var controller = new PartyBattleController(built.Party, enemies);

        Assert.True(controller.IsPlayerControlled(built.Party[0]));
        Assert.True(controller.IsAiAlly(built.Party[1]));
        Assert.True(controller.IsAiAlly(built.Party[2]));
        Assert.Equal(4, controller.TurnOrder.Count); // 3 party + 1 enemy
    }
}
