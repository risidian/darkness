using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;

namespace Darkness.Godot.Game;

public partial class WorldScene : Node2D, IInitializable
{
    private INavigationService _navigation;
    private ISessionService _session;
    private CharacterBody2D _player;
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
        _navigation = global.Services.GetRequiredService<INavigationService>();
        _session = global.Services.GetRequiredService<ISessionService>();

        _player = GetNode<CharacterBody2D>("Player");
        _dialogueBox = GetNode<PanelContainer>("CanvasLayer/DialogueBox");
        _nameLabel = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/NameLabel");
        _textLabel = GetNode<Label>("CanvasLayer/DialogueBox/VBoxContainer/TextLabel");

        GetNode<Area2D>("NPC").BodyEntered += (body) => {
            if (body == _player) StartDialogue();
        };
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

    private void TriggerEncounter()
    {
        // Navigate to Battle
        _navigation.NavigateToAsync("BattlePage");
    }
}
