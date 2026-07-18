using System.Collections.Generic;
using Darkness.Core.Models;

namespace Darkness.Core.Logic;

/// <summary>
/// Result of building a player-side party for a battle: the ordered roster (PC first, then allies)
/// plus a lookup of each AI ally's render sprite key (allies are declared in JSON as
/// <see cref="EnemySpawn"/>, so their sprite key does not live on <see cref="Character"/>).
/// </summary>
public class PartyBuildResult
{
    /// <summary>Player-side roster in turn-eligible order: <c>Party[0]</c> is the player-controlled PC,
    /// every subsequent member is an AI-controlled ally (Darius, Tywin, ...).</summary>
    public List<Character> Party { get; } = new();

    /// <summary>Maps each AI-ally <see cref="Character"/> to the <see cref="EnemySpawn.SpriteKey"/> it was
    /// created from, so the scene can render it (e.g. <c>bosses/Darius</c> via the full-sheet path).
    /// The PC is never present here — it renders through equipment compositing.</summary>
    public Dictionary<Character, string> AllySpriteKeys { get; } = new();
}

/// <summary>
/// Pure, Godot-free construction of battle rosters from a <see cref="CombatData"/>.
///
/// <para>Quest beats declare player-side allies (Darius, and Tywin from beat 9) in
/// <see cref="CombatData.Allies"/> using the <see cref="EnemySpawn"/> shape. This factory maps those
/// spawns onto player-side <see cref="Character"/> instances (PC first, then allies) and maps enemy
/// spawns onto <see cref="Enemy"/> instances, so the humble-object <see cref="PartyBattleController"/>
/// and the BattleScene can drive a party-vs-enemies fight instead of a single PC.</para>
///
/// <para>Behaviour-neutral for existing beats: when <see cref="CombatData.Allies"/> is empty (all
/// current quest JSON), <see cref="BuildParty"/> yields exactly the single-PC party the scene used before.</para>
/// </summary>
public static class PartyFactory
{
    /// <summary>
    /// Maps an <see cref="EnemySpawn"/>-shaped ally declaration onto a player-side <see cref="Character"/>.
    /// Base attributes drive combat rolls (Strength modifier for damage output, Dexterity for
    /// initiative and armor class), while the stored derived stats mirror the spawn's values.
    /// </summary>
    public static Character CreateAlly(EnemySpawn ally)
    {
        int maxHp = ally.MaxHP > 0 ? ally.MaxHP : 100;
        int currentHp = ally.CurrentHP > 0 ? ally.CurrentHP : maxHp;

        return new Character
        {
            Name = ally.Name,
            Level = ally.Level,
            MaxHP = maxHp,
            CurrentHP = currentHp,
            // Base attributes feed CombatEngine's modifier math (Character attacker uses Strength;
            // initiative and AC use Dexterity). Map Attack->STR and Speed->DEX so stronger/faster
            // allies fight better and take turns in a sensible slot.
            BaseStrength = ally.Attack > 0 ? ally.Attack : 10,
            BaseDexterity = ally.Speed > 0 ? ally.Speed : 10,
            // Stored derived stats mirror the spawn so display / defender math stays consistent.
            Attack = ally.Attack,
            Defense = ally.Defense,
            Speed = ally.Speed,
            Accuracy = ally.Accuracy > 0 ? ally.Accuracy : 80,
            Evasion = ally.Evasion,
            ArmorClass = ally.Defense,
            IsBlocking = false,
        };
    }

    /// <summary>
    /// Maps an <see cref="EnemySpawn"/> onto a combat <see cref="Enemy"/>. At battle start
    /// (<paramref name="isResuming"/> == false) a zero <see cref="EnemySpawn.CurrentHP"/> is treated as
    /// full HP; when resuming, the persisted current HP is used verbatim. Mirrors the mapping the
    /// BattleScene previously performed inline.
    /// </summary>
    public static Enemy CreateEnemy(EnemySpawn e, bool isResuming)
    {
        int initialHP = e.CurrentHP;
        if (initialHP <= 0 && !isResuming) initialHP = e.MaxHP;

        return new Enemy
        {
            Name = e.Name,
            Level = e.Level,
            MaxHP = e.MaxHP,
            CurrentHP = initialHP,
            Attack = e.Attack > 0 ? e.Attack : 10,
            Defense = e.Defense > 0 ? e.Defense : 5,
            Speed = e.Speed > 0 ? e.Speed : 10,
            Accuracy = e.Accuracy > 0 ? e.Accuracy : 80,
            Evasion = e.Evasion,
            SpriteKey = e.SpriteKey ?? "knight",
            SpriteOffsetX = e.SpriteOffsetX,
            SpriteOffsetY = e.SpriteOffsetY,
            IsInvincible = e.IsInvincible,
            MoralityImpact = e.MoralityImpact,
            ExperienceReward = e.ExperienceReward,
            GoldReward = e.GoldReward,
        };
    }

    /// <summary>
    /// Builds the ordered player-side party for a battle: the session PC first (player-controlled),
    /// followed by every ally declared in <paramref name="combat"/>.<see cref="CombatData.Allies"/>
    /// (AI-controlled), each mapped via <see cref="CreateAlly"/>. Also returns the ally sprite-key
    /// lookup for rendering.
    ///
    /// <para>The PC's HP is defaulted if unset (MaxHP -> 100, CurrentHP -> MaxHP) and its blocking flag
    /// is cleared, mirroring the scene's previous setup. When <paramref name="combat"/> is null or has no
    /// allies, the result is a single-PC party (behaviour-neutral for existing beats).</para>
    /// </summary>
    public static PartyBuildResult BuildParty(Character pc, CombatData? combat)
    {
        var result = new PartyBuildResult();

        if (pc.MaxHP <= 0) pc.MaxHP = 100;
        if (pc.CurrentHP <= 0) pc.CurrentHP = pc.MaxHP;
        pc.IsBlocking = false;
        result.Party.Add(pc);

        if (combat?.Allies != null)
        {
            foreach (var allySpawn in combat.Allies)
            {
                var ally = CreateAlly(allySpawn);
                result.Party.Add(ally);
                result.AllySpriteKeys[ally] = allySpawn.SpriteKey ?? string.Empty;
            }
        }

        return result;
    }

    /// <summary>Builds the enemy roster from <paramref name="combat"/>.<see cref="CombatData.Enemies"/>.</summary>
    public static List<Enemy> BuildEnemies(CombatData combat, bool isResuming)
    {
        var enemies = new List<Enemy>();
        foreach (var e in combat.Enemies)
        {
            enemies.Add(CreateEnemy(e, isResuming));
        }
        return enemies;
    }
}
