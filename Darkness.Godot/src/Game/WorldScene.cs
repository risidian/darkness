using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Darkness.Godot.UI;

namespace Darkness.Godot.Game;

public partial class WorldScene : Node2D, IInitializable
{
	private INavigationService _navigation = null!;
	private ISessionService _session = null!;
	private IQuestService _questService = null!;
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
	private List<DialogueChoice> _currentChoices = new();
	private QuestNode? _currentDialogueQuest = null;
	private string? _pendingNextQuestId = null; // Stores the ID of the quest chosen by the player

	public void Initialize(IDictionary<string, object> parameters)
	{
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		bool isClickOrTouch = (@event is InputEventScreenTouch touch && touch.Pressed) || 
		                      (@event is InputEventMouseButton mouse && mouse.Pressed && mouse.ButtonIndex == MouseButton.Left);

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
		GetNode<Button>("CanvasLayer/TopMenu/DeathmatchButton").Pressed += () => _navigation.NavigateToAsync("DeathmatchPage");
		GetNode<Button>("CanvasLayer/TopRightMenu/MenuButton").Pressed += () => _pauseMenu.Toggle();

		GetNode<Area2D>("NPC").BodyEntered += (body) => {
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
		await _npcSprite.SetupCharacter(new Character { 
			SkinColor = knightAppearance.SkinColor,
			HairStyle = knightAppearance.HairStyle,
			HairColor = knightAppearance.HairColor,
			ArmorType = knightAppearance.ArmorType,
			WeaponType = knightAppearance.WeaponType,
			Feet = knightAppearance.Feet,
			Arms = knightAppearance.Arms,
			Legs = knightAppearance.Legs
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
		if (Input.IsActionPressed("ui_left")) { velocity.X -= 1; keyboardMoving = true; }
		if (Input.IsActionPressed("ui_right")) { velocity.X += 1; keyboardMoving = true; }
		if (Input.IsActionPressed("ui_up")) { velocity.Y -= 1; keyboardMoving = true; }
		if (Input.IsActionPressed("ui_down")) { velocity.Y += 1; keyboardMoving = true; }

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

		// This trigger should now check for pending quests from dialogue choices
		// It should be called even if _pendingNextQuestId is set, to initiate the pending quest.
		if (_player.GlobalPosition.X > 1200)
		{
			TriggerEncounter();
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
		_currentDialogueQuest = null;
		_pendingNextQuestId = null; // Reset pending quest on new dialogue

		// Load dynamic dialogue from quest if available
		var quest = _session.CurrentCharacter != null ? _questService.GetNextAvailableMainStoryQuest(_session.CurrentCharacter) : null;
		if (quest?.Dialogue != null && quest.Dialogue.Lines.Count > 0)
		{
			_currentDialogueQuest = quest;
			_speakerName = quest.Dialogue.Speaker;
			_dialogue = new List<string>(quest.Dialogue.Lines);
			if (quest.Dialogue.Choices != null)
			{
				_currentChoices = new List<DialogueChoice>(quest.Dialogue.Choices);
			}
			GD.Print($"[WorldScene] Loaded dialogue for quest: {quest.Title} (ID: {quest.Id}). Speaker: {_speakerName}, Lines: {_dialogue.Count}, Choices: {_currentChoices.Count}");
		}
		else
		{
			// Fallback hardcoded dialogue
			_speakerName = "Old Man";
			_dialogue = new List<string> {
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

			// If there were no choices, mark the dialogue quest as complete when finished
			if (_currentChoices.Count == 0 && _currentDialogueQuest != null && _session.CurrentCharacter != null)
			{
				GD.Print($"[WorldScene] Completing quest: {_currentDialogueQuest.Id} (no choices)");
				_questService.CompleteQuest(_session.CurrentCharacter, _currentDialogueQuest.Id);
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

	private void OnChoiceSelected(DialogueChoice choice)
	{
		GD.Print($"[WorldScene] Choice selected: '{choice.Text}' (NextQuestId: {choice.NextQuestId}, MoralityImpact: {choice.MoralityImpact})");
		
		// 1. Mark the current dialogue quest as complete
		if (_currentDialogueQuest != null && _session.CurrentCharacter != null)
		{
			GD.Print($"[WorldScene] Completing current dialogue quest: {_currentDialogueQuest.Id}");
			_questService.CompleteQuest(_session.CurrentCharacter, _currentDialogueQuest.Id);
			
			// 2. Store the NextQuestId to be triggered later. Do NOT complete it yet.
			_pendingNextQuestId = choice.NextQuestId;
			GD.Print($"[WorldScene] Choice selected: '{choice.Text}'. Next quest ID pending: {_pendingNextQuestId ?? "None"}");

			// Apply morality
			if (choice.MoralityImpact != 0)
			{
				_session.CurrentCharacter.Morality += choice.MoralityImpact;
				GD.Print($"[Morality] Changed by {choice.MoralityImpact}. New Total: {_session.CurrentCharacter.Morality}");
			}
		}

		// 3. Hide dialogue box and end conversation
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
		GD.Print($"[WorldScene] TriggerEncounter called. _isEncounterTriggered: {_isEncounterTriggered}, _currentDialogueIndex: {_currentDialogueIndex}, _pendingNextQuestId: {_pendingNextQuestId ?? "null"}");

		// Prevent re-triggering if an encounter is already in progress or dialogue is active.
		// If a choice was made and a quest is pending, we SHOULD proceed to trigger it.
		if (_isEncounterTriggered || _currentDialogueIndex >= 0) 
		{
			GD.Print($"[WorldScene] TriggerEncounter blocked: Encountered={_isEncounterTriggered}, Dialogue={_currentDialogueIndex >= 0}");
			return;
		}

		QuestNode? questToTrigger = null;

		// 1. Check if a choice was made that leads to a specific quest.
		if (!string.IsNullOrEmpty(_pendingNextQuestId))
		{
			GD.Print($"[WorldScene] Checking pending quest ID: {_pendingNextQuestId}");
			questToTrigger = _questService.GetQuestById(_pendingNextQuestId);
			GD.Print($"[WorldScene] Pending quest '{_pendingNextQuestId}' found: {questToTrigger?.Title ?? "None"}");
			_pendingNextQuestId = null; // Reset after checking
		}

		// 2. If no pending choice-driven quest, use location-based or next main story quest.
		if (questToTrigger == null)
		{
			questToTrigger = _questService.GetQuestByLocation("SandyShore_East");
			GD.Print($"[WorldScene] No pending quest. Checking location 'SandyShore_East'. Found: {questToTrigger?.Title ?? "None"}");

			if (questToTrigger == null && _session.CurrentCharacter != null)
			{
				questToTrigger = _questService.GetNextAvailableMainStoryQuest(_session.CurrentCharacter);
				GD.Print($"[WorldScene] No location quest found. Falling back to next main story quest: {questToTrigger?.Title ?? "None"}");
			}
		}

		// 3. Trigger the determined quest.
		if (questToTrigger != null)
		{
			GD.Print($"[WorldScene] Triggering quest: {questToTrigger.Title} (ID: {questToTrigger.Id})");
			if (questToTrigger.Encounter != null)
			{
				GD.Print($"[WorldScene] Navigating to BattleScene with encounter from quest '{questToTrigger.Title}'.");
				await _navigation.NavigateToAsync(Routes.Battle, new BattleArgs { Encounter = questToTrigger.Encounter });
				_isEncounterTriggered = true; // Mark as triggered to prevent re-triggering immediately by player movement
			}
			else if (questToTrigger.Dialogue != null && questToTrigger.Dialogue.Lines.Count > 0)
			{
				// If the chosen quest is dialogue, we need to initiate it.
				// This might involve starting a new dialogue sequence. For now, we'll log it.
				GD.Print($"[WorldScene] Quest '{questToTrigger.Title}' has dialogue but no immediate encounter. Requires NPC interaction or separate trigger.");
				// If we want immediate dialogue after choice, we'd need to call StartDialogue or similar here.
				// For now, we assume dialogue is NPC-initiated.
				_isEncounterTriggered = true; // Mark as triggered to prevent further triggers for now.
			}
			else
			{
				GD.Print($"[WorldScene] Quest '{questToTrigger.Title}' found, but has no encounter or dialogue to trigger immediately.");
				// If it's a quest that's just marked complete, but no immediate event, reset flag.
				_isEncounterTriggered = false; 
			}
		}
		else
		{
			GD.Print("[WorldScene] No quest found to trigger.");
			// If no encounter is found, reset the flag.
			_isEncounterTriggered = false;
		}
	}
	
	// Helper to check if a quest is completed (for TriggerEncounter logic)
	private bool IsQuestCompleted(string questId)
	{
		return _session.CurrentCharacter != null && _session.CurrentCharacter.CompletedQuestIds.Contains(questId);
	}
}
