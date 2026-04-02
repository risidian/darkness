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
	private INavigationService _navigation;
	private ISessionService _session;
	private ISpriteCompositor _compositor;
	private ISpriteLayerCatalog _catalog;
	private IFileSystemService _fileSystem;

	private CharacterBody2D _player;
	private LayeredSprite _playerSprite;
	private LayeredSprite _npcSprite;
	private PanelContainer _dialogueBox;
	private PauseMenu _pauseMenu;
	private Label _nameLabel;
	private Label _textLabel;

	private float _moveSpeed = 300f;
	private string[] _dialogue = new[] {
		"Welcome to the Shore of Camelot, Wanderer.",
		"The path to the castle is blocked by shadows.",
		"You'll find only hounds and darkness to the east."
	};
	private int _currentDialogueIndex = -1;
	private string _lastDirection = "down";
	private Vector2? _targetPosition = null;

	public void Initialize(IDictionary<string, object> parameters)
	{
	}

	public override void _Input(InputEvent @event)
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
		_targetPosition = null;
		_player.Velocity = Vector2.Zero;
		_playerSprite.Play("idle_" + _lastDirection);
		
		_currentDialogueIndex = 0;
		_dialogueBox.Show();
		
		// Update prompt for mobile/touch
		var prompt = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/PromptLabel");
		prompt.Text = "[TAP TO CONTINUE]";
		
		UpdateDialogueUI();
	}

	private void NextDialogue()
	{
		_currentDialogueIndex++;
		if (_currentDialogueIndex >= _dialogue.Length)
		{
			_currentDialogueIndex = -1;
			_dialogueBox.Hide();
		}
		else
		{
			UpdateDialogueUI();
		}
	}

	private void UpdateDialogueUI()
	{
		_nameLabel.Text = "Old Man";
		_textLabel.Text = _dialogue[_currentDialogueIndex];
	}

	private async void TriggerEncounter()
	{
		GD.Print("[WorldScene] Encounter Triggered! Navigating to Battle.");
		await _navigation.NavigateToAsync("BattlePage");
	}
}
