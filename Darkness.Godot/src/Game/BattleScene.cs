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
	private Control _endBattlePanel = null!;
	private Label _endBattleTitle = null!;
	private Label _endBattleMessage = null!;
	private Button _retryButton = null!;

	private HBoxContainer _partyContainer = null!;
	private VBoxContainer _enemyContainer = null!;

	private List<Character> _party = new();
	private List<Enemy> _enemies = new();
	private List<Enemy> _originalEnemies = new(); // For retry
	private List<LayeredSprite> _partySprites = new();
	private List<LayeredSprite> _enemySprites = new();
	private List<StatusBar> _partyHealthBars = new();
	private List<StatusBar> _enemyHealthBars = new();
	private bool _isProcessingTurn = false;
	private int _selectedEnemyIndex = 0;

	private void OnEnemyTapped(int index)
	{
		if (index >= _enemies.Count) return;
		
		// Clear previous highlight
		if (_selectedEnemyIndex < _enemyHealthBars.Count)
			_enemyHealthBars[_selectedEnemyIndex].SetHighlighted(false);
			
		_selectedEnemyIndex = index;
		
		// Set new highlight
		if (_selectedEnemyIndex < _enemyHealthBars.Count)
			_enemyHealthBars[_selectedEnemyIndex].SetHighlighted(true);
			
		GD.Print($"[Battle] Target selected: {_enemies[_selectedEnemyIndex].Name}");
	}

	public void Initialize(IDictionary<string, object> parameters)
	{
		if (parameters.ContainsKey("Args") && parameters["Args"] is BattleArgs args)
		{
			if (args.Encounter != null)
			{
				_enemies.Clear();
				_originalEnemies.Clear();
				foreach (var e in args.Encounter.Enemies)
				{
					// Deep copy for independent state tracking
					var enemy = new Enemy 
					{ 
						Name = e.Name, 
						MaxHP = e.MaxHP, 
						CurrentHP = e.CurrentHP <= 0 ? e.MaxHP : e.CurrentHP,
						Defense = e.Defense > 0 ? e.Defense : 5,
						SpriteKey = e.SpriteKey ?? "knight"
					};
					_enemies.Add(enemy);
					_originalEnemies.Add(new Enemy { Name = enemy.Name, MaxHP = enemy.MaxHP, CurrentHP = enemy.MaxHP, Defense = enemy.Defense, SpriteKey = enemy.SpriteKey });
				}
			}
		}
		// Backward compatibility for old DeathmatchEncounter
		else if (parameters.ContainsKey("Encounter") && parameters["Encounter"] is DeathmatchEncounter encounter)
		{
			_enemies.Clear();
			_originalEnemies.Clear();
			foreach (var e in encounter.Enemies)
			{
				var enemy = new Enemy 
				{ 
					Name = e.Name, 
					MaxHP = e.MaxHP, 
					CurrentHP = e.MaxHP,
					Defense = 5
				};
				_enemies.Add(enemy);
				_originalEnemies.Add(new Enemy { Name = enemy.Name, MaxHP = enemy.MaxHP, CurrentHP = enemy.MaxHP, Defense = enemy.Defense });
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
		_endBattlePanel = GetNode<Control>("EndBattlePanel");
		_endBattleTitle = GetNode<Label>("%Title");
		_endBattleMessage = GetNode<Label>("%Message");
		_retryButton = GetNode<Button>("%RetryButton");
		
		_partyContainer = GetNode<HBoxContainer>("CombatArea/PartyContainer");
		_enemyContainer = GetNode<VBoxContainer>("CombatArea/EnemyContainer");

		GetNode<Button>("ActionsArea/Attack1").Pressed += () => ExecuteAttack("Basic Attack");
		GetNode<Button>("ActionsArea/Attack2").Pressed += () => ExecuteAttack("Skill A");
		GetNode<Button>("ActionsArea/Attack3").Pressed += () => ExecuteAttack("Skill B");
		GetNode<Button>("TopRightMenu/MenuButton").Pressed += () => _pauseMenu.Toggle();
		GetNode<Button>("%OkButton").Pressed += () => _navigation.NavigateToAsync("MainMenuPage");
		_retryButton.Pressed += () => RetryBattle();

		SetupBattle();
		await UpdateSprites();
	}

	private void SetupBattle()
	{
		if (_party.Count == 0 && _session.CurrentCharacter != null)
		{
			var pc = _session.CurrentCharacter;
			// Safety: Ensure player has health for the battle to function
			if (pc.MaxHP <= 0) pc.MaxHP = 100;
			if (pc.CurrentHP <= 0) pc.CurrentHP = pc.MaxHP;
			
			_party.Add(pc);
		}
        if (_enemies.Count == 0)
        {
			GD.PrintErr("No enemies provided in BattleArgs. Using default test enemies.");
            _enemies.Add(new Enemy { Name = "Hellhound Alpha", MaxHP = 60, CurrentHP = 60, Defense = 5, SpriteKey = "hound" });
            _enemies.Add(new Enemy { Name = "Hellhound Beta", MaxHP = 50, CurrentHP = 50, Defense = 5, SpriteKey = "hound" });
            _enemies.Add(new Enemy { Name = "Hellhound Gamma", MaxHP = 50, CurrentHP = 50, Defense = 5, SpriteKey = "hound" });

            foreach (var e in _enemies)
            {
                _originalEnemies.Add(new Enemy { Name = e.Name, MaxHP = e.MaxHP, CurrentHP = e.MaxHP, Defense = e.Defense, SpriteKey = e.SpriteKey });
            }
        }
	}

	private void RetryBattle()
	{
		_endBattlePanel.Hide();
		_enemies.Clear();
		_selectedEnemyIndex = 0; // Reset target
		foreach(var e in _originalEnemies)
		{
			_enemies.Add(new Enemy { Name = e.Name, MaxHP = e.MaxHP, CurrentHP = e.MaxHP, Defense = e.Defense, SpriteKey = e.SpriteKey });
		}
		
		if (_party.Count > 0)
		{
			_party[0].CurrentHP = _party[0].MaxHP;
		}
		
		_combatLog.Clear();
		_combatLog.AppendText("Retrying battle...");
		_ = UpdateSprites(); // Use discard for fire-and-forget task
	}

	private async Task UpdateSprites()
	{
		if (!IsInsideTree()) return;

		GD.Print($"[BattleScene] Updating combatants (P:{_party.Count} E:{_enemies.Count})");
		
		// Clear containers safely
		foreach (Node child in _partyContainer.GetChildren()) child.QueueFree();
		foreach (Node child in _enemyContainer.GetChildren()) child.QueueFree();
		
		_partySprites.Clear();
		_enemySprites.Clear();
		_partyHealthBars.Clear();
		_enemyHealthBars.Clear();

		// Ensure selection is valid
		if (_selectedEnemyIndex >= _enemies.Count && _enemies.Count > 0)
			_selectedEnemyIndex = _enemies.Count - 1;

		var layeredSpriteScene = GD.Load<PackedScene>("res://scenes/LayeredSprite.tscn");
		var statusBarScene = GD.Load<PackedScene>("res://scenes/StatusBar.tscn");

		foreach (var character in _party)
		{
			if (!IsInsideTree()) return;
			var wrapper = new VBoxContainer { CustomMinimumSize = new Vector2(200, 160) };
			wrapper.AddThemeConstantOverride("separation", -10); // Pull sprite up significantly
			_partyContainer.AddChild(wrapper);

			// Add Status Bar
			var hpBar = statusBarScene.Instantiate<StatusBar>();
			wrapper.AddChild(hpBar);
			hpBar.Setup(character.Name, character.CurrentHP, character.MaxHP, StatusType.HP);
			_partyHealthBars.Add(hpBar);

			// Sprite Area
			var spriteContainer = new Control { CustomMinimumSize = new Vector2(200, 120) };
			wrapper.AddChild(spriteContainer);
			
			var sprite = layeredSpriteScene.Instantiate<LayeredSprite>();
			spriteContainer.AddChild(sprite);
			// Since centered=false, (20, -10) centers the 160px sprite in 200px width and moves it up
			sprite.Position = new Vector2(20, -10); 
			sprite.Scale = new Vector2(2.5f, 2.5f);
			_partySprites.Add(sprite);

			await sprite.SetupCharacter(character, _catalog, _fileSystem);
			if (IsInsideTree()) sprite.Play("idle_right");
		}

		for (int i = 0; i < _enemies.Count; i++)
		{
			var enemy = _enemies[i];
			if (!IsInsideTree()) return;
			var wrapper = new VBoxContainer { CustomMinimumSize = new Vector2(200, 160) };
			wrapper.AddThemeConstantOverride("separation", -10);
			_enemyContainer.AddChild(wrapper);

			// Add Status Bar
			var hpBar = statusBarScene.Instantiate<StatusBar>();
			wrapper.AddChild(hpBar);
			hpBar.Setup(enemy.Name, enemy.CurrentHP, enemy.MaxHP, StatusType.HP);
			_enemyHealthBars.Add(hpBar);

			// Sprite Area
			var spriteContainer = new Control { CustomMinimumSize = new Vector2(200, 120) };
			wrapper.AddChild(spriteContainer);

			// Click/Touch targeting
			int index = i; // Local copy for closure
			spriteContainer.MouseFilter = Control.MouseFilterEnum.Stop;
			spriteContainer.GuiInput += (@event) => {
				if (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left)
					OnEnemyTapped(index);
				else if (@event is InputEventScreenTouch touch && touch.Pressed)
					OnEnemyTapped(index);
			};

			// Default selection highlight
			if (i == _selectedEnemyIndex && i < _enemyHealthBars.Count) 
				_enemyHealthBars[i].SetHighlighted(true);

			var sprite = layeredSpriteScene.Instantiate<LayeredSprite>();
			spriteContainer.AddChild(sprite);
			sprite.Position = new Vector2(30, -10);
			_enemySprites.Add(sprite);

			if (enemy.SpriteKey == "hound")
			{
				sprite.Scale = new Vector2(2.5f, 2.5f);
				await sprite.SetupMonster("hound", _fileSystem);
				if (IsInsideTree())
				{
					sprite.Play("idle");
					sprite.FlipH = false; 
				}
			}
			else if (enemy.SpriteKey.StartsWith("bosses/"))
			{
				sprite.Scale = new Vector2(2.5f, 2.5f);
				// Bosses are full LPC sheets located in assets/sprites/bosses/
				await sprite.SetupFullSheet("assets/sprites/" + enemy.SpriteKey, _fileSystem);
				if (IsInsideTree())
				{
					sprite.Play("idle_left");
					sprite.FlipH = false;
				}
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
				if (IsInsideTree()) sprite.Play("idle_left");
			}
		}
	}

	private async void ExecuteAttack(string skillType)
	{
		if (_isProcessingTurn || _enemies.Count == 0 || _party.Count == 0 || !IsInsideTree()) return;
		if (_partySprites.Count == 0 || _enemySprites.Count == 0) return;

		int actualIndex = _selectedEnemyIndex;
		if (actualIndex >= _enemies.Count) actualIndex = 0;
		
		var attacker = _party[0];
		var target = _enemies[actualIndex];
		
		if (target.CurrentHP <= 0 || attacker.CurrentHP <= 0) return;

		_isProcessingTurn = true;
		
		try 
		{
			var attackerSprite = _partySprites[0];
			var targetSprite = _enemySprites[actualIndex];

			if (!GodotObject.IsInstanceValid(attackerSprite) || !GodotObject.IsInstanceValid(targetSprite)) return;

			// Determine best animation
			string attackAnim = "walk_right";
			if (attackerSprite.HasAnimation("slash_right")) attackAnim = "slash_right";
			else if (attackerSprite.HasAnimation("spellcast_right")) attackAnim = "spellcast_right";
			else if (attackerSprite.HasAnimation("thrust_right")) attackAnim = "thrust_right";

			// Lunge Tween
			var originalPos = attackerSprite.Position;
			var lungePos = originalPos + new Vector2(50, 0);
			var tween = GetTree().CreateTween();
			tween.TweenProperty(attackerSprite, "position", lungePos, 0.15f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
			
			attackerSprite.Play(attackAnim);
			
			int damage = _combat.CalculateDamage(attacker, target);
			target.CurrentHP -= damage;
			if (actualIndex < _enemyHealthBars.Count)
				_enemyHealthBars[actualIndex].UpdateValue(target.CurrentHP, (int)target.MaxHP);

			_combatLog.AppendText($"\n[color=red]{attacker.Name}[/color] attacks {target.Name} with {skillType} for {damage} damage!");

			await ToSignal(tween, "finished");
			await ToSignal(GetTree().CreateTimer(0.2), "timeout");

			// Return Tween
			var returnTween = GetTree().CreateTween();
			returnTween.TweenProperty(attackerSprite, "position", originalPos, 0.15f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			await ToSignal(returnTween, "finished");

			if (!IsInsideTree()) return;
			if (GodotObject.IsInstanceValid(attackerSprite))
				attackerSprite.Play("idle_right");

			if (target.CurrentHP <= 0)
			{
				_combatLog.AppendText($"\n[color=gold]{target.Name} is defeated![/color]");
				
				// Apply morality impact
				if (target.MoralityImpact != 0 && _party.Count > 0)
				{
					_party[0].Morality += target.MoralityImpact;
					string color = target.MoralityImpact > 0 ? "cyan" : "purple";
					string sign = target.MoralityImpact > 0 ? "+" : "";
					_combatLog.AppendText($"\n[color={color}]Morality changed {sign}{target.MoralityImpact} (Total: {_party[0].Morality})[/color]");
				}
				
				_enemies.Remove(target);
				await UpdateSprites();
			}
			else
			{
				await ToSignal(GetTree().CreateTimer(0.3), "timeout");
				if (!IsInsideTree()) return;
				if (!GodotObject.IsInstanceValid(targetSprite) || !GodotObject.IsInstanceValid(attackerSprite)) return;
				
				// Enemy Counter-Attack
				string enemyAnim = "walk_left";
				if (target.SpriteKey == "hound") enemyAnim = "jump";
				else if (targetSprite.HasAnimation("slash_left")) enemyAnim = "slash_left";
				else if (targetSprite.HasAnimation("spellcast_left")) enemyAnim = "spellcast_left";

				// Enemy Lunge
				var eOriginalPos = targetSprite.Position;
				var eLungePos = eOriginalPos + new Vector2(-50, 0);
				var eTween = GetTree().CreateTween();
				eTween.TweenProperty(targetSprite, "position", eLungePos, 0.15f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

				targetSprite.Play(enemyAnim);

				int enemyDamage = 5; 
				attacker.CurrentHP -= enemyDamage;
				if (_partyHealthBars.Count > 0)
					_partyHealthBars[0].UpdateValue(attacker.CurrentHP, attacker.MaxHP);
				_combatLog.AppendText($"\n[color=orange]{target.Name}[/color] bites back for {enemyDamage} damage!");

				await ToSignal(eTween, "finished");
				await ToSignal(GetTree().CreateTimer(0.2), "timeout");

				// Enemy Return
				var eReturnTween = GetTree().CreateTween();
				eReturnTween.TweenProperty(targetSprite, "position", eOriginalPos, 0.15f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
				await ToSignal(eReturnTween, "finished");

				if (!IsInsideTree()) return;
				
				if (GodotObject.IsInstanceValid(targetSprite))
				{
					if (target.SpriteKey == "hound")
						targetSprite.Play("idle");
					else
						targetSprite.Play("idle_left");
				}

				if (attacker.CurrentHP <= 0)
				{
					Defeat();
					return;
				}
			}

			if (_enemies.Count == 0)
			{
				Victory();
			}
		}
		finally 
		{
			_isProcessingTurn = false;
		}
	}

	private void Victory()
	{
		_combatLog.AppendText("\n[color=yellow]VICTORY![/color]");
		_endBattleTitle.Text = "VICTORY ACHIEVED";
		_endBattleTitle.Set("theme_override_colors/font_color", Colors.Gold);
		_endBattleMessage.Text = "All enemies have been defeated!";
		_retryButton.Hide();
		_endBattlePanel.Show();
	}

	private void Defeat()
	{
		_combatLog.AppendText("\n[color=red]DEFEAT...[/color]");
		_endBattleTitle.Text = "BATTLE FAILED";
		_endBattleTitle.Set("theme_override_colors/font_color", Colors.Red);
		_endBattleMessage.Text = "Your party has been defeated in combat.";
		_retryButton.Show();
		_endBattlePanel.Show();
	}
}
