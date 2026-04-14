using System.Collections.Generic;
using Darkness.Core.Models;

namespace Darkness.Core.Interfaces;

public interface IWeaponSkillService
{
    List<Skill> GetSkillsForWeapon(string? weaponType, string? offHandType, string? shieldType, List<string>? unlockedTalentIds = null);
    List<Skill> GetAvailableSkills(Character character);
    List<Skill> GetEquippedSkills(Character character);
}