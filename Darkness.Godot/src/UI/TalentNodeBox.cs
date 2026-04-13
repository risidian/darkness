using Godot;
using Darkness.Core.Models;
using System;

namespace Darkness.Godot.UI;

public partial class TalentNodeBox : PanelContainer
{
    [Signal]
    public delegate void PurchaseRequestedEventHandler(string talentId);

    private TextureButton _iconButton = null!;
    private Label _nameLabel = null!;
    private Label _statusLabel = null!;
    
    private TalentNode? _node;
    private bool _isUnlocked;
    private bool _canPurchase;

    public override void _Ready()
    {
        _iconButton = GetNode<TextureButton>("MarginContainer/VBoxContainer/Icon");
        _nameLabel = GetNode<Label>("MarginContainer/VBoxContainer/NameLabel");
        _statusLabel = GetNode<Label>("MarginContainer/VBoxContainer/StatusLabel");

        _iconButton.Pressed += OnIconButtonPressed;
    }

    public void SetTalent(TalentNode node, bool isUnlocked, bool canPurchase)
    {
        _node = node;
        _isUnlocked = isUnlocked;
        _canPurchase = canPurchase;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_node == null) return;

        _nameLabel.Text = _node.Name;
        TooltipText = _node.Description;
        
        string iconPath = !string.IsNullOrEmpty(_node.IconPath) ? _node.IconPath : "res://icon.svg";
        _iconButton.TextureNormal = GD.Load<Texture2D>(iconPath);
        
        if (_isUnlocked)
        {
            _statusLabel.Text = "Unlocked";
            _iconButton.Disabled = true;
            _iconButton.Modulate = new Color(1, 1, 1, 1); // Normal
            SelfModulate = new Color(0.2f, 0.8f, 0.2f, 1); // Greenish background for unlocked
        }
        else if (_canPurchase)
        {
            _statusLabel.Text = $"Available (0/{_node.PointsRequired})";
            _iconButton.Disabled = false;
            _iconButton.Modulate = new Color(1, 1, 1, 1); // Normal
            SelfModulate = new Color(0.8f, 0.8f, 0.2f, 1); // Yellowish background for available
        }
        else
        {
            _statusLabel.Text = "Locked";
            _iconButton.Disabled = true;
            _iconButton.Modulate = new Color(0.5f, 0.5f, 0.5f, 1); // Greyed out
            SelfModulate = new Color(0.3f, 0.3f, 0.3f, 1); // Greyish background for locked
        }
    }

    private void OnIconButtonPressed()
    {
        if (_node != null && !_isUnlocked && _canPurchase)
        {
            EmitSignal(SignalName.PurchaseRequested, _node.Id);
        }
    }
}
