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
    private IWeaponSkillService _weaponSkillService = null!;
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

    private List<Skill> _currentWeaponSkills = new();

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
						SpriteKey = e.SpriteKey ?? "knight",
						MoralityImpact = e.MoralityImpact
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
        _weaponSkillService = sp.GetRequiredService<IWeaponSkillService>();

		_combatLog = GetNode<RichTextLabel>("CombatLog");
		_pauseMenu = GetNode<PauseMenu>("PauseMenu");
		_endBattlePanel = GetNode<Control>("EndBattlePanel");
		_endBattleTitle = GetNode<Label>("%Title");
		_endBattleMessage = GetNode<Label>("%Message");
		_retryButton = GetNode<Button>("%RetryButton");
		
		_partyContainer = GetNode<HBoxContainer>("CombatArea/PartyContainer");
		_enemyContainer = GetNode<VBoxContainer>("CombatArea/EnemyContainer");

		GetNode<Button>("TopRightMenu/MenuButton").Pressed += () => _pauseMenu.Toggle();
		GetNode<Button>("%OkButton").Pressed += () => _navigation.NavigateToAsync("MainMenuPage");
		_retryButton.Pressed += () => RetryBattle();

		SetupBattle();
        SetupWeaponSkills();
		await UpdateSprites();
	}

    private void SetupWeaponSkills()
    {
        if (_session.CurrentCharacter == null) return;
        var character = _session.CurrentCharacter;
        
        _currentWeaponSkills = _weaponSkillService.GetSkillsForWeapon(character.WeaponType ?? "None", character.ShieldType ?? "None");
        
        var buttons = new[] {
            GetNode<Button>("ActionsArea/Attack1"),
            GetNode<Button>("ActionsArea/Attack2"),
            GetNode<Button>("ActionsArea/Attack3")
        };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (i < _currentWeaponSkills.Count)
            {
                var skill = _currentWeaponSkills[i];
                buttons[i].Text = skill.Name.ToUpper();
                buttons[i].TooltipText = skill.Description;
                
                // Clear existing connections if any (though usually fine on _Ready)
                foreach (var connection in buttons[i].GetSignalConnectionList("pressed"))
                    buttons[i].Disconnect("pressed", connection["callable"].AsCallable());
                
                buttons[i].Pressed += () => ExecuteWeaponSkill(skill);
                buttons[i].Show();
            }
            else
            {
                buttons[i].Hide();
            }
        }
    }

	private void SetupBattle()
	{
		if (_party.Count == 0 && _session.CurrentCharacter != null)
		{
			var pc = _session.CurrentCharacter;
			if (pc.MaxHP <= 0) pc.MaxHP = 100;
			if (pc.CurrentHP <= 0) pc.CurrentHP = pc.MaxHP;
			pc.IsBlocking = false;
			_party.Add(pc);
		}

		if (_enemies.Count == 0)
		{
			_enemies.Add(new Enemy { Name = "Hellhound Alpha", MaxHP = 60, CurrentHP = 60, Defense = 5, Attack = 10,Accuracy = 100, SpriteKey = "hound" });
			_enemies.Add(new Enemy { Name = "Hellhound Beta", MaxHP = 50, CurrentHP = 50, Defense = 5,Attack = 8, Accuracy = 80, SpriteKey = "hound" });
			_enemies.Add(new Enemy { Name = "Hellhound Gamma", MaxHP = 50, CurrentHP = 50, Defense = 5,Attack = 8, Accuracy = 80, SpriteKey = "hound" });
			
			foreach(var e in _enemies)
			{
				_originalEnemies.Add(new Enemy { Name = e.Name, MaxHP = e.MaxHP, CurrentHP = e.MaxHP, Defense = e.Defense, SpriteKey = e.SpriteKey });
			}
		}
	}

	private void RetryBattle()
	{
		_endBattlePanel.Hide();
		_enemies.Clear();
		foreach(var e in _originalEnemies)
		{
			_enemies.Add(new Enemy { Name = e.Name, MaxHP = e.MaxHP, CurrentHP = e.MaxHP, Defense = e.Defense, SpriteKey = e.SpriteKey });
		}
		
		if (_party.Count > 0)
		{
			_party[0].CurrentHP = _party[0].MaxHP;
			_party[0].IsBlocking = false;
		}
		
		_combatLog.Clear();
		_combatLog.AppendText("Retrying battle...");
		_selectedEnemyIndex = 0;
		_ = UpdateSprites();
	}

	private void OnEnemyTapped(int index)
	{
		if (index >= _enemies.Count) return;
		
		if (_selectedEnemyIndex < _enemyHealthBars.Count)
			_enemyHealthBars[_selectedEnemyIndex].SetHighlighted(false);
			
		_selectedEnemyIndex = index;
		
		if (_selectedEnemyIndex < _enemyHealthBars.Count)
			_enemyHealthBars[_selectedEnemyIndex].SetHighlighted(true);
			
		GD.Print($"[Battle] Target selected: {_enemies[_selectedEnemyIndex].Name}");
	}

	private async Task UpdateSprites()
	{
		if (!IsInsideTree()) return;

		foreach (Node child in _partyContainer.GetChildren()) child.QueueFree();
		foreach (Node child in _enemyContainer.GetChildren()) child.QueueFree();
		
		_partySprites.Clear();
		_enemySprites.Clear();
		_partyHealthBars.Clear();
		_enemyHealthBars.Clear();

		var layeredSpriteScene = GD.Load<PackedScene>("res://scenes/LayeredSprite.tscn");
		var statusBarScene = GD.Load<PackedScene>("res://scenes/StatusBar.tscn");

		foreach (var character in _party)
		{
			var wrapper = new VBoxContainer { CustomMinimumSize = new Vector2(200, 160) };
			wrapper.AddThemeConstantOverride("separation", -30);
			_partyContainer.AddChild(wrapper);

			var hpBar = statusBarScene.Instantiate<StatusBar>();
			wrapper.AddChild(hpBar);
			hpBar.Setup(character.Name, character.CurrentHP, character.MaxHP, StatusType.HP);
			_partyHealthBars.Add(hpBar);

			var spriteContainer = new Control { CustomMinimumSize = new Vector2(200, 120) };
			wrapper.AddChild(spriteContainer);
			
			var sprite = layeredSpriteScene.Instantiate<LayeredSprite>();
			spriteContainer.AddChild(sprite);
			sprite.Position = new Vector2(20, -10); 
			sprite.Scale = new Vector2(2.5f, 2.5f);
			_partySprites.Add(sprite);

			await sprite.SetupCharacter(character, _catalog, _fileSystem);
			sprite.Play("idle_right");
		}

		for (int i = 0; i < _enemies.Count; i++)
		{
			var enemy = _enemies[i];
			var wrapper = new VBoxContainer { CustomMinimumSize = new Vector2(200, 160) };
			wrapper.AddThemeConstantOverride("separation", -30);
			_enemyContainer.AddChild(wrapper);

			var hpBar = statusBarScene.Instantiate<StatusBar>();
			wrapper.AddChild(hpBar);
			hpBar.Setup(enemy.Name, enemy.CurrentHP, enemy.MaxHP, StatusType.HP);
			_enemyHealthBars.Add(hpBar);

			var spriteContainer = new Control { CustomMinimumSize = new Vector2(200, 120) };
			spriteContainer.MouseFilter = Control.MouseFilterEnum.Stop;
			int index = i;
			spriteContainer.GuiInput += (@event) => {
				if ((@event is InputEventMouseButton m && m.Pressed && m.ButtonIndex == MouseButton.Left) ||
				    (@event is InputEventScreenTouch t && t.Pressed))
					OnEnemyTapped(index);
			};
			wrapper.AddChild(spriteContainer);

			var sprite = layeredSpriteScene.Instantiate<LayeredSprite>();
			spriteContainer.AddChild(sprite);
			sprite.Position = new Vector2(20, -10);
			_enemySprites.Add(sprite);

			if (enemy.SpriteKey == "hound")
			{
				sprite.Scale = new Vector2(2.5f, 2.5f);
				await sprite.SetupMonster("hound", _fileSystem);
				sprite.Play("idle");
			}
			else if (enemy.SpriteKey.StartsWith("bosses/"))
			{
				sprite.Scale = new Vector2(2.5f, 2.5f);
				await sprite.SetupFullSheet("assets/sprites/" + enemy.SpriteKey + ".png", _fileSystem);
				sprite.Play("idle_left");
			}
			else
			{
				sprite.Scale = new Vector2(2.5f, 2.5f);
				var knightAppearance = _catalog.GetDefaultAppearanceForClass("Knight");
				await sprite.SetupCharacter(new Character {
					SkinColor = knightAppearance.SkinColor,
					Head = "Human Male",
					ArmorType = knightAppearance.ArmorType,
					WeaponType = knightAppearance.WeaponType,
					Legs = knightAppearance.Legs,
					Feet = knightAppearance.Feet
				}, _catalog, _fileSystem);
				sprite.Play("idle_left");
			}

			if (i == _selectedEnemyIndex) hpBar.SetHighlighted(true);
		}
	}

	private async void ExecuteWeaponSkill(Skill skill)
	{
		if (_isProcessingTurn || _enemies.Count == 0 || _party.Count == 0 || !IsInsideTree()) return;
		
		int actualIndex = _selectedEnemyIndex < _enemies.Count ? _selectedEnemyIndex : 0;
		var attacker = _party[0];
		var target = _enemies[actualIndex];
		
		if (target.CurrentHP <= 0 || attacker.CurrentHP <= 0) return;

		_isProcessingTurn = true;
		attacker.IsBlocking = false;

		try 
		{
			var attackerSprite = _partySprites[0];
			var targetSprite = _enemySprites[actualIndex];

			if (skill.SkillType == "Defensive")
			{
				attacker.IsBlocking = true;
				_combatLog.AppendText($"\n[color=cyan]{attacker.Name}[/color] uses [b]{skill.Name}[/b]!");
				_combatLog.AppendText($"\n{skill.Description}");
				await ToSignal(GetTree().CreateTimer(0.5), "timeout");
			}
			else
			{
				// Attack Logic
				string attackAnim = skill.AssociatedAction switch {
                    ActionType.Slash => "slash_right",
                    ActionType.Thrust => "thrust_right",
                    ActionType.Cast => "spellcast_right",
                    ActionType.Shoot => "bow_right",
                    _ => "walk_right"
                };
                
				if (!attackerSprite.HasAnimation(attackAnim)) 
                {
                    if (skill.SkillType == "Magical") attackAnim = "spellcast_right";
                    else attackAnim = "walk_right";
                }

				var originalPos = attackerSprite.Position;
				var lungePos = originalPos + new Vector2(50, 0);
				var tween = GetTree().CreateTween();
				tween.TweenProperty(attackerSprite, "position", lungePos, 0.15f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
				
				attackerSprite.Play(attackAnim);
				
				int damage = _combat.CalculateDamage(attacker, target, skill: skill);
				target.CurrentHP -= damage;
				_enemyHealthBars[actualIndex].UpdateValue(target.CurrentHP, (int)target.MaxHP);

				_combatLog.AppendText($"\n[color=red]{attacker.Name}[/color] uses [b]{skill.Name}[/b] on {target.Name} for {damage} damage!");

				await ToSignal(tween, "finished");
				await ToSignal(GetTree().CreateTimer(0.2), "timeout");

				var returnTween = GetTree().CreateTween();
				returnTween.TweenProperty(attackerSprite, "position", originalPos, 0.15f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
				await ToSignal(returnTween, "finished");
				attackerSprite.Play("idle_right");
			}

			if (target.CurrentHP <= 0)
			{
				_combatLog.AppendText($"\n[color=gold]{target.Name} is defeated![/color]");
				if (target.MoralityImpact != 0) _party[0].Morality += target.MoralityImpact;
				_enemies.Remove(target);
				_selectedEnemyIndex = 0;
				await UpdateSprites();
			}
			else if (attacker.CurrentHP > 0)
			{
				// Enemy Turn
				await ToSignal(GetTree().CreateTimer(0.3), "timeout");
				
				string enemyAnim = target.SpriteKey == "hound" ? "jump" : (targetSprite.HasAnimation("slash_left") ? "slash_left" : "walk_left");
				var eOriginalPos = targetSprite.Position;
				var eLungePos = eOriginalPos + new Vector2(-50, 0);
				var eTween = GetTree().CreateTween();
				eTween.TweenProperty(targetSprite, "position", eLungePos, 0.15f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);

				targetSprite.Play(enemyAnim);

				int enemyDamage = _combat.CalculateDamage(target, attacker);
				attacker.CurrentHP -= enemyDamage;
				_partyHealthBars[0].UpdateValue(attacker.CurrentHP, attacker.MaxHP);
				
				string defendMsg = attacker.IsBlocking ? " (Blocked!)" : "";
				_combatLog.AppendText($"\n[color=orange]{target.Name}[/color] attacks for {enemyDamage} damage!{defendMsg}");

				await ToSignal(eTween, "finished");
				await ToSignal(GetTree().CreateTimer(0.2), "timeout");

				var eReturnTween = GetTree().CreateTween();
				eReturnTween.TweenProperty(targetSprite, "position", eOriginalPos, 0.15f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
				await ToSignal(eReturnTween, "finished");
				targetSprite.Play(target.SpriteKey == "hound" ? "idle" : "idle_left");

				if (attacker.CurrentHP <= 0)
				{
					Defeat();
					return;
				}
			}

			if (_enemies.Count == 0) Victory();
		}
		finally 
		{
			_isProcessingTurn = false;
			attacker.IsBlocking = false; 
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
