using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Godot.Game;

public partial class WorldScene : Node2D, IInitializable
{
    private INavigationService _navigation;
    private ISessionService _session;
    private ISpriteCompositor _compositor;
    private ISpriteLayerCatalog _catalog;
    private IFileSystemService _fileSystem;

    private CharacterBody2D _player;
    private Sprite2D _playerSprite;
    private Sprite2D _npcSprite;
    private PanelContainer _dialogueBox;
    private Label _nameLabel;
    private Label _textLabel;

    private float _moveSpeed = 300f;
    private string[] _dialogue = new[] {
        "Welcome to the Shore of Camelot, Wanderer.",
        "The path to the castle is blocked by shadows.",
        "You'll find only hounds and darkness to the east."
    };
    private int _currentDialogueIndex = -1;

    public void Initialize(IDictionary<string, object> parameters)
    {
        // Handle parameters
    }

    public override void _Ready()
    {
        var global = GetNode<Global>("/root/Global");
        var sp = global.Services!;
        _navigation = sp.GetRequiredService<INavigationService>();
        _session = sp.GetRequiredService<ISessionService>();
        _compositor = sp.GetRequiredService<ISpriteCompositor>();
        _catalog = sp.GetRequiredService<ISpriteLayerCatalog>();
        _fileSystem = sp.GetRequiredService<IFileSystemService>();

        _player = GetNode<CharacterBody2D>("Player");
        _playerSprite = GetNode<Sprite2D>("Player/Sprite");
        _npcSprite = GetNode<Sprite2D>("NPC/Sprite");
        
        _dialogueBox = GetNode<PanelContainer>("CanvasLayer/DialogueBox");
        _nameLabel = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/NameLabel");
        _textLabel = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/TextLabel");

        GetNode<Button>("CanvasLayer/TopMenu/ForgeButton").Pressed += () => _navigation.NavigateToAsync("ForgePage");
        GetNode<Button>("CanvasLayer/TopMenu/StudyButton").Pressed += () => _navigation.NavigateToAsync("StudyPage");
        GetNode<Button>("CanvasLayer/TopMenu/AlliesButton").Pressed += () => _navigation.NavigateToAsync("AlliesPage");
        GetNode<Button>("CanvasLayer/TopMenu/DeathmatchButton").Pressed += () => _navigation.NavigateToAsync("DeathmatchPage");

        GetNode<Area2D>("NPC").BodyEntered += (body) => {
            if (body == _player) StartDialogue();
        };

        UpdateSprites();
    }

    private async void UpdateSprites()
    {
        // 1. Update Player Sprite
        if (_session.CurrentCharacter != null)
        {
            await LoadCharacterSprite(_session.CurrentCharacter, _playerSprite);
        }

        // 2. Update NPC Sprite (Basic Knight)
        var knightAppearance = _catalog.GetDefaultAppearanceForClass("Knight");
        await LoadCharacterSprite(new Character { 
            SkinColor = knightAppearance.SkinColor,
            HairStyle = knightAppearance.HairStyle,
            HairColor = knightAppearance.HairColor,
            ArmorType = knightAppearance.ArmorType,
            WeaponType = knightAppearance.WeaponType,
            Face = "Default",
            Eyes = "Default"
        }, _npcSprite);
    }

    private async Task LoadCharacterSprite(Character c, Sprite2D spriteNode)
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
                var frameBytes = _compositor.ExtractFrame(sheetBytes, 0, 2 * 64, 64, 64, 1);

                var img = new Image();
                img.LoadPngFromBuffer(frameBytes);
                spriteNode.Texture = ImageTexture.CreateFromImage(img);
            }
            
            foreach (var s in streams) s.Dispose();
        }
        catch (System.Exception ex)
        {
            GD.PrintErr($"[WorldScene] Failed to load sprite: {ex.Message}");
        }
    }

    public override void _Process(double delta)
    {
        if (_currentDialogueIndex >= 0)
        {
            if (Input.IsActionJustPressed("ui_accept"))
            {
                NextDialogue();
            }
            return;
        }

        Vector2 velocity = Vector2.Zero;
        if (Input.IsActionPressed("ui_left")) velocity.X -= 1;
        if (Input.IsActionPressed("ui_right")) velocity.X += 1;
        if (Input.IsActionPressed("ui_up")) velocity.Y -= 1;
        if (Input.IsActionPressed("ui_down")) velocity.Y += 1;

        if (velocity != Vector2.Zero)
        {
            _player.Velocity = velocity.Normalized() * _moveSpeed;
            _player.MoveAndSlide();
        }

        // Check for exit trigger
        if (_player.GlobalPosition.X > 1200)
        {
            TriggerEncounter();
        }
    }

    private void StartDialogue()
    {
        _currentDialogueIndex = 0;
        _dialogueBox.Show();
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
        // Navigate to Battle
        await _navigation.NavigateToAsync("BattlePage");
    }
}
