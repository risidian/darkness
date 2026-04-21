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
    private ISheetDefinitionCatalog _catalog = null!;
    private IFileSystemService _fileSystem = null!;
    private IWeaponSkillService _weaponSkillService = null!;
    private ILevelingService _leveling = null!;
    private IQuestService _questService = null!;
    private IRewardService _rewardService = null!;
    private ICharacterService _characterService = null!;
    private RichTextLabel _combatLog = null!;
    private PauseMenu _pauseMenu = null!;
    private Control _endBattlePanel = null!;
    private Label _endBattleTitle = null!;
    private Label _endBattleMessage = null!;
    private Button _retryButton = null!;
    private Button _continueButton = null!;
    private Button _okButton = null!;
    private Tooltip _tooltip = null!;
    private string? _questChainId;
    private string? _questStepId;
    private Vector2? _returnPosition;
    private BattleArgs? _battleArgs;

    private ItemList _turnOrderList = null!;

    private HBoxContainer _partyContainer = null!;
    private VBoxContainer _enemyContainer = null!;
    private HBoxContainer _hotbarContainer = null!;

    private TurnManager _turnManager = new();

    private List<Character> _party = new();
    private List<Enemy> _enemies = new();
    private Dictionary<Enemy, EnemySpawn> _enemyMap = new();
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

    private Dictionary<int, int> _skillCooldowns = new();

    private List<Skill> _currentWeaponSkills = new();
    private string? _backgroundKey;

    public void Initialize(IDictionary<string, object> parameters)
    {
        if (parameters.ContainsKey("Args") && parameters["Args"] is BattleArgs args)
        {
            _battleArgs = args;
            _questChainId = args.QuestChainId;
            _questStepId = args.QuestStepId;
            _returnPosition = new Vector2(args.ReturnPositionX, args.ReturnPositionY);

            if (args.Combat != null)
            {
                var combat = args.Combat;
                _survivalTurns = combat.SurvivalTurns ?? 0;
                _backgroundKey = combat.BackgroundKey;
                _enemies.Clear();
                _enemyMap.Clear();
                _originalEnemies.Clear();
                
                foreach (var e in combat.Enemies)
                {
                    // Start of battle: e.CurrentHP is usually 0 in JSON, so we use MaxHP.
                    // Resuming: args.IsResuming will be true, so we use e.CurrentHP (even if 0).
                    int initialHP = e.CurrentHP;
                    if (initialHP <= 0 && !args.IsResuming) initialHP = e.MaxHP;

                    var enemy = new Enemy
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
                        GoldReward = e.GoldReward
                    };

                    _enemies.Add(enemy);
                    _enemyMap[enemy] = e;
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
                
                // Remove dead enemies if we are resuming
                if (args.IsResuming)
                {
                    // If no snapshot was passed in memory, try to load from database
                    if (args.Snapshot == null && _session.CurrentCharacter != null && !string.IsNullOrEmpty(_questChainId))
                    {
                        var state = _questService.GetQuestState(_session.CurrentCharacter.Id, _questChainId);
                        if (state != null && state.CurrentCombatSnapshot != null)
                        {
                            GD.Print($"[BattleScene] Restoring combat snapshot from database for chain: {_questChainId}");
                            args.Snapshot = state.CurrentCombatSnapshot;
                            
                            // Restore enemy HP from snapshot
                            for (int i = 0; i < args.Combat.Enemies.Count; i++)
                            {
                                if (args.Snapshot.EnemyHP.TryGetValue(i, out int hp))
                                {
                                    args.Combat.Enemies[i].CurrentHP = hp;
                                }
                            }
                        }
                    }

                    _enemies.RemoveAll(e => e.CurrentHP <= 0);
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
                    CurrentHP = e.CurrentHP > 0 ? e.CurrentHP : e.MaxHP,
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
        _catalog = sp.GetRequiredService<ISheetDefinitionCatalog>();
        _fileSystem = sp.GetRequiredService<IFileSystemService>();
        _weaponSkillService = sp.GetRequiredService<IWeaponSkillService>();
        _leveling = sp.GetRequiredService<ILevelingService>();
        _questService = sp.GetRequiredService<IQuestService>();
        _rewardService = sp.GetRequiredService<IRewardService>();
        _characterService = sp.GetRequiredService<ICharacterService>();

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
        _hotbarContainer = GetNode<HBoxContainer>("HotbarContainer");

        _turnOrderList = new ItemList();
        _turnOrderList.Name = "TurnOrderList";
        _turnOrderList.CustomMinimumSize = new Vector2(200, 200);
        AddChild(_turnOrderList);
        _turnOrderList.SetPosition(new Vector2(20, 150));

        GetNode<Button>("TopRightMenu/MenuButton").Pressed += () => _pauseMenu.Toggle();
        GetNode<Button>("TopLeftMenu/InventoryButton").Pressed += () => 
        {
            SyncCombatState();
            _navigation.NavigateToAsync("InventoryPage");
        };

        _okButton.Pressed += async () =>
        {
            await _navigation.NavigateToAsync("MainMenuPage");
        };

        _continueButton.Pressed += async () =>
        {
            // Navigate back to world with the passed return position or a safe default
            var parameters = new Dictionary<string, object>
            {
                { "PlayerPosition", _returnPosition ?? new Vector2(200, 300) }
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

        // Apply background if available
        if (!string.IsNullOrEmpty(_backgroundKey))
        {
            var texPath = $"res://assets/backgrounds/{_backgroundKey}.png";
            if (global::Godot.FileAccess.FileExists(texPath) || ResourceLoader.Exists(texPath))
            {
                GD.Print($"[BattleScene] Loading background: {texPath}");
                var bgImage = GetNode<TextureRect>("BackgroundImage");
                bgImage.Texture = GD.Load<Texture2D>(texPath);
                bgImage.Show();
                GetNode<ColorRect>("Background").Hide();
            }
            else
            {
                GD.PrintErr($"[BattleScene] Background artwork not found: {texPath}");
            }
        }

        // Wait a single frame to ensure GodotNavigationService has called Initialize()
        await ToSignal(GetTree(), "process_frame");

        if (_session.CurrentCharacter != null)
        {
            _session.CurrentCharacter.ConsolidateInventory();
        }

        SetupBattle();
        SetupWeaponSkills();
        SetupHotbar();
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

        if (_battleArgs != null && _battleArgs.IsResuming && _battleArgs.Snapshot != null)
        {
            var snapshot = _battleArgs.Snapshot;
            // Party and Enemy HP already restored via Initialize reference sharing or loop.
            // Only need to restore round/turn state.
            _turnManager.CurrentRound = snapshot.CurrentRound;
            _turnManager.CurrentTurnIndex = snapshot.CurrentTurnIndex;
        }

        UpdateTurnOrderUI();
    }

    private void SetupWeaponSkills()
    {
        if (_session.CurrentCharacter == null) return;
        var character = _session.CurrentCharacter;

        _currentWeaponSkills = _weaponSkillService.GetEquippedSkills(character);

        var actionsArea = GetNodeOrNull<HBoxContainer>("ActionsArea");
        if (actionsArea == null)
        {
            GD.PrintErr("[BattleScene] ActionsArea not found!");
            return;
        }

        var buttons = new Button[]
        {
            actionsArea.GetNodeOrNull<Button>("Attack1"),
            actionsArea.GetNodeOrNull<Button>("Attack2"),
            actionsArea.GetNodeOrNull<Button>("Attack3"),
            actionsArea.GetNodeOrNull<Button>("Attack4"),
            actionsArea.GetNodeOrNull<Button>("Attack5")
        };

        for (int i = 0; i < buttons.Length; i++)
        {
            var btn = buttons[i];
            if (btn == null) continue;

            if (i < _currentWeaponSkills.Count)
            {
                var skill = _currentWeaponSkills[i];
                bool onCooldown = _skillCooldowns.TryGetValue(skill.Id, out int turnsLeft) && turnsLeft > 0;

                if (onCooldown)
                {
                    btn.Text = $"{skill.Name.ToUpper()} (CD: {turnsLeft})";
                    btn.Disabled = true;
                }
                else
                {
                    btn.Text = skill.Name.ToUpper();
                    btn.Disabled = false;
                }

                btn.TooltipText = skill.Description;

                // Clear existing connections if any
                foreach (var connection in btn.GetSignalConnectionList("pressed"))
                    btn.Disconnect("pressed", connection["callable"].AsCallable());

                btn.Pressed += () => ExecuteWeaponSkill(skill);
                btn.Show();
            }
            else
            {
                btn.Hide();
            }
        }
    }

    private void SetupHotbar()
    {
        if (_session.CurrentCharacter == null) return;
        var character = _session.CurrentCharacter;

        var buttons = new[]
        {
            _hotbarContainer.GetNode<Button>("Slot1"),
            _hotbarContainer.GetNode<Button>("Slot2"),
            _hotbarContainer.GetNode<Button>("Slot3"),
            _hotbarContainer.GetNode<Button>("Slot4"),
            _hotbarContainer.GetNode<Button>("Slot5")
        };

        for (int i = 0; i < 5; i++)
        {
            string? itemName = character.Hotbar[i];
            var btn = buttons[i];

            // Clear existing connections
            foreach (var connection in btn.GetSignalConnectionList("pressed"))
                btn.Disconnect("pressed", connection["callable"].AsCallable());

            if (!string.IsNullOrEmpty(itemName))
            {
                var item = character.Inventory.FirstOrDefault(it => it.Name == itemName);
                string btnText = itemName.ToUpper();
                
                if (item != null && item.Quantity > 0)
                {
                    btn.Text = $"{btnText} (X{item.Quantity})";
                    btn.TooltipText = item.Description;
                    btn.Disabled = false;
                    btn.Modulate = new Color(1, 1, 1, 1);
                    int slot = i;
                    btn.Pressed += () => UseHotbarItem(slot);
                }
                else
                {
                    // Item missing or quantity 0 - KEEP ON HOTBAR but grey out
                    btn.Text = $"{btnText} (X0)";
                    btn.TooltipText = "";
                    TooltipHelper.Bind(btn, "Out of items!", _tooltip);
                    btn.Disabled = true;
                    btn.Modulate = new Color(1, 1, 1, 0.5f); 
                }
                btn.Show();
            }
            else
            {
                btn.Hide();
            }
        }
    }

    private async void UseHotbarItem(int slot)
    {
        if (_isProcessingTurn || _session.CurrentCharacter == null || !IsInsideTree()) return;
        
        var character = _session.CurrentCharacter;
        string? itemName = character.Hotbar[slot];
        if (string.IsNullOrEmpty(itemName)) return;

        var item = character.Inventory.FirstOrDefault(it => it.Name == itemName);
        if (item == null || item.Quantity <= 0) return;

        _isProcessingTurn = true;
        EnablePlayerInput(false);
        character.IsBlocking = false;

        try
        {
            if (item.Type == "Consumable")
            {
                _combatLog.AppendText($"\n[color=cyan]{character.Name}[/color] uses [b]{item.Name}[/b]!");
                
                // Basic healing logic
                if (item.Name.Contains("Health Potion"))
                {
                    int healAmount = 30; // Standard heal for now
                    character.CurrentHP = Mathf.Min(character.CurrentHP + healAmount, character.MaxHP);
                    if (_partyHealthBars.Count > 0)
                        _partyHealthBars[0].UpdateValue(character.CurrentHP, character.MaxHP);
                    
                    _combatLog.AppendText($"\nHealed for {healAmount} HP!");
                }
                else
                {
                    _combatLog.AppendText("\nNo effect!");
                }
                
                // Decrement quantity
                item.Quantity--;
                if (item.Quantity <= 0)
                {
                    character.Inventory.Remove(item);
                }
                
                await Task.Run(() => _characterService.SaveCharacter(character));
                
                SetupHotbar(); // Refresh hotbar icons/visibility
            }
            else
            {
                 _combatLog.AppendText($"\n[color=gray]Cannot use {item.Name} in battle.[/color]");
                 EnablePlayerInput(true);
                 _isProcessingTurn = false;
                 return;
            }

            await ToSignal(GetTree().CreateTimer(0.5), "timeout");
            
            _turnManager.NextTurn();
            ProcessNextTurn();
        }
        finally
        {
            _isProcessingTurn = false;
        }
    }

    private async void RetryBattle()
    {
        _endBattlePanel.Hide();
        _enemies.Clear();
        _enemyMap.Clear();
        
        foreach (var e in _originalEnemies)
        {
            var enemy = new Enemy
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
            };
            _enemies.Add(enemy);
            
            // Link to the original spawn if we have BattleArgs
            if (_battleArgs?.Combat != null)
            {
                var spawn = _battleArgs.Combat.Enemies.FirstOrDefault(s => s.Name == e.Name);
                if (spawn != null)
                {
                    spawn.CurrentHP = e.MaxHP;
                    _enemyMap[enemy] = spawn;
                }
            }
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
        SetupHotbar();
        
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

        if (!_isProcessingTurn && _turnManager.CurrentEntity is Character)
        {
            if (_currentWeaponSkills.Count > 0)
            {
                ExecuteWeaponSkill(_currentWeaponSkills[0]);
            }
        }
    }

    private async Task UpdateSprites()
    {
        if (!IsInsideTree()) return;

        // Optimization: If counts match, only update health bars to avoid frame hitches
        if (_partyContainer.GetChildCount() == _party.Count && _enemyContainer.GetChildCount() == _enemies.Count)
        {
            for (int i = 0; i < _party.Count; i++)
                _partyHealthBars[i].UpdateValue(_party[i].CurrentHP, _party[i].MaxHP);
            for (int i = 0; i < _enemies.Count; i++)
                _enemyHealthBars[i].UpdateValue(_enemies[i].CurrentHP, (int)_enemies[i].MaxHP);
            return;
        }

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
            var wrapper = new VBoxContainer 
            { 
                CustomMinimumSize = new Vector2(250, 180),
                ClipContents = false
            };
            wrapper.AddThemeConstantOverride("separation", 5);
            _partyContainer.AddChild(wrapper);

            var hpBar = statusBarScene.Instantiate<StatusBar>();
            wrapper.AddChild(hpBar);
            hpBar.Setup(character.Name, character.CurrentHP, character.MaxHP, StatusType.HP);
            _partyHealthBars.Add(hpBar);

            var spriteContainer = new Control 
            { 
                CustomMinimumSize = new Vector2(250, 135),
                ClipContents = false
            };
            wrapper.AddChild(spriteContainer);

            var sprite = layeredSpriteScene.Instantiate<LayeredSprite>();
            spriteContainer.AddChild(sprite);
            sprite.Position = new Vector2(20, 0);
            sprite.Scale = new Vector2(2.5f, 2.5f);
            _partySprites.Add(sprite);

            await sprite.SetupCharacter(character, _catalog, _fileSystem, _compositor);
            sprite.Play("idle_right");
        }

        for (int i = 0; i < _enemies.Count; i++)
        {
            var enemy = _enemies[i];
            var wrapper = new VBoxContainer { CustomMinimumSize = new Vector2(250, 180) };
            wrapper.AddThemeConstantOverride("separation", 5);
            _enemyContainer.AddChild(wrapper);

            var hpBar = statusBarScene.Instantiate<StatusBar>();
            wrapper.AddChild(hpBar);
            hpBar.Setup(enemy.Name, enemy.CurrentHP, enemy.MaxHP, StatusType.HP);
            _enemyHealthBars.Add(hpBar);

            var spriteContainer = new Control { CustomMinimumSize = new Vector2(250, 135) };
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
            
            float offsetX = enemy.SpriteOffsetX ?? 20f;
            float offsetY = enemy.SpriteOffsetY ?? 0f;
            sprite.Position = new Vector2(offsetX, offsetY);
            
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
                
                //This is how to add a weapon overlay
                //if (enemy.SpriteKey.Contains("Balgathor"))
                //{
                //    await sprite.AddWeaponOverlay("Arming Sword (Steel)", _catalog, _fileSystem, _compositor);
                //}
                
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

        SyncCombatState();

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
            foreach (var key in _skillCooldowns.Keys.ToList())
            {
                _skillCooldowns[key]--;
                if (_skillCooldowns[key] <= 0)
                    _skillCooldowns.Remove(key);
            }
            SetupWeaponSkills();

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
        var attackButtons = new[]
        {
            GetNode<Button>("ActionsArea/Attack1"),
            GetNode<Button>("ActionsArea/Attack2"),
            GetNode<Button>("ActionsArea/Attack3"),
            GetNode<Button>("ActionsArea/Attack4"),
            GetNode<Button>("ActionsArea/Attack5")
        };
        for (int i = 0; i < attackButtons.Length; i++)
        {
            if (!enable)
            {
                attackButtons[i].Disabled = true;
            }
            else
            {
                if (i < _currentWeaponSkills.Count)
                {
                    var skill = _currentWeaponSkills[i];
                    attackButtons[i].Disabled = _skillCooldowns.TryGetValue(skill.Id, out int cd) && cd > 0;
                }
                else
                {
                    attackButtons[i].Disabled = true;
                }
            }
        }

        if (_hotbarContainer != null && _session.CurrentCharacter != null)
        {
            var character = _session.CurrentCharacter;
            for (int i = 0; i < 5; i++)
            {
                var btn = _hotbarContainer.GetNode<Button>($"Slot{i + 1}");
                string? itemName = character.Hotbar[i];
                var item = character.Inventory.FirstOrDefault(it => it.Name == itemName);

                if (enable)
                {
                    // Only re-enable if there is actually an item to use
                    btn.Disabled = string.IsNullOrEmpty(itemName) || item == null || item.Quantity <= 0;
                }
                else
                {
                    btn.Disabled = true;
                }
            }
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
                SyncCombatState();

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
        if (_skillCooldowns.TryGetValue(skill.Id, out int cd) && cd > 0) return;

        var currentEntity = _turnManager.CurrentEntity;
        if (!(currentEntity is Character attacker)) return;

        int actualIndex = _selectedEnemyIndex < _enemies.Count ? _selectedEnemyIndex : 0;
        var target = _enemies[actualIndex];

        if (target.CurrentHP <= 0 || attacker.CurrentHP <= 0) return;

        _isProcessingTurn = true;
        EnablePlayerInput(false);
        attacker.IsBlocking = false;

        // Deduct skill resource costs (mana/stamina)
        _combat.ApplySkillCosts(attacker, skill);

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
                    ActionType.Shoot => "shoot_right",
                    _ => "walk_right"
                };

                if (!attackerSprite.HasAnimation(attackAnim))
                {
                    if (skill.SkillType == "Magical") attackAnim = "spellcast_right";
                    else attackAnim = "walk_right";
                }

                // Handle oversize animations for attacks
                string oversizeAnim = skill.AssociatedAction switch
                {
                    ActionType.Slash => "slash_oversize_right",
                    ActionType.Thrust => "thrust_oversize_right",
                    _ => ""
                };

                if (!string.IsNullOrEmpty(oversizeAnim) && attackerSprite.HasAnimation(oversizeAnim))
                {
                    attackAnim = oversizeAnim;
                }

                var originalPos = attackerSprite.Position;
                var targetPos = targetSprite.Position;
                bool isRanged = skill.AssociatedAction == ActionType.Cast || skill.AssociatedAction == ActionType.Shoot;

                attackerSprite.Play(attackAnim);

                if (isRanged)
                {
                    string colorHex = skill.SkillType == "Magical" ? "#00FFFF" : "#CCCCCC";
                    var proj = new ColorRect
                    {
                        Color = new Color(colorHex),
                        Size = new Vector2(10, 10),
                        GlobalPosition = attackerSprite.GlobalPosition + new Vector2(100, 100)
                    };
                    AddChild(proj);

                    var projTween = GetTree().CreateTween();
                    projTween.TweenProperty(proj, "global_position", targetSprite.GlobalPosition + new Vector2(100, 100), 0.3f)
                        .SetTrans(Tween.TransitionType.Linear);

                    await ToSignal(projTween, "finished");
                    proj.QueueFree();
                }
                else
                {
                    var lungePos = originalPos + new Vector2(50, 0);
                    var tween = GetTree().CreateTween();
                    tween.TweenProperty(attackerSprite, "position", lungePos, 0.15f).SetTrans(Tween.TransitionType.Quad)
                        .SetEase(Tween.EaseType.Out);
                    
                    attackerSprite.Play(attackAnim);
                    await ToSignal(tween, "finished");
                    await ToSignal(GetTree().CreateTimer(0.5), "timeout");
                }

                if (skill.IsAOE)
                {
                    _combatLog.AppendText($"\n[color=orange]{attacker.Name}[/color] performs a sweeping [b]{skill.Name}[/b]!");
                    for (int i = 0; i < _enemies.Count; i++)
                    {
                        var e = _enemies[i];
                        var result = _combat.CalculateDamage(attacker, e, skill: skill);
                        if (result.IsHit)
                        {
                            e.CurrentHP -= result.DamageDealt;
                            _enemyHealthBars[i].UpdateValue(e.CurrentHP, (int)e.MaxHP);
                            string critMsg = result.IsCriticalHit ? "[color=yellow]CRITICAL HIT! [/color]" : "";
                            _combatLog.AppendText($"\n{critMsg}[color=red]{attacker.Name}[/color] hits {e.Name} for {result.DamageDealt} damage! Rolled {result.D20Roll}");
                        }
                        else
                        {
                            string missMsg = result.IsCriticalMiss ? "[color=gray]CRITICAL MISS! [/color]" : "[color=gray]Miss! [/color]";
                            _combatLog.AppendText($"\n{missMsg}[color=red]{attacker.Name}[/color] misses {e.Name}! Rolled {result.D20Roll}");
                        }
                    }
                    SyncCombatState();
                }
                else
                {
                    var combatResult = _combat.CalculateDamage(attacker, target, skill: skill);
                    
                    if (combatResult.IsHit)
                    {
                        target.CurrentHP -= combatResult.DamageDealt;
                        _enemyHealthBars[actualIndex].UpdateValue(target.CurrentHP, (int)target.MaxHP);
                        SyncCombatState();
                        
                        string critMsg = combatResult.IsCriticalHit ? "[color=yellow]CRITICAL HIT! [/color]" : "";
                        _combatLog.AppendText($"\n{critMsg}[color=red]{attacker.Name}[/color] uses [b]{skill.Name}[/b] on {target.Name} for {combatResult.DamageDealt} damage! Rolled {combatResult.D20Roll}");
                    }
                    else
                    {
                        string missMsg = combatResult.IsCriticalMiss ? "[color=gray]CRITICAL MISS! [/color]" : "[color=gray]Miss! [/color]";
                        _combatLog.AppendText($"\n{missMsg}[color=red]{attacker.Name}[/color] tried to use [b]{skill.Name}[/b] on {target.Name} but missed! Rolled {combatResult.D20Roll}");
                    }
                }

                if (!isRanged)
                {
                    var returnTween = GetTree().CreateTween();
                    returnTween.TweenProperty(attackerSprite, "position", originalPos, 0.15f)
                        .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
                    await ToSignal(returnTween, "finished");
                }
                
                attackerSprite.Play("idle_right");
            }

            var defeatedEnemies = _enemies.Where(e => e.CurrentHP <= 0).ToList();
            foreach (var defeated in defeatedEnemies)
            {
                _combatLog.AppendText($"\n[color=gold]{defeated.Name} is defeated![/color]");
                if (defeated.MoralityImpact != 0) _party[0].Morality += defeated.MoralityImpact;
                
                _turnManager.RemoveEntity(defeated);
                _enemies.Remove(defeated);
            }

            if (defeatedEnemies.Count > 0)
            {
                _selectedEnemyIndex = 0;
                await UpdateSprites();
            }

            if (_enemies.Count == 0)
            {
                Victory();
                return;
            }
            
            if (skill.Cooldown > 0)
            {
                _skillCooldowns[skill.Id] = skill.Cooldown;
            }
            
            _turnManager.NextTurn();
            ProcessNextTurn();
        }
        finally
        {
            _isProcessingTurn = false;
        }
    }

    private async void Victory(bool survived = false)
    {
        _combatLog.AppendText("\n[color=yellow]VICTORY![/color]");
        _endBattleTitle.Text = "VICTORY ACHIEVED";
        _endBattleTitle.Set("theme_override_colors/font_color", Colors.Gold);

        var character = _session.CurrentCharacter;

        // Restore HP to full on victory as requested
        if (character != null)
        {
            character.CurrentHP = character.MaxHP;
        }

        int totalXp = _originalEnemies.Sum(e => e.ExperienceReward);
        string victoryMsg = survived ? $"You survived {_survivalTurns} turns!" : "All enemies have been defeated!";

        if (character != null && totalXp > 0)
        {
            var result = _leveling.AwardExperience(character, totalXp);
            victoryMsg += $"\n+{result.XpAwarded} XP";
            if (result.DidLevelUp)
                victoryMsg += $"\nLevel Up! You are now level {result.NewLevel}!";
            if (result.TalentPointsAwarded > 0)
                victoryMsg += $"\n[color=gold]TALENT POINTS GAINED: {result.TalentPointsAwarded}![/color]";
        }

        if (character != null)
        {
            var rewards = _rewardService.ProcessCombatRewards(character, _originalEnemies);
            if (rewards.GoldAwarded > 0)
                victoryMsg += $"\n+{rewards.GoldAwarded} Gold";
            
            if (rewards.ItemsAwarded.Count > 0)
            {
                var itemNames = string.Join(", ", rewards.ItemsAwarded.Select(i => i.Name));
                victoryMsg += $"\nItems: {itemNames}";
            }

            // Save character after rewards and XP are processed
            await Task.Run(() => _characterService.SaveCharacter(character));
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

    private void SyncCombatState()
    {
        if (_battleArgs == null) return;

        _battleArgs.IsResuming = true;

        if (_battleArgs.Snapshot == null)
            _battleArgs.Snapshot = new CombatSnapshot();

        _battleArgs.Snapshot.CurrentRound = _turnManager.CurrentRound;
        _battleArgs.Snapshot.CurrentTurnIndex = _turnManager.CurrentTurnIndex;

        _battleArgs.Snapshot.PartyHP.Clear();
        for (int i = 0; i < _party.Count; i++)
            _battleArgs.Snapshot.PartyHP[i] = _party[i].CurrentHP;

        _battleArgs.Snapshot.EnemyHP.Clear();
        _battleArgs.Snapshot.SkillCooldowns = new Dictionary<int, int>(_skillCooldowns);

        if (_battleArgs.Combat != null)
        {
            // We must iterate through the ORIGINAL enemy list to ensure snapshot indices match Initialize loop
            for (int i = 0; i < _battleArgs.Combat.Enemies.Count; i++)
            {
                var spawn = _battleArgs.Combat.Enemies[i];
                
                // Find corresponding live enemy in our map
                var liveEnemy = _enemyMap.FirstOrDefault(p => p.Value == spawn).Key;
                if (liveEnemy != null)
                {
                    spawn.CurrentHP = liveEnemy.CurrentHP;
                    _battleArgs.Snapshot.EnemyHP[i] = liveEnemy.CurrentHP;
                }
            }
        }

        // Persist to database for auto-resume
        if (_session.CurrentCharacter != null && !string.IsNullOrEmpty(_questChainId))
        {
            var state = _questService.GetQuestState(_session.CurrentCharacter.Id, _questChainId);
            if (state != null)
            {
                state.CurrentCombatSnapshot = _battleArgs.Snapshot;
                _questService.UpdateQuestState(state);
            }
        }
    }
}
