using System.Collections.Generic;
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IWeaponSkillService
{
    List<Skill> GetSkillsForWeapon(string? weaponType, string? offHandType, string? shieldType);
}