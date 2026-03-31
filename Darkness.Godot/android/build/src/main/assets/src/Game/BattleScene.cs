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
	private INavigationService _navigation;
	private ISessionService _session;
	private ICombatService _combat;
	private ISpriteCompositor _compositor;
	private ISpriteLayerCatalog _catalog;
	private IFileSystemService _fileSystem;
	private RichTextLabel _combatLog;
	private PauseMenu _pauseMenu;

	private HBoxContainer _partyContainer;
	private HBoxContainer _enemyContainer;

	private List<Character> _party = new();
	private List<Enemy> _enemies = new();

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
		_enemyContainer = GetNode<HBoxContainer>("CombatArea/EnemyContainer");

		GetNode<Button>("ActionsArea/Attack1").Pressed += () => ExecuteAttack(0);
		GetNode<Button>("ActionsArea/Attack2").Pressed += () => ExecuteAttack(1);
		GetNode<Button>("ActionsArea/Attack3").Pressed += () => ExecuteAttack(2);
		GetNode<Button>("MenuButton").Pressed += () => _pauseMenu.Toggle();

		SetupBattle();
		await UpdateSprites();
	}

	public override void _Input(InputEvent @event)
	{
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			_pauseMenu.Toggle();
		}
	}

	private void SetupBattle()
	{
		if (_session.CurrentCharacter != null)
		{
			_party.Add(_session.CurrentCharacter);
		}

		if (_enemies.Count == 0)
		{
			_enemies.Add(new Enemy { Name = "Hound", MaxHP = 50, CurrentHP = 50, Defense = 5 });
			_enemies.Add(new Enemy { Name = "Shadow", MaxHP = 75, CurrentHP = 75, Defense = 8 });
		}
	}

	private async Task UpdateSprites()
	{
		foreach (Node child in _partyContainer.GetChildren()) child.QueueFree();
		foreach (Node child in _enemyContainer.GetChildren()) child.QueueFree();

		foreach (var character in _party)
		{
			var sprite = new AnimatedSprite2D { Scale = new Vector2(2, 2) };
			_partyContainer.AddChild(sprite);
			sprite.SpriteFrames = await LoadCharacterFrames(character);
			sprite.Play("idle_right");
		}

		foreach (var enemy in _enemies)
		{
			var sprite = new AnimatedSprite2D { Scale = new Vector2(2, 2) };
			_enemyContainer.AddChild(sprite);
			var knightAppearance = _catalog.GetDefaultAppearanceForClass("Knight");
			sprite.SpriteFrames = await LoadCharacterFrames(new Character {
				SkinColor = knightAppearance.SkinColor,
				HairStyle = knightAppearance.HairStyle,
				HairColor = knightAppearance.HairColor,
				ArmorType = knightAppearance.ArmorType,
				WeaponType = knightAppearance.WeaponType
			});
			sprite.Play("idle_left");
		}
	}

	private async Task<SpriteFrames?> LoadCharacterFrames(Character c)
	{
		var appearance = new CharacterAppearance
		{
			SkinColor = c.SkinColor,
			Face = c.Face ?? "Default",
			Eyes = c.Eyes ?? "Default",
			HairStyle = c.HairStyle,
			HairColor = c.HairColor,
			ArmorType = c.ArmorType,
			WeaponType = c.WeaponType,
			Head = "Human Male"
		};

		try
		{
			var layers = _catalog.GetLayersForAppearance(appearance);
			var streams = new List<System.IO.Stream>();
			foreach (var layer in layers)
			{
				var stream = await _fileSystem.OpenAppPackageFileAsync(layer.ResourcePath);
				streams.Add(stream);
			}

			if (streams.Count > 0)
			{
				var sheetBytes = _compositor.CompositeLayers(streams, 576, 256);
				return ImageUtils.CreateSpriteFrames(sheetBytes, 64, 64);
			}
			
			foreach (var s in streams) s.Dispose();
		}
		catch (System.Exception ex)
		{
			GD.PrintErr($"[BattleScene] Failed to load frames: {ex.Message}");
		}
		return null;
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
			UpdateSprites().FireAndForget();
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
