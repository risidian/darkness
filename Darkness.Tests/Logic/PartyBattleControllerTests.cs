using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Logic;
using Darkness.Core.Models;
using LiteDB;
using Xunit;

namespace Darkness.Tests.Logic;

public class PartyBattleControllerTests
{
    private static Character Pc(string name = "Hero", int dex = 15, int hp = 30) =>
        new Character { Name = name, BaseDexterity = dex, MaxHP = hp, CurrentHP = hp };

    private static Enemy Foe(string name, int dex = 10, int hp = 20, bool invincible = false) =>
        new Enemy { Name = name, DEX = dex, MaxHP = hp, CurrentHP = hp, IsInvincible = invincible };

    // --- Turn order ---

    [Fact]
    public void TurnOrder_TwoMemberParty_VsThreeEnemies_HasFiveParticipants()
    {
        var party = new List<Character> { Pc("Hero"), Pc("Darius") };
        var enemies = new List<Enemy> { Foe("A"), Foe("B"), Foe("C") };

        var sut = new PartyBattleController(party, enemies);

        Assert.Equal(5, sut.TurnOrder.Count);
        foreach (var c in party) Assert.Contains(c, sut.TurnOrder);
        foreach (var e in enemies) Assert.Contains(e, sut.TurnOrder);
    }

    [Fact]
    public void TurnOrder_ThreeMemberParty_VsTwoEnemies_HasFiveParticipants()
    {
        var party = new List<Character> { Pc("Hero"), Pc("Darius"), Pc("Tywin") };
        var enemies = new List<Enemy> { Foe("A"), Foe("B") };

        var sut = new PartyBattleController(party, enemies);

        Assert.Equal(5, sut.TurnOrder.Count);
        foreach (var c in party) Assert.Contains(c, sut.TurnOrder);
        foreach (var e in enemies) Assert.Contains(e, sut.TurnOrder);
    }

    // --- Role flagging ---

    [Fact]
    public void Roles_FirstMemberIsPlayerControlled_RestAreAiAllies_ByDefault()
    {
        var pc = Pc("Hero");
        var darius = Pc("Darius");
        var tywin = Pc("Tywin");
        var sut = new PartyBattleController(
            new List<Character> { pc, darius, tywin },
            new List<Enemy> { Foe("A") });

        Assert.True(sut.IsPlayerControlled(pc));
        Assert.False(sut.IsAiAlly(pc));
        Assert.True(sut.IsAiAlly(darius));
        Assert.True(sut.IsAiAlly(tywin));

        Assert.Equal(ParticipantRole.PlayerControlled, sut.RoleOf(pc));
        Assert.Equal(ParticipantRole.AiAlly, sut.RoleOf(darius));
        Assert.Equal(ParticipantRole.Enemy, sut.RoleOf(sut.Enemies[0]));
    }

    [Fact]
    public void Roles_ExplicitAiAlliesSet_OverridesDefault()
    {
        var pc = Pc("Hero");
        var ally = Pc("Darius");
        var sut = new PartyBattleController(
            new List<Character> { pc, ally },
            new List<Enemy> { Foe("A") },
            aiAllies: new List<Character> { pc }); // flag the PC as AI instead

        Assert.True(sut.IsAiAlly(pc));
        Assert.False(sut.IsAiAlly(ally));
        Assert.True(sut.IsPlayerControlled(ally));
    }

    [Fact]
    public void CurrentRole_ReflectsWhoseTurnItIsForEveryParticipant()
    {
        var party = new List<Character> { Pc("Hero"), Pc("Darius") };
        var enemies = new List<Enemy> { Foe("A"), Foe("B") };
        var sut = new PartyBattleController(party, enemies);

        // Walk the whole rotation; each current role must match RoleOf(current).
        for (int i = 0; i < sut.TurnOrder.Count; i++)
        {
            Assert.NotNull(sut.CurrentParticipant);
            Assert.Equal(sut.RoleOf(sut.CurrentParticipant!), sut.CurrentRole);
            sut.AdvanceTurn();
        }
    }

    // --- Enemy target selection ---

    [Fact]
    public void ChooseEnemyTarget_ReturnsLivingPartyMember()
    {
        var pc = Pc("Hero");
        var darius = Pc("Darius");
        var sut = new PartyBattleController(
            new List<Character> { pc, darius },
            new List<Enemy> { Foe("A") });

        var target = sut.ChooseEnemyTarget();

        Assert.NotNull(target);
        Assert.True(target!.CurrentHP > 0);
        Assert.Same(pc, target); // first-living default
    }

    [Fact]
    public void ChooseEnemyTarget_ReTargets_WhenFirstMemberDies()
    {
        var pc = Pc("Hero");
        var darius = Pc("Darius");
        var sut = new PartyBattleController(
            new List<Character> { pc, darius },
            new List<Enemy> { Foe("A") });

        pc.CurrentHP = 0; // PC falls

        var target = sut.ChooseEnemyTarget();

        Assert.Same(darius, target);
    }

    [Fact]
    public void ChooseEnemyTarget_ReturnsNull_WhenNoLivingPartyMember()
    {
        var pc = Pc("Hero");
        pc.CurrentHP = 0;
        var sut = new PartyBattleController(
            new List<Character> { pc },
            new List<Enemy> { Foe("A") });

        Assert.Null(sut.ChooseEnemyTarget());
    }

    // --- AI ally target selection ---

    [Fact]
    public void ChooseAllyAiTarget_ReturnsLivingEnemy()
    {
        var sut = new PartyBattleController(
            new List<Character> { Pc("Hero"), Pc("Darius") },
            new List<Enemy> { Foe("A"), Foe("B") });

        var target = sut.ChooseAllyAiTarget();

        Assert.NotNull(target);
        Assert.True(target!.CurrentHP > 0);
    }

    [Fact]
    public void ChooseAllyAiTarget_SkipsDeadEnemy()
    {
        var dead = Foe("A");
        var alive = Foe("B");
        dead.CurrentHP = 0;
        var sut = new PartyBattleController(
            new List<Character> { Pc("Hero") },
            new List<Enemy> { dead, alive });

        Assert.Same(alive, sut.ChooseAllyAiTarget());
    }

    // --- Removal of dead participants ---

    [Fact]
    public void RemoveDefeated_RemovesDeadPartyMemberFromRotation()
    {
        var pc = Pc("Hero");
        var darius = Pc("Darius");
        var enemy = Foe("A");
        var sut = new PartyBattleController(
            new List<Character> { pc, darius },
            new List<Enemy> { enemy });

        int before = sut.TurnOrder.Count;
        darius.CurrentHP = 0;
        sut.RemoveDefeated();

        Assert.Equal(before - 1, sut.TurnOrder.Count);
        Assert.DoesNotContain(darius, sut.TurnOrder);
        Assert.Contains(pc, sut.TurnOrder);
        Assert.Contains(enemy, sut.TurnOrder);
    }

    [Fact]
    public void RemoveDefeated_RemovesDeadEnemyFromRotation()
    {
        var enemy = Foe("A");
        var sut = new PartyBattleController(
            new List<Character> { Pc("Hero") },
            new List<Enemy> { enemy, Foe("B") });

        enemy.CurrentHP = 0;
        sut.RemoveDefeated();

        Assert.DoesNotContain(enemy, sut.TurnOrder);
    }

    // --- Victory / defeat ---

    [Fact]
    public void IsVictory_True_WhenAllEnemiesDead()
    {
        var enemies = new List<Enemy> { Foe("A"), Foe("B") };
        var sut = new PartyBattleController(new List<Character> { Pc("Hero") }, enemies);

        Assert.False(sut.IsVictory());
        foreach (var e in enemies) e.CurrentHP = 0;
        Assert.True(sut.IsVictory());
        Assert.False(sut.IsDefeat());
    }

    [Fact]
    public void IsDefeat_True_WhenAllPartyDead()
    {
        var party = new List<Character> { Pc("Hero"), Pc("Darius") };
        var sut = new PartyBattleController(party, new List<Enemy> { Foe("A") });

        Assert.False(sut.IsDefeat());
        foreach (var c in party) c.CurrentHP = 0;
        Assert.True(sut.IsDefeat());
        Assert.False(sut.IsVictory());
    }

    // --- Survival beat vs invincible boss ---

    [Fact]
    public void SurvivalBeat_Victory_AfterSurvivingRequiredRounds_VsInvincibleBoss()
    {
        var party = new List<Character> { Pc("Hero"), Pc("Darius") };
        var boss = Foe("Dariartimous", invincible: true, hp: 999);
        var sut = new PartyBattleController(party, new List<Enemy> { boss }, survivalTurns: 5);

        Assert.True(sut.HasInvincibleBoss);
        Assert.False(sut.IsVictory()); // round 1

        // Advance a full round: with (party 2 + boss 1) = 3 participants, 3 AdvanceTurns => next round.
        int perRound = sut.TurnOrder.Count;
        for (int round = 1; round <= 5; round++)
        {
            Assert.False(sut.SurvivalGoalReached); // not yet reached during rounds 1..5
            for (int t = 0; t < perRound; t++) sut.AdvanceTurn();
        }

        // Now CurrentRound == 6 > 5 => survived.
        Assert.Equal(6, sut.CurrentRound);
        Assert.True(sut.SurvivalGoalReached);
        Assert.True(sut.IsVictory());
        Assert.False(sut.AllEnemiesDefeated); // boss never died
    }

    [Fact]
    public void SurvivalGoal_NotReached_WhenNoSurvivalTurnsConfigured()
    {
        var sut = new PartyBattleController(
            new List<Character> { Pc("Hero") },
            new List<Enemy> { Foe("A") });

        for (int i = 0; i < 20; i++) sut.AdvanceTurn();
        Assert.False(sut.SurvivalGoalReached);
    }

    // --- CombatData.Allies round-trip ---

    [Fact]
    public void CombatData_Allies_RoundTripsThroughLiteDbBsonMapper()
    {
        var mapper = new BsonMapper();
        var combat = new CombatData
        {
            BackgroundKey = "cliff",
            SurvivalTurns = 5,
            Enemies = { new EnemySpawn { Name = "Tauros", MaxHP = 120, Attack = 15 } },
            Allies =
            {
                new EnemySpawn { Name = "Darius", SpriteKey = "darius", MaxHP = 40, Attack = 8, Defense = 4, Speed = 6 },
                new EnemySpawn { Name = "Tywin", SpriteKey = "tywin", MaxHP = 55, Attack = 12 },
            },
        };

        var doc = mapper.ToDocument(combat);
        var restored = mapper.ToObject<CombatData>(doc);

        Assert.Equal(2, restored.Allies.Count);
        Assert.Equal("Darius", restored.Allies[0].Name);
        Assert.Equal(40, restored.Allies[0].MaxHP);
        Assert.Equal("tywin", restored.Allies[1].SpriteKey);
        // Existing fields still round-trip unaffected.
        Assert.Single(restored.Enemies);
        Assert.Equal("Tauros", restored.Enemies[0].Name);
        Assert.Equal("cliff", restored.BackgroundKey);
        Assert.Equal(5, restored.SurvivalTurns);
    }

    [Fact]
    public void CombatData_Allies_DefaultsEmpty_AndExistingJsonRoundTripsUnchanged()
    {
        // Simulates existing combat JSON (no "Allies" key): Allies must be empty, not null.
        const string json = "{\"Enemies\":[{\"Name\":\"Goblin\",\"MaxHP\":15}],\"BackgroundKey\":\"forest\"}";
        var restored = System.Text.Json.JsonSerializer.Deserialize<CombatData>(json);

        Assert.NotNull(restored);
        Assert.NotNull(restored!.Allies);
        Assert.Empty(restored.Allies);
        Assert.Single(restored.Enemies);
        Assert.Equal("Goblin", restored.Enemies[0].Name);
        Assert.Equal("forest", restored.BackgroundKey);
    }
}
