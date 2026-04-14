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
    private Tooltip _tooltip = null!;

    private TalentNode? _node;    private bool _isUnlocked;
    private bool _canPurchase;
    private string? _failureReason;

    public override void _Ready()
    {
        _iconButton = GetNode<TextureButton>("MarginContainer/VBoxContainer/Icon");
        _nameLabel = GetNode<Label>("MarginContainer/VBoxContainer/NameLabel");
        _statusLabel = GetNode<Label>("MarginContainer/VBoxContainer/StatusLabel");

        var tooltipScene = GD.Load<PackedScene>("res://scenes/UI/Tooltip.tscn");
        _tooltip = tooltipScene.Instantiate<Tooltip>();
        AddChild(_tooltip);

        _iconButton.Pressed += OnIconButtonPressed;
    }

    public void SetTalent(TalentNode node, bool isUnlocked, TalentPurchaseResult purchaseResult)
    {
        _node = node;
        _isUnlocked = isUnlocked;
        _canPurchase = purchaseResult.Success;
        _failureReason = purchaseResult.FailureReason;

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (_node == null) return;

        _nameLabel.Text = _node.Name;
        
        if (_isUnlocked)
        {
            TooltipText = "";
            TooltipHelper.Bind(this, _node.Description, _tooltip);
            _statusLabel.Text = "Unlocked";
            _iconButton.Disabled = true;
            _iconButton.Modulate = new Color(1, 1, 1, 1); // Normal
            SelfModulate = new Color(0.2f, 0.8f, 0.2f, 1); // Greenish background for unlocked
        }
        else if (_canPurchase)
        {
            TooltipText = "";
            TooltipHelper.Bind(this, _node.Description, _tooltip);
            _statusLabel.Text = $"Available (0/{_node.PointsRequired})";
            _iconButton.Disabled = false;
            _iconButton.Modulate = new Color(1, 1, 1, 1); // Normal
            SelfModulate = new Color(0.8f, 0.8f, 0.2f, 1); // Yellowish background for available
        }
        else
        {
            TooltipText = "";
            TooltipHelper.Bind(this, $"{_node.Description}\n\n[LOCKED]\n{_failureReason}", _tooltip);
            _statusLabel.Text = "Locked";
            _iconButton.Disabled = true;
            _iconButton.Modulate = new Color(0.5f, 0.5f, 0.5f, 1); // Greyed out
            SelfModulate = new Color(0.3f, 0.3f, 0.3f, 1); // Greyish background for locked
        }

        string iconPath = !string.IsNullOrEmpty(_node.IconPath) ? _node.IconPath : "res://icon.svg";
        _iconButton.TextureNormal = GD.Load<Texture2D>(iconPath);
    }

    private void OnIconButtonPressed()
    {
        if (_node != null && !_isUnlocked && _canPurchase)
        {
            EmitSignal(SignalName.PurchaseRequested, _node.Id);
        }
    }
}
