using Darkness.Core.Interfaces;
using Darkness.Core.Models;

namespace Darkness.Core.Services;

public class EquipmentService : IEquipmentService
{
    public bool Equip(Character character, Item item)
    {
        if (character == null || item == null) return false;
        if (!item.CanEquip(character, out _)) return false;
        if (string.IsNullOrEmpty(item.EquipmentSlot)) return false;

        // Unequip existing item in that slot first
        Unequip(character, item.EquipmentSlot);

        // Apply stat bonuses
        AddBonus(character.StatBonuses, "Strength", item.StrengthBonus);
        AddBonus(character.StatBonuses, "Dexterity", item.DexterityBonus);
        AddBonus(character.StatBonuses, "Intelligence", item.IntelligenceBonus);
        AddBonus(character.StatBonuses, "Defense", item.DefenseBonus);
        AddBonus(character.StatBonuses, "Attack", item.AttackBonus);

        character.EquipmentSlots[item.EquipmentSlot] = item.Name;

        // Ensure the item is in inventory so Unequip can find it for bonus reversal
        if (!character.Inventory.Any(i => i.Name == item.Name))
            character.Inventory.Add(item);

        character.RecalculateDerivedStats();
        return true;
    }

    public bool Unequip(Character character, string slot)
    {
        if (character == null || string.IsNullOrEmpty(slot)) return false;
        if (!character.EquipmentSlots.ContainsKey(slot)) return false;

        var itemName = character.EquipmentSlots[slot];
        var item = character.Inventory.FirstOrDefault(i => i.Name == itemName);

        if (item != null)
        {
            RemoveBonus(character.StatBonuses, "Strength", item.StrengthBonus);
            RemoveBonus(character.StatBonuses, "Dexterity", item.DexterityBonus);
            RemoveBonus(character.StatBonuses, "Intelligence", item.IntelligenceBonus);
            RemoveBonus(character.StatBonuses, "Defense", item.DefenseBonus);
            RemoveBonus(character.StatBonuses, "Attack", item.AttackBonus);
        }

        character.EquipmentSlots.Remove(slot);
        character.RecalculateDerivedStats();
        return true;
    }

    private static void AddBonus(Dictionary<string, int> bonuses, string stat, int value)
    {
        if (value == 0) return;
        bonuses[stat] = bonuses.GetValueOrDefault(stat) + value;
    }

    private static void RemoveBonus(Dictionary<string, int> bonuses, string stat, int value)
    {
        if (value == 0) return;
        var newVal = bonuses.GetValueOrDefault(stat) - value;
        if (newVal == 0) bonuses.Remove(stat);
        else bonuses[stat] = newVal;
    }
}
