using Darkness.Core.Models;

namespace Darkness.Core.Interfaces
{
    public interface ICombatService
    {
        /// <summary>
        /// Calculates the turn order for all combat participants.
        /// </summary>
        /// <param name="party">The player party.</param>
        /// <param name="enemies">List of enemies.</param>
        /// <returns>A sorted list of participants by their initiative.</returns>
        List<object> CalculateTurnOrder(List<Character> party, List<Enemy> enemies);

        /// <summary>
        /// Calculates damage dealt by a character to an enemy.
        /// </summary>
        /// <param name="attacker">The character attacking.</param>
        /// <param name="defender">The enemy defending.</param>
        /// <param name="skill">The skill used, if any.</param>
        /// <returns>The amount of damage dealt.</returns>
        int CalculateDamage(Character attacker, Enemy defender, Skill? skill = null);

        /// <summary>
        /// Calculates damage dealt by an enemy to a character.
        /// </summary>
        /// <param name="attacker">The enemy attacking.</param>
        /// <param name="defender">The character defending.</param>
        /// <param name="skill">The skill used, if any.</param>
        /// <returns>The amount of damage dealt.</returns>
        int CalculateDamage(Enemy attacker, Character defender, Skill? skill = null);

        /// <summary>
        /// Checks if a status effect is applied based on resistance.
        /// </summary>
        /// <param name="target">The target of the status effect.</param>
        /// <param name="effect">The status effect being applied.</param>
        /// <returns>True if the effect is applied, false if resisted.</returns>
        bool CheckStatusEffect(Character target, StatusEffect effect);

        /// <summary>
        /// Checks if a status effect is applied based on resistance.
        /// </summary>
        /// <param name="target">The target of the status effect.</param>
        /// <param name="effect">The status effect being applied.</param>
        /// <returns>True if the effect is applied, false if resisted.</returns>
        bool CheckStatusEffect(Enemy target, StatusEffect effect);
    }
}
