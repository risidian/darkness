using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Logic;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darkness.Godot.UI;

namespace Darkness.Godot.Game;

public partial class BattleScene : Control, IInitializable
{
	private INavigationService _navigation = null!;
	private ISessionService _session = null!;
	private ICombatService _combat = null!;
	private ISpriteCompositor _compositor = null!;
	private ISpriteLayerCatalog _catalog = null!;
	private IFileSystemService _fileSystem = null!;
	private RichTextLabel _combatLog = null!;
	private PauseMenu _pauseMenu = null!;

	private HBoxContainer _partyContainer = null!;
	private VBoxContainer _enemyContainer = null!;

	private List<Character> _party = new();
	private List<Enemy> _enemies = new();
	private List<LayeredSprite> _partySprites = new();
	private List<LayeredSprite> _enemySprites = new();

	public void Initialize(IDictionary<string, object> parameters)
	{
		if (parameters.ContainsKey("Encounter") && parameters["Encounter"] is DeathmatchEncounter encounter)
		{
			_enemies.Clear();
			foreach (var e in encounter.Enemies)
			{
				_enemies.Add(new Enemy 
				{ 
					Name = e.Name, 
					MaxHP = e.MaxHP, 
					CurrentHP = e.MaxHP,
					Defense = 5
				});
			}
		}
	}

	public override async void _Ready()
	{
		var global = GetNode<Global>("/root/Global");
		var sp = global.Services!;
		_navigation = sp.GetRequiredService<INavigationService>();
		_session = sp.GetRequiredService<ISessionService>();
		_combat = sp.GetRequiredService<ICombatService>();
		_compositor = sp.GetRequiredService<ISpriteCompositor>();
		_catalog = sp.GetRequiredService<ISpriteLayerCatalog>();
		_fileSystem = sp.GetRequiredService<IFileSystemService>();

		_combatLog = GetNode<RichTextLabel>("CombatLog");
		_pauseMenu = GetNode<PauseMenu>("PauseMenu");
		_partyContainer = GetNode<HBoxContainer>("CombatArea/PartyContainer");
		_enemyContainer = GetNode<VBoxContainer>("CombatArea/EnemyContainer");

		GetNode<Button>("ActionsArea/Attack1").Pressed += () => ExecuteAttack(0);
		GetNode<Button>("ActionsArea/Attack2").Pressed += () => ExecuteAttack(1);
		GetNode<Button>("ActionsArea/Attack3").Pressed += () => ExecuteAttack(2);
		GetNode<Button>("TopRightMenu/MenuButton").Pressed += () => _pauseMenu.Toggle();

		SetupBattle();
		await UpdateSprites();
	}

	private void SetupBattle()
	{
		if (_session.CurrentCharacter != null)
		{
			_party.Add(_session.CurrentCharacter);
		}

		if (_enemies.Count == 0)
		{
			_enemies.Add(new Enemy { Name = "Hellhound Alpha", MaxHP = 60, CurrentHP = 60, Defense = 5 });
			_enemies.Add(new Enemy { Name = "Hellhound Beta", MaxHP = 50, CurrentHP = 50, Defense = 5 });
			_enemies.Add(new Enemy { Name = "Hellhound Gamma", MaxHP = 50, CurrentHP = 50, Defense = 5 });
		}
	}

	private async Task UpdateSprites()
	{
		GD.Print($"[BattleScene] UpdateSprites started. Party: {_party.Count}, Enemies: {_enemies.Count}");
		foreach (Node child in _partyContainer.GetChildren()) child.QueueFree();
		foreach (Node child in _enemyContainer.GetChildren()) child.QueueFree();
		_partySprites.Clear();
		_enemySprites.Clear();

		var layeredSpriteScene = GD.Load<PackedScene>("res://scenes/LayeredSprite.tscn");

		foreach (var character in _party)
		{
			var sprite = layeredSpriteScene.Instantiate<LayeredSprite>();
			sprite.Hide();
			
			var wrapper = new Control { CustomMinimumSize = new Vector2(200, 120) };
			wrapper.AddChild(sprite);
			sprite.Position = new Vector2(100, 60);
			sprite.Scale = new Vector2(2.5f, 2.5f);

			_partyContainer.AddChild(wrapper);
			_partySprites.Add(sprite);

			await sprite.SetupCharacter(character, _catalog, _fileSystem);
			sprite.Play("idle_right"); // LPC sheets use rows for direction
			sprite.Show();
		}

		foreach (var enemy in _enemies)
		{
			var sprite = layeredSpriteScene.Instantiate<LayeredSprite>();
			sprite.Hide();

			var wrapper = new Control { CustomMinimumSize = new Vector2(200, 120) };
			wrapper.AddChild(sprite);
			sprite.Position = new Vector2(100, 60);

			_enemyContainer.AddChild(wrapper);
			_enemySprites.Add(sprite);

			if (enemy.Name.ToLower().Contains("hound"))
			{
				sprite.Scale = new Vector2(2.5f, 2.5f);
				await sprite.SetupMonster("hound", _fileSystem);
				sprite.Play("idle");
				
				// Hounds face LEFT by default in your PNGs.
				// Enemies are on the right, so they should face LEFT to see the player.
				// Therefore, FlipH should be FALSE.
				sprite.FlipH = false; 
			}
			else
			{
				sprite.Scale = new Vector2(2.5f, 2.5f);
				var knightAppearance = _catalog.GetDefaultAppearanceForClass("Knight");
				await sprite.SetupCharacter(new Character {
					SkinColor = knightAppearance.SkinColor,
					HairStyle = knightAppearance.HairStyle,
					HairColor = knightAppearance.HairColor,
					ArmorType = knightAppearance.ArmorType,
					WeaponType = knightAppearance.WeaponType,
					Feet = knightAppearance.Feet,
					Arms = knightAppearance.Arms,
					Legs = knightAppearance.Legs
				}, _catalog, _fileSystem);
				sprite.Play("idle_left");
			}
			sprite.Show();
		}
		GD.Print("[BattleScene] UpdateSprites complete.");
	}

	private async void ExecuteAttack(int enemyIndex)
	{
		if (_enemies.Count == 0 || _party.Count == 0) return;
		
		int actualIndex = enemyIndex < _enemies.Count ? enemyIndex : 0;

		var attacker = _party[0];
		var target = _enemies[actualIndex];
		var attackerSprite = _partySprites[0];
		var targetSprite = _enemySprites[actualIndex];

		// Safety check for disposed objects
		if (!GodotObject.IsInstanceValid(attackerSprite) || !GodotObject.IsInstanceValid(targetSprite)) return;

		attackerSprite.Play("walk_right");
		
		int damage = _combat.CalculateDamage(attacker, target);
		target.CurrentHP -= damage;

		_combatLog.AppendText($"\n[color=red]{attacker.Name}[/color] attacks {target.Name} for {damage} damage!");

		await ToSignal(GetTree().CreateTimer(0.5), "timeout");
		if (GodotObject.IsInstanceValid(attackerSprite))
			attackerSprite.Play("idle_right");

		if (target.CurrentHP <= 0)
		{
			_combatLog.AppendText($"\n[color=gold]{target.Name} is defeated![/color]");
			_enemies.RemoveAt(actualIndex);
			await UpdateSprites();
		}
		else
		{
			await ToSignal(GetTree().CreateTimer(0.5), "timeout");
			if (!GodotObject.IsInstanceValid(targetSprite) || !GodotObject.IsInstanceValid(attackerSprite)) return;
			
			if (target.Name.ToLower().Contains("hound"))
				targetSprite.Play("jump");
			else
				targetSprite.Play("walk_left");

			int enemyDamage = 5; 
			attacker.CurrentHP -= enemyDamage;
			_combatLog.AppendText($"\n[color=orange]{target.Name}[/color] bites back for {enemyDamage} damage!");

			await ToSignal(GetTree().CreateTimer(0.8), "timeout");
			
			if (GodotObject.IsInstanceValid(targetSprite))
			{
				if (target.Name.ToLower().Contains("hound"))
					targetSprite.Play("idle");
				else
					targetSprite.Play("idle_left");
			}
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
		await _navigation.NavigateToAsync("MainMenuPage");
	}
}
