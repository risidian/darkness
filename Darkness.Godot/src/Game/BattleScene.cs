using Darkness.Core.Interfaces;
using Darkness.Core.Logic;
using Darkness.Core.Models;
using Darkness.Godot.Core;
using Darkness.Godot.UI;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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
    private ILevelingService _leveling = null!;
    private IQuestService _questService = null!;
    private RichTextLabel _combatLog = null!;
    private PauseMenu _pauseMenu = null!;
    private Control _endBattlePanel = null!;
    private Label _endBattleTitle = null!;
    private Label _endBattleMessage = null!;
    private Button _retryButton = null!;
    private Button _continueButton = null!;
    private Button _okButton = null!;
    private string? _questChainId;
    private string? _questStepId;

    private ItemList _turnOrderList = null!;

    private HBoxContainer _partyContainer = null!;
    private VBoxContainer _enemyContainer = null!;

    private TurnManager _turnManager = new();

    private List<Character> _party = new();
    private List<Enemy> _enemies = new();
    private List<Enemy> _originalEnemies = new(); // For retry
    private List<LayeredSprite> _partySprites = new();
    private List<LayeredSprite> _enemySprites = new();
    private List<StatusBar> _partyHealthBars = new();
    private List<StatusBar> _enemyHealthBars = new();
    private bool _isProcessingTurn = false;
    private int _selectedEnemyIndex = 0;

    private int _survivalTurns = 0;
    private ProgressBar? _survivalBar;
    private Label? _survivalLabel;

    private List<Skill> _currentWeaponSkills = new();

    public void Initialize(IDictionary<string, object> parameters)
    {
        if (parameters.ContainsKey("Args") && parameters["Args"] is BattleArgs args)
        {
            _questChainId = args.QuestChainId;
            _questStepId = args.QuestStepId;

            if (args.Combat != null)
            {
                var combat = args.Combat;
                _survivalTurns = combat.SurvivalTurns ?? 0;
                _enemies.Clear();
                _originalEnemies.Clear();
                var enemies = combat.Enemies.Select(e => new Enemy
                {
                    Name = e.Name,
                    Level = e.Level,
                    MaxHP = e.MaxHP,
                    CurrentHP = e.CurrentHP <= 0 ? e.MaxHP : e.CurrentHP,
                    Attack = e.Attack > 0 ? e.Attack : 10,
                    Defense = e.Defense > 0 ? e.Defense : 5,
                    Speed = e.Speed > 0 ? e.Speed : 10,
                    Accuracy = e.Accuracy > 0 ? e.Accuracy : 80,
                    Evasion = e.Evasion,
                    SpriteKey = e.SpriteKey ?? "knight",
                    IsInvincible = e.IsInvincible,
                    MoralityImpact = e.MoralityImpact,
                    ExperienceReward = e.ExperienceReward,
                    GoldReward = e.GoldReward
                }).ToList();

                foreach (var enemy in enemies)
                {
                    _enemies.Add(enemy);
                    _originalEnemies.Add(new Enemy
                    {
                        Name = enemy.Name,
                        Level = enemy.Level,
                        MaxHP = enemy.MaxHP,
                        CurrentHP = enemy.MaxHP,
                        Attack = enemy.Attack,
                        Defense = enemy.Defense,
                        Accuracy = enemy.Accuracy,
                        Speed = enemy.Speed,
                        Evasion = enemy.Evasion,
                        SpriteKey = enemy.SpriteKey,
                        IsInvincible = enemy.IsInvincible,
                        MoralityImpact = enemy.MoralityImpact,
                        ExperienceReward = enemy.ExperienceReward,
                        GoldReward = enemy.GoldReward
                    });
                }

                // Handle dynamic background
                if (!string.IsNullOrEmpty(combat.BackgroundKey))
                {
                    var texPath = $"res://assets/backgrounds/{combat.BackgroundKey}.png";
                    if (global::Godot.FileAccess.FileExists(texPath) || ResourceLoader.Exists(texPath))
                    {
                        var bgImage = GetNode<TextureRect>("BackgroundImage");
                        bgImage.Texture = GD.Load<Texture2D>(texPath);
                        bgImage.Show();
                        GetNode<ColorRect>("Background").Hide();
                    }
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
                    Attack = e.Attack > 0 ? e.Attack : 10,
                    Defense = e.Defense > 0 ? e.Defense : 5,
                    Accuracy = e.Accuracy > 0 ? e.Accuracy : 80,
                    SpriteKey = e.SpriteKey ?? "knight"
                };
                _enemies.Add(enemy);
                _originalEnemies.Add(new Enemy
                {
                    Name = enemy.Name,
                    MaxHP = enemy.MaxHP,
                    CurrentHP = enemy.MaxHP,
                    Attack = enemy.Attack,
                    Defense = enemy.Defense,
                    Accuracy = enemy.Accuracy,
                    SpriteKey = enemy.SpriteKey
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
        _weaponSkillService = sp.GetRequiredService<IWeaponSkillService>();
        _leveling = sp.GetRequiredService<ILevelingService>();
        _questService = sp.GetRequiredService<IQuestService>();

        _combatLog = GetNode<RichTextLabel>("CombatLog");
        
        // Add translucent background to make text visible against backgrounds
        var logStyleBox = new StyleBoxFlat
        {
            BgColor = new Color(0, 0, 0, 0.6f),
            CornerRadiusTopLeft = 8,
            CornerRadiusTopRight = 8,
            CornerRadiusBottomLeft = 8,
            CornerRadiusBottomRight = 8,
            ContentMarginLeft = 10,
            ContentMarginTop = 10,
            ContentMarginRight = 10,
            ContentMarginBottom = 10
        };
        _combatLog.AddThemeStyleboxOverride("normal", logStyleBox);
        
        _pauseMenu = GetNode<PauseMenu>("PauseMenu");
        _endBattlePanel = GetNode<Control>("EndBattlePanel");
        _endBattleTitle = GetNode<Label>("%Title");
        _endBattleMessage = GetNode<Label>("%Message");
        _retryButton = GetNode<Button>("%RetryButton");
        _continueButton = GetNode<Button>("%ContinueButton");
        _okButton = GetNode<Button>("%OkButton");

        var combatArea = GetNode<Control>("CombatArea");
        _partyContainer = combatArea.GetNode<HBoxContainer>("PartyContainer");
        _enemyContainer = combatArea.GetNode<VBoxContainer>("EnemyContainer");

        _turnOrderList = new ItemList();
        _turnOrderList.Name = "TurnOrderList";
        _turnOrderList.CustomMinimumSize = new Vector2(200, 200);
        AddChild(_turnOrderList);
        _turnOrderList.SetPosition(new Vector2(20, 150));

        GetNode<Button>("TopRightMenu/MenuButton").Pressed += () => _pauseMenu.Toggle();
        GetNode<Button>("TopLeftMenu/InventoryButton").Pressed += () => _navigation.NavigateToAsync("InventoryPage");

        _okButton.Pressed += async () =>
        {
            await _navigation.NavigateToAsync("MainMenuPage");
        };

        _continueButton.Pressed += async () =>
        {
            // Navigate back to world with a safe start position to avoid trigger loops
            var parameters = new Dictionary<string, object>
            {
                { "PlayerPosition", new Vector2(200, 300) }
            };
            await _navigation.NavigateToAsync("WorldScene", parameters);
        };

        _retryButton.Pressed += () => RetryBattle();

        if (_survivalTurns > 0)
        {
            var survivalContainer = new VBoxContainer
            {
                CustomMinimumSize = new Vector2(300, 50),
                Position = new Vector2(GetViewportRect().Size.X / 2 - 150, 20),
                MouseFilter = MouseFilterEnum.Ignore
            };

            _survivalLabel = new Label
            {
                Text = $"Survive! (0/{_survivalTurns} Turns)",
                HorizontalAlignment = HorizontalAlignment.Center
            };
            
            _survivalBar = new ProgressBar
            {
                MinValue = 0,
                MaxValue = _survivalTurns,
                Value = 0,
                CustomMinimumSize = new Vector2(300, 20),
                ShowPercentage = false
            };

            survivalContainer.AddChild(_survivalLabel);
            survivalContainer.AddChild(_survivalBar);
            AddChild(survivalContainer);
        }

        // Wait a single frame to ensure GodotNavigationService has called Initialize()
        await ToSignal(GetTree(), "process_frame");

        SetupBattle();
        SetupWeaponSkills();
        await UpdateSprites();
        ProcessNextTurn();
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
            GD.PrintErr("[BattleScene] No enemies provided in parameters, using default Hellhound encounter.");
            _enemies.Add(new Enemy
            {
                Name = "Hellhound Alpha", MaxHP = 60, CurrentHP = 60, Defense = 5, Attack = 10, Accuracy = 100,
                SpriteKey = "hound"
            });
            _enemies.Add(new Enemy
            {
                Name = "Hellhound Beta", MaxHP = 50, CurrentHP = 50, Defense = 5, Attack = 8, Accuracy = 80,
                SpriteKey = "hound"
            });
            _enemies.Add(new Enemy
            {
                Name = "Hellhound Gamma", MaxHP = 50, CurrentHP = 50, Defense = 5, Attack = 8, Accuracy = 80,
                SpriteKey = "hound"
            });
        }

        // Ensure _originalEnemies is populated once if empty (whether from params or defaults)
        if (_originalEnemies.Count == 0)
        {
            foreach (var e in _enemies)
            {
                _originalEnemies.Add(new Enemy
                {
                    Name = e.Name,
                    Level = e.Level,
                    MaxHP = e.MaxHP,
                    CurrentHP = e.MaxHP,
                    Attack = e.Attack,
                    Defense = e.Defense,
                    Accuracy = e.Accuracy,
                    Speed = e.Speed,
                    Evasion = e.Evasion,
                    SpriteKey = e.SpriteKey,
                    IsInvincible = e.IsInvincible,
                    MoralityImpact = e.MoralityImpact,
                    ExperienceReward = e.ExperienceReward,
                    GoldReward = e.GoldReward
                });
            }
        }

        _turnManager.Setup(_party, _enemies, (CombatEngine)_combat);
        UpdateTurnOrderUI();
    }

    private void SetupWeaponSkills()
    {
        if (_session.CurrentCharacter == null) return;
        var character = _session.CurrentCharacter;

        _currentWeaponSkills =
            _weaponSkillService.GetSkillsForWeapon(character.WeaponType ?? "None", character.ShieldType ?? "None");

        var buttons = new[]
        {
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

    private async void RetryBattle()
    {
        _endBattlePanel.Hide();
        _enemies.Clear();
        foreach (var e in _originalEnemies)
        {
            _enemies.Add(new Enemy
            {
                Name = e.Name,
                MaxHP = e.MaxHP,
                CurrentHP = e.MaxHP,
                Attack = e.Attack,
                Defense = e.Defense,
                Accuracy = e.Accuracy,
                Speed = e.Speed,
                SpriteKey = e.SpriteKey,
                IsInvincible = e.IsInvincible,
                MoralityImpact = e.MoralityImpact,
                ExperienceReward = e.ExperienceReward,
                GoldReward = e.GoldReward
            });
        }

        if (_party.Count > 0)
        {
            _party[0].CurrentHP = _party[0].MaxHP;
            _party[0].IsBlocking = false;
        }

        if (_survivalBar != null) _survivalBar.Value = 0;
        if (_survivalLabel != null) _survivalLabel.Text = $"Survive! (0/{_survivalTurns} Turns)";

        _combatLog.Clear();
        _combatLog.AppendText("Retrying battle...");
        _selectedEnemyIndex = 0;
        
        _turnManager.Setup(_party, _enemies, (CombatEngine)_combat);
        UpdateTurnOrderUI();
        SetupWeaponSkills();
        
        await UpdateSprites();
        ProcessNextTurn();
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

            await sprite.SetupCharacter(character, _catalog, _fileSystem, _compositor);
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
            spriteContainer.GuiInput += (@event) =>
            {
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
                await sprite.SetupCharacter(new Character
                {
                    SkinColor = knightAppearance.SkinColor,
                    Head = "Human Male",
                    ArmorType = knightAppearance.ArmorType,
                    WeaponType = knightAppearance.WeaponType,
                    Legs = knightAppearance.Legs,
                    Feet = knightAppearance.Feet
                }, _catalog, _fileSystem, _compositor);
                sprite.Play("idle_left");
            }

            if (i == _selectedEnemyIndex) hpBar.SetHighlighted(true);
        }
    }

    private void UpdateTurnOrderUI()
    {
        _turnOrderList.Clear();
        for (int i = 0; i < _turnManager.TurnOrder.Count; i++)
        {
            var entity = _turnManager.TurnOrder[i];
            string name = entity is Character c ? c.Name : ((Enemy)entity).Name;
            _turnOrderList.AddItem(name);
            if (i == _turnManager.CurrentTurnIndex)
            {
                _turnOrderList.SetItemCustomBgColor(i, new Color(0, 0.5f, 0, 0.5f));
            }
        }
    }

    private async void ProcessNextTurn()
    {
        if (!IsInsideTree() || _party.Count == 0 || _enemies.Count == 0 || _turnManager.TurnOrder.Count == 0) return;

        if (_survivalTurns > 0)
        {
            int currentSurvivalTurn = _turnManager.CurrentRound - 1;
            if (_survivalBar != null && _survivalLabel != null)
            {
                _survivalBar.Value = currentSurvivalTurn;
                _survivalLabel.Text = $"Survive! ({currentSurvivalTurn}/{_survivalTurns} Turns)";
            }

            if (currentSurvivalTurn >= _survivalTurns)
            {
                Victory(survived: true);
                return;
            }
        }

        UpdateTurnOrderUI();
        var currentEntity = _turnManager.CurrentEntity;

        if (currentEntity is Character character)
        {
            EnablePlayerInput(true);
            _combatLog.AppendText($"\n\n--- [color=cyan]{character.Name}'s Turn[/color] ---");
            _combatLog.AppendText("\n[color=gray]Select an action and target![/color]");
        }
        else if (currentEntity is Enemy enemy)
        {
            EnablePlayerInput(false);
            _combatLog.AppendText($"\n\n--- [color=orange]{enemy.Name}'s Turn[/color] ---");
            await ExecuteEnemyTurn(enemy);
        }
    }

    private void EnablePlayerInput(bool enable)
    {
        var buttons = new[]
        {
            GetNode<Button>("ActionsArea/Attack1"),
            GetNode<Button>("ActionsArea/Attack2"),
            GetNode<Button>("ActionsArea/Attack3")
        };
        foreach (var btn in buttons)
        {
            btn.Disabled = !enable;
        }
    }

    private async Task ExecuteEnemyTurn(Enemy target)
    {
        _isProcessingTurn = true;
        try
        {
            var attacker = _party[0]; // Player is the only valid target currently
            if (attacker.CurrentHP <= 0) return;

            int actualIndex = _enemies.IndexOf(target);
            if (actualIndex < 0)
            {
                _turnManager.NextTurn();
                ProcessNextTurn();
                return;
            }

            var targetSprite = _enemySprites[actualIndex];

            await ToSignal(GetTree().CreateTimer(0.5), "timeout");

            string enemyAnim = target.SpriteKey == "hound"
                ? "jump"
                : (targetSprite.HasAnimation("slash_left") ? "slash_left" : "walk_left");
            var eOriginalPos = targetSprite.Position;
            var eLungePos = eOriginalPos + new Vector2(-50, 0);
            var eTween = GetTree().CreateTween();
            eTween.TweenProperty(targetSprite, "position", eLungePos, 0.15f).SetTrans(Tween.TransitionType.Quad)
                .SetEase(Tween.EaseType.Out);

            targetSprite.Play(enemyAnim);

            var enemyCombatResult = _combat.CalculateDamage(target, attacker);
            
            if (enemyCombatResult.IsHit)
            {
                attacker.CurrentHP -= enemyCombatResult.DamageDealt;
                _partyHealthBars[0].UpdateValue(attacker.CurrentHP, attacker.MaxHP);

                string defendMsg = attacker.IsBlocking ? " (Blocked!)" : "";
                string eCritMsg = enemyCombatResult.IsCriticalHit ? "[color=yellow]CRITICAL HIT! [/color]" : "";
                _combatLog.AppendText($"\n{eCritMsg}[color=orange]{target.Name}[/color] attacks for {enemyCombatResult.DamageDealt} damage!{defendMsg}");
            }
            else
            {
                string eMissMsg = enemyCombatResult.IsCriticalMiss ? "[color=gray]CRITICAL MISS! [/color]" : "[color=gray]Miss! [/color]";
                _combatLog.AppendText($"\n{eMissMsg}[color=orange]{target.Name}[/color] attacks but misses!");
            }

            await ToSignal(eTween, "finished");
            await ToSignal(GetTree().CreateTimer(0.2), "timeout");

            var eReturnTween = GetTree().CreateTween();
            eReturnTween.TweenProperty(targetSprite, "position", eOriginalPos, 0.15f)
                .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
            await ToSignal(eReturnTween, "finished");
            targetSprite.Play(target.SpriteKey == "hound" ? "idle" : "idle_left");

            if (attacker.CurrentHP <= 0)
            {
                Defeat();
                return;
            }
            
            _turnManager.NextTurn();
            ProcessNextTurn();
        }
        finally
        {
            _isProcessingTurn = false;
        }
    }

    private async void ExecuteWeaponSkill(Skill skill)
    {
        if (_isProcessingTurn || _enemies.Count == 0 || _party.Count == 0 || !IsInsideTree()) return;

        var currentEntity = _turnManager.CurrentEntity;
        if (!(currentEntity is Character attacker)) return;

        int actualIndex = _selectedEnemyIndex < _enemies.Count ? _selectedEnemyIndex : 0;
        var target = _enemies[actualIndex];

        if (target.CurrentHP <= 0 || attacker.CurrentHP <= 0) return;

        _isProcessingTurn = true;
        EnablePlayerInput(false);
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
                string attackAnim = skill.AssociatedAction switch
                {
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
                tween.TweenProperty(attackerSprite, "position", lungePos, 0.15f).SetTrans(Tween.TransitionType.Quad)
                    .SetEase(Tween.EaseType.Out);

                attackerSprite.Play(attackAnim);

                var combatResult = _combat.CalculateDamage(attacker, target, skill: skill);
                
                if (combatResult.IsHit)
                {
                    target.CurrentHP -= combatResult.DamageDealt;
                    _enemyHealthBars[actualIndex].UpdateValue(target.CurrentHP, (int)target.MaxHP);
                    
                    string critMsg = combatResult.IsCriticalHit ? "[color=yellow]CRITICAL HIT! [/color]" : "";
                    _combatLog.AppendText($"\n{critMsg}[color=red]{attacker.Name}[/color] uses [b]{skill.Name}[/b] on {target.Name} for {combatResult.DamageDealt} damage! Rolled {combatResult.D20Roll}");
                }
                else
                {
                    string missMsg = combatResult.IsCriticalMiss ? "[color=gray]CRITICAL MISS! [/color]" : "[color=gray]Miss! [/color]";
                    _combatLog.AppendText($"\n{missMsg}[color=red]{attacker.Name}[/color] tried to use [b]{skill.Name}[/b] on {target.Name} but missed! Rolled { combatResult.D20Roll}");
                }

                await ToSignal(tween, "finished");
                await ToSignal(GetTree().CreateTimer(0.2), "timeout");

                var returnTween = GetTree().CreateTween();
                returnTween.TweenProperty(attackerSprite, "position", originalPos, 0.15f)
                    .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
                await ToSignal(returnTween, "finished");
                attackerSprite.Play("idle_right");
            }

            if (target.CurrentHP <= 0)
            {
                _combatLog.AppendText($"\n[color=gold]{target.Name} is defeated![/color]");
                if (target.MoralityImpact != 0) _party[0].Morality += target.MoralityImpact;
                
                _turnManager.RemoveEntity(target);
                
                _enemies.Remove(target);
                _selectedEnemyIndex = 0;
                await UpdateSprites();
            }

            if (_enemies.Count == 0)
            {
                Victory();
                return;
            }
            
            _turnManager.NextTurn();
            ProcessNextTurn();
        }
        finally
        {
            _isProcessingTurn = false;
        }
    }

    private void Victory(bool survived = false)
    {
        _combatLog.AppendText("\n[color=yellow]VICTORY![/color]");
        _endBattleTitle.Text = "VICTORY ACHIEVED";
        _endBattleTitle.Set("theme_override_colors/font_color", Colors.Gold);

        var character = _session.CurrentCharacter;
        int totalXp = _originalEnemies.Sum(e => e.ExperienceReward);
        string victoryMsg = survived ? $"You survived {_survivalTurns} turns!" : "All enemies have been defeated!";

        if (character != null && totalXp > 0)
        {
            var result = _leveling.AwardExperience(character, totalXp);
            victoryMsg += $"\n+{result.XpAwarded} XP";
            if (result.DidLevelUp)
                victoryMsg += $"\nLevel Up! You are now level {result.NewLevel}!";
        }
        
        if (character != null && _questChainId != null)
        {
            GD.Print($"[BattleScene] Victory! Advancing quest chain: {_questChainId} for Char {character.Id}");
            _questService.AdvanceStep(character, _questChainId);
        }

        _endBattleMessage.Text = victoryMsg;
        
        _retryButton.Hide();
        _continueButton.Show();
        _okButton.Show();
        
        _endBattlePanel.Show();
    }

    private void Defeat()
    {
        _combatLog.AppendText("\n[color=red]DEFEAT...[/color]");
        _endBattleTitle.Text = "BATTLE FAILED";
        _endBattleTitle.Set("theme_override_colors/font_color", Colors.Red);
        _endBattleMessage.Text = "Your party has been defeated in combat.";
        
        _retryButton.Show();
        _continueButton.Hide();
        _okButton.Hide();
        
        _endBattlePanel.Show();
    }
}
