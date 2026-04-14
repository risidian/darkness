using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IEquipmentService
{
    bool Equip(Character character, Item item);
    bool Unequip(Character character, string slot);
}
