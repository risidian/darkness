using Darkness.Core.Models;
using Darkness.Core.Services;
using Xunit;

namespace Darkness.Tests.Services;

public class EquipmentServiceTests
{
    private readonly EquipmentService _service = new();

    [Fact]
    public void Equip_AppliesStatBonuses()
    {
        var character = new Character { Strength = 10 };
        var sword = new Item { Name = "Iron Sword", Type = "Weapon", EquipmentSlot = "Weapon", AttackBonus = 5, StrengthBonus = 2 };

        var result = _service.Equip(character, sword);

        Assert.True(result);
        Assert.Equal(2, character.StatBonuses.GetValueOrDefault("Strength"));
    }

    [Fact]
    public void Equip_RejectsIfRequirementsNotMet()
    {
        var character = new Character { Strength = 5 };
        var sword = new Item { Name = "Heavy Sword", Type = "Weapon", EquipmentSlot = "Weapon", RequiredStrength = 15 };

        var result = _service.Equip(character, sword);

        Assert.False(result);
    }

    [Fact]
    public void Unequip_RemovesStatBonuses()
    {
        var character = new Character { Strength = 10 };
        var sword = new Item { Name = "Iron Sword", Type = "Weapon", EquipmentSlot = "Weapon", AttackBonus = 5, StrengthBonus = 2 };

        _service.Equip(character, sword);
        _service.Unequip(character, "Weapon");

        Assert.Equal(0, character.StatBonuses.GetValueOrDefault("Strength"));
    }

    [Fact]
    public void Equip_ReplacesExistingItemInSlot()
    {
        var character = new Character { Strength = 10 };
        var sword1 = new Item { Name = "Iron Sword", Type = "Weapon", EquipmentSlot = "Weapon", AttackBonus = 3 };
        var sword2 = new Item { Name = "Steel Sword", Type = "Weapon", EquipmentSlot = "Weapon", AttackBonus = 7 };
        character.Inventory.Add(sword1);

        _service.Equip(character, sword1);
        _service.Equip(character, sword2);

        Assert.Equal(7, character.StatBonuses.GetValueOrDefault("Attack"));
        Assert.Equal("Steel Sword", character.EquipmentSlots["Weapon"]);
    }
}
