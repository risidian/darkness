using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Logic;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace Darkness.Godot.Game;

public partial class BattleScene : Control, IInitializable
{
    private INavigationService _navigation;
    private ISessionService _session;
    private ICombatService _combat;
    private RichTextLabel _combatLog;

    private List<Character> _party = new();
    private List<Enemy> _enemies = new();

    public void Initialize(IDictionary<string, object> parameters)
    {
        // Parameters passed from WorldScene
    }

    public override void _Ready()
    {
        var global = GetNode<Global>("/root/Global");
        _navigation = global.Services.GetRequiredService<INavigationService>();
        _session = global.Services.GetRequiredService<ISessionService>();
        _combat = global.Services.GetRequiredService<ICombatService>();

        _combatLog = GetNode<RichTextLabel>("CombatLog");

        GetNode<Button>("ActionsArea/Attack1").Pressed += () => ExecuteAttack(0);
        GetNode<Button>("ActionsArea/Attack2").Pressed += () => ExecuteAttack(1);
        GetNode<Button>("ActionsArea/Attack3").Pressed += () => ExecuteAttack(2);

        SetupBattle();
    }

    private void SetupBattle()
    {
        if (_session.CurrentCharacter != null)
        {
            _party.Add(_session.CurrentCharacter);
        }

        // Spawn test enemies
        _enemies.Add(new Enemy { Name = "Hound", MaxHP = 50, CurrentHP = 50 });
        _enemies.Add(new Enemy { Name = "Shadow", MaxHP = 75, CurrentHP = 75 });
    }

    private void ExecuteAttack(int enemyIndex)
    {
        if (enemyIndex >= _enemies.Count || _party.Count == 0) return;

        var attacker = _party[0];
        var target = _enemies[enemyIndex];

        int damage = _combat.CalculateDamage(attacker, target);
        target.CurrentHP -= damage;

        _combatLog.AppendText($"\n[color=red]{attacker.Name}[/color] attacks {target.Name} for {damage} damage!");

        if (target.CurrentHP <= 0)
        {
            _combatLog.AppendText($"\n[color=gold]{target.Name} is defeated![/color]");
            _enemies.RemoveAt(enemyIndex);
        }

        if (_enemies.Count == 0)
        {
            Victory();
        }
    }

    private async void Victory()
    {
        _combatLog.AppendText("\n[color=yellow]VICTORY![/color]");
        await ToSignal(GetTree().CreateTimer(2.0), "timeout");
        _navigation.NavigateToAsync("MainPage");
    }
}
