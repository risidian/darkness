using System;
using System.Collections.Generic;
using System.Linq;
using Darkness.Core.Models;

namespace Darkness.Core.Logic;

/// <summary>
/// Role of a participant in a party battle, used to decide who supplies the next action.
/// </summary>
public enum ParticipantRole
{
    /// <summary>The player-controlled PC — the UI/human chooses this turn's action.</summary>
    PlayerControlled,

    /// <summary>A named party ally (e.g. Darius, Tywin) — the AI chooses this turn's action.</summary>
    AiAlly,

    /// <summary>An enemy combatant — the AI chooses this turn's action.</summary>
    Enemy,
}

/// <summary>
/// Testable "humble object" orchestration model for party-vs-enemies combat.
/// Owns a battle in progress: turn order, who acts next (and by whom — player, AI ally, or enemy),
/// deterministic target selection, dead-participant removal, and victory/defeat/survival evaluation.
///
/// This is pure Core logic with NO Godot dependencies and NO combat side effects — it never mutates
/// HP or applies damage; it only reads participant state (e.g. <see cref="Character.CurrentHP"/>) and
/// reports decisions. The Godot BattleScene will later delegate its orchestration here (separate task);
/// this type is purely additive and changes no runtime behavior on its own.
///
/// <para><b>Player vs AI flagging:</b> the player-side roster is a single <c>party</c> list. Membership
/// in <see cref="_aiAllies"/> marks a character as AI-controlled. By convention the first party member
/// (<c>party[0]</c>) is the player-controlled PC and every other member is an AI ally (Darius, Tywin).
/// Callers may override this by passing an explicit <c>aiAllies</c> set to the constructor.</para>
/// </summary>
public class PartyBattleController
{
    private readonly TurnManager _turnManager = new();
    private readonly List<Character> _party;
    private readonly List<Enemy> _enemies;
    private readonly HashSet<Character> _aiAllies;

    /// <summary>The full player-side roster (PC first, then allies). Read-only.</summary>
    public IReadOnlyList<Character> Party => _party;

    /// <summary>The enemy roster. Read-only.</summary>
    public IReadOnlyList<Enemy> Enemies => _enemies;

    /// <summary>Number of rounds the party must survive for a survival-beat victory, or null for a normal battle.</summary>
    public int? SurvivalTurns { get; }

    /// <summary>Current turn order (living + not-yet-removed participants). Read-only.</summary>
    public IReadOnlyList<object> TurnOrder => _turnManager.TurnOrder;

    /// <summary>The participant whose turn it currently is (a <see cref="Character"/> or <see cref="Enemy"/>), or null.</summary>
    public object? CurrentParticipant => _turnManager.CurrentEntity;

    /// <summary>1-based combat round.</summary>
    public int CurrentRound => _turnManager.CurrentRound;

    /// <summary>
    /// Selector deciding which living party member an enemy attacks. Defaults to first-living
    /// (deterministic). Receives only the living party members; return null if none remain.
    /// </summary>
    public Func<IReadOnlyList<Character>, Character?> PartyTargetSelector { get; set; }

    /// <summary>
    /// Selector deciding which living enemy an AI ally attacks. Defaults to first-living
    /// (deterministic). Receives only the living enemies; return null if none remain.
    /// </summary>
    public Func<IReadOnlyList<Enemy>, Enemy?> EnemyTargetSelector { get; set; }

    public PartyBattleController(
        List<Character> party,
        List<Enemy> enemies,
        CombatEngine? engine = null,
        int? survivalTurns = null,
        IEnumerable<Character>? aiAllies = null)
    {
        _party = party ?? new List<Character>();
        _enemies = enemies ?? new List<Enemy>();
        SurvivalTurns = survivalTurns;

        // Convention: party[0] is the player-controlled PC; all remaining members are AI allies.
        // An explicit aiAllies set overrides this default.
        _aiAllies = aiAllies != null
            ? new HashSet<Character>(aiAllies)
            : new HashSet<Character>(_party.Skip(1));

        PartyTargetSelector = list => list.Count > 0 ? list[0] : null;
        EnemyTargetSelector = list => list.Count > 0 ? list[0] : null;

        _turnManager.Setup(_party, _enemies, engine ?? new CombatEngine());
    }

    // --- Living views ---

    /// <summary>Party members still alive (CurrentHP &gt; 0), in roster order.</summary>
    public IReadOnlyList<Character> LivingParty => _party.Where(c => c.CurrentHP > 0).ToList();

    /// <summary>Enemies still alive (CurrentHP &gt; 0), in roster order.</summary>
    public IReadOnlyList<Enemy> LivingEnemies => _enemies.Where(e => e.CurrentHP > 0).ToList();

    // --- Role classification ---

    /// <summary>True if the character is the player-controlled PC (in the party and not flagged AI).</summary>
    public bool IsPlayerControlled(Character c) => _party.Contains(c) && !_aiAllies.Contains(c);

    /// <summary>True if the character is an AI-controlled party ally.</summary>
    public bool IsAiAlly(Character c) => _aiAllies.Contains(c);

    /// <summary>Classifies any participant object as player-controlled, AI ally, or enemy.</summary>
    public ParticipantRole RoleOf(object participant)
    {
        return participant switch
        {
            Enemy => ParticipantRole.Enemy,
            Character c => _aiAllies.Contains(c) ? ParticipantRole.AiAlly : ParticipantRole.PlayerControlled,
            _ => throw new ArgumentException($"Unknown participant type: {participant?.GetType().Name ?? "null"}"),
        };
    }

    /// <summary>The role of the current participant, or null if the turn order is empty.</summary>
    public ParticipantRole? CurrentRole => CurrentParticipant is null ? null : RoleOf(CurrentParticipant);

    // --- Turn progression ---

    /// <summary>Advances to the next participant's turn (looping and incrementing the round as needed).</summary>
    public void AdvanceTurn() => _turnManager.NextTurn();

    /// <summary>
    /// Removes every defeated (CurrentHP &lt;= 0) participant from the turn order, keeping the current
    /// index aligned. Defeated participants remain in <see cref="Party"/>/<see cref="Enemies"/> so
    /// victory/defeat evaluation still counts them. Reuses <see cref="TurnManager.RemoveEntity"/>.
    /// </summary>
    public void RemoveDefeated()
    {
        foreach (var c in _party.Where(c => c.CurrentHP <= 0).ToList())
        {
            _turnManager.RemoveEntity(c);
        }
        foreach (var e in _enemies.Where(e => e.CurrentHP <= 0).ToList())
        {
            _turnManager.RemoveEntity(e);
        }
    }

    // --- Target selection (deterministic; no side effects) ---

    /// <summary>Picks the living party member the current (or a) enemy should attack, or null if all are down.</summary>
    public Character? ChooseEnemyTarget() => PartyTargetSelector(LivingParty);

    /// <summary>Picks the living enemy the current (or an) AI ally should attack, or null if all are down.</summary>
    public Enemy? ChooseAllyAiTarget() => EnemyTargetSelector(LivingEnemies);

    // --- Outcome evaluation ---

    /// <summary>True when every enemy is defeated.</summary>
    public bool AllEnemiesDefeated => _enemies.Count > 0 && _enemies.All(e => e.CurrentHP <= 0);

    /// <summary>True when every party member is defeated.</summary>
    public bool AllPartyDefeated => _party.Count > 0 && _party.All(c => c.CurrentHP <= 0);

    /// <summary>True when at least one enemy is an invincible boss (a survival-beat scenario).</summary>
    public bool HasInvincibleBoss => _enemies.Any(e => e.IsInvincible);

    /// <summary>
    /// True once the party has survived the required number of rounds. Matches the BattleScene rule
    /// (survived-turns = CurrentRound - 1 &gt;= SurvivalTurns), i.e. CurrentRound &gt; SurvivalTurns.
    /// </summary>
    public bool SurvivalGoalReached =>
        SurvivalTurns.HasValue && SurvivalTurns.Value > 0 && CurrentRound > SurvivalTurns.Value;

    /// <summary>Victory when all enemies are dead, or (survival beats) when the party has outlasted the required rounds.</summary>
    public bool IsVictory() => AllEnemiesDefeated || SurvivalGoalReached;

    /// <summary>Defeat when the entire party is down.</summary>
    public bool IsDefeat() => AllPartyDefeated;
}
