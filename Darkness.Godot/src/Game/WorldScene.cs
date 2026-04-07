using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Darkness.Godot.UI;

namespace Darkness.Godot.Game;

public partial class WorldScene : Node2D, IInitializable
{
    private INavigationService _navigation = null!;
    private ISessionService _session = null!;
    private IQuestService _questService = null!;
    private ITriggerService _triggerService = null!;
    private ISpriteCompositor _compositor = null!;
    private ISpriteLayerCatalog _catalog = null!;
    private IFileSystemService _fileSystem = null!;

    private CharacterBody2D _player = null!;
    private LayeredSprite _playerSprite = null!;
    private LayeredSprite _npcSprite = null!;
    private PanelContainer _dialogueBox = null!;
    private PauseMenu _pauseMenu = null!;
    private Label _nameLabel = null!;
    private Label _textLabel = null!;

    private bool _isEncounterTriggered = false;
    private bool _isReady = false;
    private float _moveSpeed = 300f;
    private List<string> _dialogue = new();
    private string _speakerName = "Old Man";
    private int _currentDialogueIndex = -1;
    private string _lastDirection = "down";
    private Vector2? _targetPosition = null;
    private Vector2? _startingPosition = null;

    private VBoxContainer _choicesContainer = null!;
    private List<BranchOption> _currentChoices = new();
    private QuestChain? _currentDialogueChain = null;
    private QuestStep? _currentDialogueStep = null;

    public void Initialize(IDictionary<string, object> parameters)
    {
        _isEncounterTriggered = true; // Block triggers during initialization

        if (parameters.ContainsKey("PlayerPosition") && parameters["PlayerPosition"] is Vector2 pos)
        {
            _startingPosition = pos;
        }

        if (parameters.ContainsKey("StealthOutcome") && _session.CurrentCharacter != null)
        {
            string outcome = parameters["StealthOutcome"].ToString() ?? "Failure";
            string? questChainId = parameters.ContainsKey("QuestChainId")
                ? parameters["QuestChainId"].ToString()
                : null;

            if (outcome == "Success")
            {
                GD.Print("[WorldScene] Stealth successful. Skipping combat and finishing chain.");
                if (questChainId != null)
                {
                    // Advance past stealth
                    _questService.AdvanceStep(_session.CurrentCharacter, questChainId);
                    // Advance past combat (finish chain)
                    _questService.AdvanceStep(_session.CurrentCharacter, questChainId);
                }
            }
            else
            {
                GD.Print("[WorldScene] Stealth failed. Advancing quest to trigger combat.");
                if (questChainId != null)
                    _questService.AdvanceStep(_session.CurrentCharacter, questChainId);
            }
        }
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        bool isClickOrTouch = (@event is InputEventScreenTouch touch && touch.Pressed) ||
                              (@event is InputEventMouseButton mouse && mouse.Pressed &&
                               mouse.ButtonIndex == MouseButton.Left);

        if (isClickOrTouch)
        {
            if (_currentDialogueIndex >= 0)
            {
                NextDialogue();
                GetViewport().SetInputAsHandled();
            }
            else
            {
                _targetPosition = GetGlobalMousePosition();
            }
        }
    }

    public override async void _Ready()
    {
        var global = GetNode<Global>("/root/Global");
        var sp = global.Services!;
        _navigation = sp.GetRequiredService<INavigationService>();
        _session = sp.GetRequiredService<ISessionService>();
        _questService = sp.GetRequiredService<IQuestService>();
        _triggerService = sp.GetRequiredService<ITriggerService>();
        _compositor = sp.GetRequiredService<ISpriteCompositor>();
        _catalog = sp.GetRequiredService<ISpriteLayerCatalog>();
        _fileSystem = sp.GetRequiredService<IFileSystemService>();

        // Apply simple shader to Backgrounds
        var bgShader = GD.Load<Shader>("res://src/Shaders/simple_rect.gdshader");
        var bgMat = new ShaderMaterial { Shader = bgShader };
        GetNode<ColorRect>("Background").Material = bgMat;
        GetNode<ColorRect>("Water").Material = bgMat;

        _player = GetNode<CharacterBody2D>("Player");
        _playerSprite = GetNode<LayeredSprite>("Player/Sprite");
        _npcSprite = GetNode<LayeredSprite>("NPC/Sprite");

        if (_startingPosition.HasValue)
        {
            _player.GlobalPosition = _startingPosition.Value;
            GD.Print($"[WorldScene] Set starting position to: {_startingPosition.Value}");
        }

        _dialogueBox = GetNode<PanelContainer>("CanvasLayer/DialogueBox");
        _pauseMenu = GetNode<PauseMenu>("CanvasLayer/PauseMenu");
        _nameLabel = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/NameLabel");
        _textLabel = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/TextLabel");

        _choicesContainer = new VBoxContainer();
        GetNode<VBoxContainer>("CanvasLayer/DialogueBox/VBoxContainer").AddChild(_choicesContainer);

        GetNode<Button>("CanvasLayer/TopMenu/ForgeButton").Pressed += () => _navigation.NavigateToAsync("ForgePage");
        GetNode<Button>("CanvasLayer/TopMenu/StudyButton").Pressed += () => _navigation.NavigateToAsync("StudyPage");
        GetNode<Button>("CanvasLayer/TopMenu/AlliesButton").Pressed += () => _navigation.NavigateToAsync("AlliesPage");
        GetNode<Button>("CanvasLayer/TopMenu/DeathmatchButton").Pressed +=
            () => _navigation.NavigateToAsync("DeathmatchPage");
        GetNode<Button>("CanvasLayer/TopRightMenu/MenuButton").Pressed += () => _pauseMenu.Toggle();

        GetNode<Area2D>("NPC").BodyEntered += (body) =>
        {
            if (body == _player) _ = TriggerEncounter();
        };

        // Find current step to initialize visuals
        if (_session.CurrentCharacter != null)
        {
            var character = _session.CurrentCharacter;
            GD.Print($"[WorldScene] Diagnostic START for Char {character.Id} ({character.Name})");
            
            var completedChains = _questService.GetCompletedChainIds(character.Id);
            var availableChains = _questService.GetAvailableChains(character);

            GD.Print($"[WorldScene]   Completed chains: [{string.Join(", ", completedChains)}]");
            GD.Print($"[WorldScene]   Available chains: [{string.Join(", ", availableChains.Select(c => c.Id))}]");

            var chain = availableChains.FirstOrDefault();
            if (chain != null)
            {
                var step = _questService.GetCurrentStep(character, chain.Id);
                GD.Print($"[WorldScene] Initializing visuals for chain: {chain.Id}, step: {step?.Id ?? "NULL"}");
                await UpdateVisuals(step, chain);
            }
            else
            {
                GD.PrintErr("[WorldScene] No available quest chains found in _Ready.");
            }
        }

        await UpdateSprites();

        // Final safety: Wait one more frame before enabling triggers
        await ToSignal(GetTree(), "process_frame");
        _isReady = true;
        _isEncounterTriggered = false;
        GD.Print("[WorldScene] Ready and triggers enabled.");
    }

    private async Task UpdateSprites()
    {
        if (_session.CurrentCharacter != null)
        {
            await _playerSprite.SetupCharacter(_session.CurrentCharacter, _catalog, _fileSystem, _compositor);
            _playerSprite.Play("idle_down");
        }
    }

    public override void _Process(double delta)
    {
        if (!IsInsideTree() || !_isReady) return;
        if (Input.IsActionJustPressed("ui_cancel"))
        {
            _pauseMenu.Toggle();
            return;
        }

        if (_currentDialogueIndex >= 0)
        {
            if (Input.IsActionJustPressed("ui_accept"))
            {
                NextDialogue();
            }

            return;
        }

        Vector2 velocity = Vector2.Zero;

        bool keyboardMoving = false;
        if (Input.IsActionPressed("ui_left"))
        {
            velocity.X -= 1;
            keyboardMoving = true;
        }

        if (Input.IsActionPressed("ui_right"))
        {
            velocity.X += 1;
            keyboardMoving = true;
        }

        if (Input.IsActionPressed("ui_up"))
        {
            velocity.Y -= 1;
            keyboardMoving = true;
        }

        if (Input.IsActionPressed("ui_down"))
        {
            velocity.Y += 1;
            keyboardMoving = true;
        }

        if (keyboardMoving)
        {
            _targetPosition = null;
        }
        else if (_targetPosition.HasValue)
        {
            Vector2 toTarget = _targetPosition.Value - _player.GlobalPosition;
            if (toTarget.Length() < 5)
            {
                _targetPosition = null;
            }
            else
            {
                velocity = toTarget.Normalized();
            }
        }

        if (velocity != Vector2.Zero)
        {
            _player.Velocity = velocity.Normalized() * _moveSpeed;
            _player.MoveAndSlide();
            UpdateAnimation(velocity);
        }
        else
        {
            _playerSprite.Play("idle_" + _lastDirection);
        }

        // Check location trigger for quest encounters at the eastern edge
        if (_session.CurrentCharacter != null && _player.GlobalPosition.X > 1200)
        {
            var triggerStep = _triggerService.CheckLocationTrigger(_session.CurrentCharacter, "SandyShore_East");
            if (triggerStep != null)
            {
                _ = TriggerEncounter(true);
            }
        }
    }

    private void UpdateAnimation(Vector2 velocity)
    {
        if (Mathf.Abs(velocity.X) > Mathf.Abs(velocity.Y))
        {
            _lastDirection = velocity.X > 0 ? "right" : "left";
        }
        else
        {
            _lastDirection = velocity.Y > 0 ? "down" : "up";
        }

        _playerSprite.Play("walk_" + _lastDirection);
    }

    private void StartDialogue()
    {
        GD.Print("StartDialogue called.");
        GD.Print(System.Environment.StackTrace);
        _targetPosition = null;
        _player.Velocity = Vector2.Zero;
        _playerSprite.Play("idle_" + _lastDirection);

        _currentChoices.Clear();
        _currentDialogueChain = null;
        _currentDialogueStep = null;

        // Load dynamic dialogue from quest if available
        var character = _session.CurrentCharacter;
        QuestChain? chain = null;
        QuestStep? step = null;

        if (character != null)
        {
            var availableChains = _questService.GetAvailableChains(character);
            if(availableChains.Count == 0)
                GD.PrintErr("[WorldScene] No available quest chains for current character.");
            chain = availableChains.FirstOrDefault();
            if (chain != null)
                step = _questService.GetCurrentStep(character, chain.Id);
        }

        if (step != null && (step.Combat != null || step.Type == "stealth" || (step.Location?.SceneKey == "stealth")))
        {
            GD.Print($"[WorldScene] StartDialogue called on a non-dialogue step ({step.Id}). Redirecting to TriggerEncounter.");
            _isEncounterTriggered = false; // Reset so TriggerEncounter can run
            _ = TriggerEncounter();
            return;
        }

        if (step?.Dialogue != null && step.Dialogue.Lines.Count > 0)
        {
            _currentDialogueChain = chain;
            _currentDialogueStep = step;
            _speakerName = step.Dialogue.Speaker;
            _dialogue = new List<string>(step.Dialogue.Lines);

            // Use BranchData for choices if available
            if (step.Branch?.Options != null && step.Branch.Options.Count > 0)
            {
                _currentChoices = new List<BranchOption>(step.Branch.Options);
            }

            GD.Print(
                $"[WorldScene] Loaded dialogue for chain: {chain!.Title} step: {step.Id}. Speaker: {_speakerName}, Lines: {_dialogue.Count}, Choices: {_currentChoices.Count}");
        }
        else
        {
            GD.PrintErr($"Failed to find dialogue for step {step?.Id ?? "NULL"} - falling back to Default");
            // Fallback hardcoded dialogue
            _speakerName = _currentDialogueStep?.Visuals?.Npc?.Name ?? "Wanderer";
            _dialogue = new List<string>
            {
                "The path ahead is dangerous.",
                "You should prepare yourself for what's to come."
            };
            GD.Print($"[WorldScene] No quest dialogue found. Using fallback dialogue. Lines: {_dialogue.Count}");
        }

        if (_dialogue.Count > 0)
        {
            _currentDialogueIndex = 0;
            _dialogueBox.Show();

            // Update prompt for mobile/touch
            var prompt = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/PromptLabel");
            prompt.Text = "[TAP TO CONTINUE]";

            UpdateDialogueUI();
        }
        else
        {
            GD.Print("[WorldScene] No dialogue lines to display. Hiding dialogue box.");
            _dialogueBox.Hide();
        }
    }

    private async void NextDialogue()
    {
        // If we are showing choices, tapping should not advance or close the dialogue
        if (_currentDialogueIndex == _dialogue.Count - 1 && _currentChoices.Count > 0)
        {
            GD.Print("[WorldScene] Last line of dialogue with choices, tap does nothing.");
            return;
        }

        _currentDialogueIndex++;
        if (_currentDialogueIndex >= _dialogue.Count)
        {
            GD.Print("[WorldScene] End of dialogue reached.");
            _currentDialogueIndex = -1;
            _dialogueBox.Hide();
            _isEncounterTriggered = false; // Release the lock so sequential encounters can trigger

            // If there were no choices, advance the quest step when finished
            if (_currentChoices.Count == 0 && _currentDialogueChain != null && _session.CurrentCharacter != null)
            {
                GD.Print($"[WorldScene] Advancing quest chain: {_currentDialogueChain.Id} (no choices)");
                var nextStep = _questService.AdvanceStep(_session.CurrentCharacter, _currentDialogueChain.Id);

                if (nextStep != null)
                {
                    await UpdateVisuals(nextStep, _currentDialogueChain);

                    // If the new step is an immediate encounter, trigger it
                    if (nextStep.Combat != null || nextStep.Type == "stealth" || (nextStep.Location?.SceneKey == "stealth"))
                    {
                        await TriggerEncounter();
                    }
                }

                // Check if main story is complete
                if (_questService.IsMainStoryComplete(_session.CurrentCharacter))
                {
                    GD.Print("[WorldScene] Main story is complete!");
                }
            }
        }
        else
        {
            UpdateDialogueUI();
        }
    }

    private void UpdateDialogueUI()
    {
        _nameLabel.Text = _speakerName;
        _textLabel.Text = _dialogue[_currentDialogueIndex];

        var prompt = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/PromptLabel");

        // Clear existing buttons
        foreach (Node child in _choicesContainer.GetChildren())
        {
            child.QueueFree();
        }

        // If we are at the last line and have choices, show them
        if (_currentDialogueIndex == _dialogue.Count - 1 && _currentChoices.Count > 0)
        {
            GD.Print($"[WorldScene] Showing {_currentChoices.Count} choices.");
            prompt.Hide(); // Hide "TAP TO CONTINUE"

            foreach (var choice in _currentChoices)
            {
                var btn = new Button { Text = choice.Text };
                btn.Pressed += () => OnChoiceSelected(choice);
                _choicesContainer.AddChild(btn);
            }
        }
        else
        {
            prompt.Show(); // Show "TAP TO CONTINUE"
            prompt.Text = "[TAP TO CONTINUE]";
        }
    }

    private async void OnChoiceSelected(BranchOption choice)
    {
        GD.Print(
            $"[WorldScene] Choice selected: '{choice.Text}' (NextStepId: {choice.NextStepId}, MoralityImpact: {choice.MoralityImpact})");

        if (_currentDialogueChain != null && _session.CurrentCharacter != null)
        {
            // Apply morality
            if (choice.MoralityImpact != 0)
            {
                _session.CurrentCharacter.Morality += choice.MoralityImpact;
                GD.Print(
                    $"[Morality] Changed by {choice.MoralityImpact}. New Total: {_session.CurrentCharacter.Morality}");
            }

            // Advance to the chosen step
            GD.Print($"[WorldScene] Advancing chain {_currentDialogueChain.Id} to step {choice.NextStepId}");
            var nextStep = _questService.AdvanceStep(_session.CurrentCharacter, _currentDialogueChain.Id, choice.NextStepId);

            // Update visuals for the new step
            await UpdateVisuals(nextStep, _currentDialogueChain);

            // Check if main story is complete
            if (_questService.IsMainStoryComplete(_session.CurrentCharacter))
            {
                GD.Print("[WorldScene] Main story is complete!");
            }

            // If the next step has dialogue, show it immediately
            if (nextStep?.Dialogue != null && nextStep.Dialogue.Lines.Count > 0)
            {
                GD.Print($"[WorldScene] Next step '{nextStep.Id}' has dialogue. Starting immediately.");
                _currentDialogueStep = nextStep;
                _speakerName = nextStep.Dialogue.Speaker;
                _dialogue = new List<string>(nextStep.Dialogue.Lines);

                if (nextStep.Branch?.Options != null && nextStep.Branch.Options.Count > 0)
                    _currentChoices = new List<BranchOption>(nextStep.Branch.Options);
                else
                    _currentChoices.Clear();

                _currentDialogueIndex = 0;
                _dialogueBox.Show();

                var prompt = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/PromptLabel");
                prompt.Text = "[TAP TO CONTINUE]";

                UpdateDialogueUI();
                return;
            }

            // If no immediate dialogue, check if it's an encounter
            if (nextStep != null && (nextStep.Combat != null || nextStep.Type == "stealth" || (nextStep.Location?.SceneKey == "stealth")))
            {
                // Must reset dialogue index and hide box before triggering
                _currentDialogueIndex = -1;
                _dialogueBox.Hide();
                await TriggerEncounter();
                return;
            }
        }

        // Hide dialogue box and end conversation
        _currentDialogueIndex = -1;
        _dialogueBox.Hide();

        // Clear buttons
        foreach (Node child in _choicesContainer.GetChildren())
        {
            child.QueueFree();
        }
    }

    private async Task TriggerEncounter(bool isLocationTrigger = false)
    {
        if (_isEncounterTriggered) return;
        _isEncounterTriggered = true; // Block immediately

        if (_currentDialogueIndex >= 0)
        {
            _isEncounterTriggered = false; // Dialogue is already showing
            return;
        }

        var character = _session.CurrentCharacter;
        if (character == null)
        {
            _isEncounterTriggered = false;
            return;
        }

        // Find the first available quest chain and its current step
        var availableChains = _questService.GetAvailableChains(character);
        QuestChain? chain = null;
        QuestStep? step = null;

        foreach (var c in availableChains)
        {
            var s = _questService.GetCurrentStep(character, c.Id);
            if (s != null)
            {
                chain = c;
                step = s;
                break;
            }
        }

        if (chain == null || step == null)
        {
            GD.Print("[WorldScene] No quest step found to trigger.");
            return;
        }

        GD.Print($"[WorldScene] Triggering step: {step.Id} from chain: {chain.Title} (type: {step.Type})");

        if (step.Combat != null)
        {
            GD.Print($"[WorldScene] Navigating to BattleScene with combat from step '{step.Id}'.");
            _isEncounterTriggered = true; // Set before await to prevent double trigger
            await _navigation.NavigateToAsync(Routes.Battle,
                new BattleArgs { Combat = step.Combat, QuestChainId = chain.Id, QuestStepId = step.Id });
        }
        else if (step.Type == "stealth" || (step.Location?.SceneKey == "stealth"))
        {
            GD.Print($"[WorldScene] Navigating to StealthScene for step '{step.Id}'.");
            _isEncounterTriggered = true; // Set before await
            await _navigation.NavigateToAsync(Routes.Stealth,
                new StealthArgs { QuestChainId = chain.Id, QuestStepId = step.Id });
        }
        else if (!isLocationTrigger && step.Dialogue != null && step.Dialogue.Lines.Count > 0)
        {
            GD.Print($"[WorldScene] Step '{step.Id}' has dialogue. Initiating.");
            _isEncounterTriggered = true;
            StartDialogue();
        }
        else
        {
            GD.Print($"[WorldScene] Step '{step.Id}' has no actionable content or is skipped due to location trigger.");
        }
    }

    private async Task UpdateVisuals(QuestStep? step, QuestChain? chain = null)
    {
        var visuals = step?.Visuals;
        
        // Fallback: If current step has no visuals, try to use the first step of the chain
        if (visuals == null && chain != null && chain.Steps.Count > 0)
        {
            visuals = chain.Steps[0].Visuals;
            if (visuals != null)
                GD.Print($"[WorldScene] Step '{step?.Id ?? "NULL"}' has no visuals. Falling back to chain-level visuals from '{chain.Steps[0].Id}'.");
        }

        if (visuals == null)
        {
            GD.Print("[WorldScene] No visual configuration found for step or chain fallback.");
            return;
        }

        GD.Print($"[WorldScene] Applying visuals for step: {step?.Id ?? "Fallback"}");

        // 1. Background Logic
        var bgRect = GetNode<ColorRect>("Background");
        var waterRect = GetNode<ColorRect>("Water");
        var bgImage = GetNode<TextureRect>("BackgroundImage");
        bool imageLoaded = false;

        if (!string.IsNullOrEmpty(visuals.BackgroundKey))
        {
            var texPath = visuals.BackgroundKey.StartsWith("res://")
                ? visuals.BackgroundKey
                : $"res://assets/backgrounds/{visuals.BackgroundKey}.png";

            GD.Print($"[WorldScene] Loading background: {texPath}");
            if (global::Godot.FileAccess.FileExists(texPath) || ResourceLoader.Exists(texPath))
            {
                bgImage.Texture = GD.Load<Texture2D>(texPath);
                bgImage.Show();
                bgRect.Hide();
                imageLoaded = true;
            }
            else
            {
                GD.PrintErr($"[WorldScene] Background artwork not found: {texPath}. Falling back to colors.");
                bgImage.Hide();
                bgRect.Show();
            }
        }
        else
        {
            bgImage.Hide();
            bgRect.Show();
        }

        if (!string.IsNullOrEmpty(visuals.GroundColor))
        {
            bgRect.Color = Color.FromHtml(visuals.GroundColor);
        }

        if (!string.IsNullOrEmpty(visuals.WaterColor) && !imageLoaded)
        {
            waterRect.Color = Color.FromHtml(visuals.WaterColor);
            waterRect.Show();
        }
        else
        {
            waterRect.Hide();
        }

        // 2. NPC Logic
        if (visuals.Npc != null)
        {
            var npcNode = GetNode<Area2D>("NPC");
            npcNode.GlobalPosition = new Vector2(visuals.Npc.PositionX, visuals.Npc.PositionY);
            _speakerName = visuals.Npc.Name;

            var npcSprite = GetNode<LayeredSprite>("NPC/Sprite");
            GD.Print($"[WorldScene] Setting up NPC: {visuals.Npc.Name}");

            if (!string.IsNullOrEmpty(visuals.Npc.SpriteKey))
            {
                var spritePath = visuals.Npc.SpriteKey.StartsWith("res://")
                    ? visuals.Npc.SpriteKey
                    : $"res://assets/sprites/{visuals.Npc.SpriteKey}.png";

                if (global::Godot.FileAccess.FileExists(spritePath))
                {
                    GD.Print($"[WorldScene] Using full sheet for NPC: {spritePath}");
                    await npcSprite.SetupFullSheet(spritePath, _fileSystem);
                }
                else
                {
                    GD.PrintErr($"[WorldScene] NPC sprite '{spritePath}' not found.");
                    if (visuals.Npc.Appearance != null)
                    {
                        GD.Print("[WorldScene] Falling back to generated LPC appearance.");
                        var npcChar = CreateNpcCharacter(visuals.Npc);
                        await npcSprite.SetupCharacter(npcChar, _catalog, _fileSystem, _compositor);
                    }
                }
            }
            else if (visuals.Npc.Appearance != null)
            {
                GD.Print("[WorldScene] Using generated LPC appearance for NPC.");
                var npcChar = CreateNpcCharacter(visuals.Npc);
                await npcSprite.SetupCharacter(npcChar, _catalog, _fileSystem, _compositor);
            }
            npcNode.Show();
        }
        else
        {
            GD.Print("[WorldScene] Hiding NPC node (none in this step).");
            GetNode<Area2D>("NPC").Hide();
        }
    }

    private Character CreateNpcCharacter(NpcConfig config)
    {
        // Use class defaults as base to avoid "naked" sprites
        var defaults = _catalog.GetDefaultAppearanceForClass("Warrior");
        var app = config.Appearance;

        return new Character
        {
            Name = config.Name,
            SkinColor = !string.IsNullOrEmpty(app?.SkinColor) ? app.SkinColor : defaults.SkinColor,
            HairStyle = !string.IsNullOrEmpty(app?.HairStyle) ? app.HairStyle : defaults.HairStyle,
            HairColor = !string.IsNullOrEmpty(app?.HairColor) ? app.HairColor : defaults.HairColor,
            ArmorType = !string.IsNullOrEmpty(app?.ArmorType) ? app.ArmorType : defaults.ArmorType,
            WeaponType = !string.IsNullOrEmpty(app?.WeaponType) ? app.WeaponType : defaults.WeaponType,
            Feet = !string.IsNullOrEmpty(app?.Feet) ? app.Feet : defaults.Feet,
            Arms = defaults.Arms,
            Legs = !string.IsNullOrEmpty(app?.Legs) ? app.Legs : defaults.Legs,
            Head = !string.IsNullOrEmpty(app?.Head) ? app.Head : defaults.Head,
            Face = !string.IsNullOrEmpty(app?.Face) ? app.Face : defaults.Face,
            Eyes = defaults.Eyes,
            ShieldType = defaults.ShieldType
        };
    }
}
