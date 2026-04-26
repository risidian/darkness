using System.Collections.Generic;
using Darkness.Core.Logic;
using Darkness.Core.Models;
using Moq;
using Xunit;

namespace Darkness.Tests.Logic;

public class TurnManagerTests
{
    private TurnManager _sut;
    private Mock<System.Random> _randomMock;

    public TurnManagerTests()
    {
        _sut = new TurnManager();
    }

    [Fact]
    public void Setup_PopulatesTurnOrderAndSetsCurrentTurnIndexToZero()
    {
        // Arrange
        var party = new List<Character> { new Character { Name = "Hero", BaseDexterity = 15 } };
        var enemies = new List<Enemy> { new Enemy { Name = "Goblin", DEX = 10 } };
        
        // Act
        _sut.Setup(party, enemies, new CombatEngine());

        // Assert
        Assert.Equal(2, _sut.TurnOrder.Count);
        Assert.Equal(0, _sut.CurrentTurnIndex);
        Assert.NotNull(_sut.CurrentEntity);
    }

    [Fact]
    public void NextTurn_AdvancesIndexAndLoops()
    {
        // Arrange
        var party = new List<Character> { new Character { Name = "Hero", BaseDexterity = 15 } };
        var enemies = new List<Enemy> { new Enemy { Name = "Goblin", DEX = 10 } };
        _sut.Setup(party, enemies, new CombatEngine());
        
        var first = _sut.CurrentEntity;

        // Act & Assert
        _sut.NextTurn();
        Assert.Equal(1, _sut.CurrentTurnIndex);
        var second = _sut.CurrentEntity;
        Assert.NotEqual(first, second);

        _sut.NextTurn();
        Assert.Equal(0, _sut.CurrentTurnIndex); // Loop back
        Assert.Equal(first, _sut.CurrentEntity);
    }

    [Fact]
    public void NextTurn_IncrementsSurvivalTurn_WhenLooping()
    {
        // Arrange
        var party = new List<Character> { new Character { Name = "Hero", BaseDexterity = 15 } };
        var enemies = new List<Enemy> { new Enemy { Name = "Goblin", DEX = 10 } };
        _sut.Setup(party, enemies, new CombatEngine());
        
        // Act
        _sut.NextTurn(); // Index 1
        _sut.NextTurn(); // Index 0, round 2

        // Assert
        Assert.Equal(2, _sut.CurrentRound);
    }

    [Fact]
    public void RemoveEntity_RemovesFromOrderAndAdjustsIndex()
    {
        // Arrange
        var hero = new Character { Name = "Hero", BaseDexterity = 15 };
        var goblin = new Enemy { Name = "Goblin", DEX = 10 };
        var orc = new Enemy { Name = "Orc", DEX = 12 };
        _sut.Setup(new List<Character> { hero }, new List<Enemy> { goblin, orc }, new CombatEngine());
        
        // Force specific order: hero (0), orc (1), goblin (2)
        _sut.TurnOrder.Clear();
        _sut.TurnOrder.Add(hero);
        _sut.TurnOrder.Add(orc);
        _sut.TurnOrder.Add(goblin);

        _sut.CurrentTurnIndex = 1; // Current is Orc
        
        // Act: remove hero (index 0, before current)
        _sut.RemoveEntity(hero);

        // Assert
        Assert.Equal(2, _sut.TurnOrder.Count);
        Assert.Equal(0, _sut.CurrentTurnIndex); // Shifted down
        Assert.Equal(orc, _sut.CurrentEntity); // Still Orc's turn
    }

    [Fact]
    public void Retry_ResetsEverything()
    {
        // Arrange
        var party = new List<Character> { new Character { Name = "Hero", BaseDexterity = 15 } };
        var enemies = new List<Enemy> { new Enemy { Name = "Goblin", DEX = 10 } };
        _sut.Setup(party, enemies, new CombatEngine());
        _sut.NextTurn();
        _sut.NextTurn(); // Round 2

        // Act
        _sut.Setup(party, enemies, new CombatEngine()); // Retry is effectively calling Setup again

        // Assert
        Assert.Equal(0, _sut.CurrentTurnIndex);
        Assert.Equal(1, _sut.CurrentRound);
    }
}
