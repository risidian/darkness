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
    private float _moveSpeed = 300f;
    private List<string> _dialogue = new();
    private string _speakerName = "Old Man";
    private int _currentDialogueIndex = -1;
    private string _lastDirection = "down";
    private Vector2? _targetPosition = null;

    private VBoxContainer _choicesContainer = null!;
    private List<BranchOption> _currentChoices = new();
    private QuestChain? _currentDialogueChain = null;
    private QuestStep? _currentDialogueStep = null;

    public void Initialize(IDictionary<string, object> parameters)
    {
        if (parameters.ContainsKey("StealthOutcome") && _session.CurrentCharacter != null)
        {
            string outcome = parameters["StealthOutcome"].ToString() ?? "Failure";
            string? questChainId = parameters.ContainsKey("QuestChainId")
                ? parameters["QuestChainId"].ToString()
                : null;

            if (outcome == "Success")
            {
                GD.Print("[WorldScene] Stealth successful. Advancing quest.");
                if (questChainId != null)
                    _questService.AdvanceStep(_session.CurrentCharacter, questChainId);
                _isEncounterTriggered = true;
            }
            else
            {
                GD.Print("[WorldScene] Stealth failed. Triggering monster battle.");
                // Let trigger logic run in Process to find the next combat step
                _isEncounterTriggered = false;
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
            if (body == _player) StartDialogue();
        };

        await UpdateSprites();
    }

    private async Task UpdateSprites()
    {
        if (_session.CurrentCharacter != null)
        {
            await _playerSprite.SetupCharacter(_session.CurrentCharacter, _catalog, _fileSystem);
            _playerSprite.Play("idle_down");
        }

        var knightAppearance = _catalog.GetDefaultAppearanceForClass("Knight");
        await _npcSprite.SetupCharacter(new Character
        {
            Name = "Old Man",
            SkinColor = knightAppearance.SkinColor,
            HairStyle = knightAppearance.HairStyle,
            HairColor = knightAppearance.HairColor,
            ArmorType = knightAppearance.ArmorType,
            WeaponType = knightAppearance.WeaponType,
            Feet = knightAppearance.Feet,
            Arms = knightAppearance.Arms,
            Legs = knightAppearance.Legs,
            Head = "Human Male",
            Face = "Default"
        }, _catalog, _fileSystem);
        _npcSprite.Play("idle_down");
    }

    public override void _Process(double delta)
    {
        if (!IsInsideTree()) return;
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

        // Check location trigger for quest encounters
        if (_session.CurrentCharacter != null)
        {
            var triggerStep = _triggerService.CheckLocationTrigger(_session.CurrentCharacter, "SandyShore_East");
            if (triggerStep != null)
            {
                TriggerEncounter();
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
            chain = availableChains.FirstOrDefault();
            if (chain != null)
                step = _questService.GetCurrentStep(character, chain.Id);
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
            // Fallback hardcoded dialogue
            _speakerName = "Old Man";
            _dialogue = new List<string>
            {
                "Welcome to the Shore of Camelot, Wanderer.",
                "The path to the castle is blocked by shadows.",
                "You'll find only hounds and darkness to the east."
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

    private void NextDialogue()
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

            // If there were no choices, advance the quest step when finished
            if (_currentChoices.Count == 0 && _currentDialogueChain != null && _session.CurrentCharacter != null)
            {
                GD.Print($"[WorldScene] Advancing quest chain: {_currentDialogueChain.Id} (no choices)");
                _questService.AdvanceStep(_session.CurrentCharacter, _currentDialogueChain.Id);

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

    private void OnChoiceSelected(BranchOption choice)
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

    private async void TriggerEncounter()
    {
        if (_isEncounterTriggered || _currentDialogueIndex >= 0) return;

        var character = _session.CurrentCharacter;
        if (character == null) return;

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
            await _navigation.NavigateToAsync(Routes.Battle,
                new BattleArgs { Combat = step.Combat, QuestChainId = chain.Id, QuestStepId = step.Id });
            _isEncounterTriggered = true;
        }
        else if (step.Type == "stealth")
        {
            GD.Print($"[WorldScene] Navigating to StealthScene for step '{step.Id}'.");
            await _navigation.NavigateToAsync(Routes.Stealth,
                new StealthArgs { QuestChainId = chain.Id, QuestStepId = step.Id });
            _isEncounterTriggered = true;
        }
        else if (step.Dialogue != null && step.Dialogue.Lines.Count > 0)
        {
            GD.Print($"[WorldScene] Step '{step.Id}' has dialogue. Initiating.");
            _isEncounterTriggered = true;
            StartDialogue();
        }
        else
        {
            GD.Print($"[WorldScene] Step '{step.Id}' has no actionable content.");
        }
    }
}
