using Darkness.Core.Models;
using Xunit;

namespace Darkness.Tests.Models;

public class ItemCloneTests
{
    [Fact]
    public void Clone_CopiesAllPersistentFields()
    {
        // Locks in the contract that Clone() reproduces every persistent Item field.
        // If you add a new field to Item, set it here and assert it round-trips.
        var original = new Item
        {
            Id = 99, // intentionally non-zero — see assertion below
            Name = "Mythril Greatsword",
            Description = "A legendary blade.",
            Type = "Weapon",
            Weight = 12,
            Value = 500,
            StrengthBonus = 5,
            DexterityBonus = 1,
            IntelligenceBonus = 2,
            DefenseBonus = 3,
            AttackBonus = 7,
            ArmorClass = 4,
            DamageDice = "2d8",
            EquipmentSlot = "Weapon",
            EquipmentSpriteId = 17,
            RequiredStrength = 14,
            RequiredDexterity = 12,
            RequiredIntelligence = 10,
            RequiredLevel = 8,
            Tier = 2,
            Infusion = "Fire",
            Quantity = 5
        };

        var clone = original.Clone();

        Assert.NotSame(original, clone);
        // Clone() intentionally drops Id — inventory items are detached copies, not LiteDB rows.
        Assert.Equal(0, clone.Id);

        Assert.Equal(original.Name, clone.Name);
        Assert.Equal(original.Description, clone.Description);
        Assert.Equal(original.Type, clone.Type);
        Assert.Equal(original.Weight, clone.Weight);
        Assert.Equal(original.Value, clone.Value);
        Assert.Equal(original.StrengthBonus, clone.StrengthBonus);
        Assert.Equal(original.DexterityBonus, clone.DexterityBonus);
        Assert.Equal(original.IntelligenceBonus, clone.IntelligenceBonus);
        Assert.Equal(original.DefenseBonus, clone.DefenseBonus);
        Assert.Equal(original.AttackBonus, clone.AttackBonus);
        Assert.Equal(original.ArmorClass, clone.ArmorClass);
        Assert.Equal(original.DamageDice, clone.DamageDice);
        Assert.Equal(original.EquipmentSlot, clone.EquipmentSlot);
        Assert.Equal(original.EquipmentSpriteId, clone.EquipmentSpriteId);
        Assert.Equal(original.RequiredStrength, clone.RequiredStrength);
        Assert.Equal(original.RequiredDexterity, clone.RequiredDexterity);
        Assert.Equal(original.RequiredIntelligence, clone.RequiredIntelligence);
        Assert.Equal(original.RequiredLevel, clone.RequiredLevel);
        Assert.Equal(original.Tier, clone.Tier);
        Assert.Equal(original.Infusion, clone.Infusion);
        Assert.Equal(original.Quantity, clone.Quantity);
    }

    [Fact]
    public void Clone_DoesNotMutateOriginal()
    {
        var original = new Item { Name = "Health Potion", Quantity = 5 };
        var clone = original.Clone();
        clone.Quantity = 1;
        Assert.Equal(5, original.Quantity);
    }
}
