using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Models;

namespace Darkness.Core.Logic
{
    /// <summary>
    /// Pure helpers for filtering skill collections. Kept in Core so both the
    /// Godot UI and unit tests exercise the same production logic.
    /// </summary>
    public static class SkillFilter
    {
        /// <summary>
        /// Returns only the active (non-passive) skills. Passive skills are not
        /// assignable to active hotbar slots and are excluded from the skills UI.
        /// </summary>
        public static IEnumerable<Skill> ActiveOnly(IEnumerable<Skill> skills)
        {
            return skills.Where(s => !s.IsPassive);
        }
    }
}
