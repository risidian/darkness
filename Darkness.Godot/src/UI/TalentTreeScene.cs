using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Darkness.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace Darkness.Godot.UI;

public partial class TalentTreeScene : Control
{
    private const int ROW_GAP = 160;
    private const int COL_GAP = 200;
    private const int CANVAS_WIDTH = 600; // 3 columns * 200
    private const int NODE_WIDTH = 180;
    private const int NODE_HEIGHT = 120;

    private ITalentService _talentService = null!;
    private ISessionService _session = null!;
    private INavigationService _navigation = null!;
    private IDialogService _dialogService = null!;
    private ICharacterService _characterService = null!;

    private Label _pointsLabel = null!;
    private TabContainer _tabContainer = null!;
    private Button _backButton = null!;

    private PackedScene _nodeBoxScene = null!;

    public override void _Ready()
    {
        if (!IsInsideTree()) return;
        var global = GetNode<Global>("/root/Global");
        _talentService = global.Services!.GetRequiredService<ITalentService>();
        _session = global.Services!.GetRequiredService<ISessionService>();
        _navigation = global.Services!.GetRequiredService<INavigationService>();
        _dialogService = global.Services!.GetRequiredService<IDialogService>();
        _characterService = global.Services!.GetRequiredService<ICharacterService>();

        _nodeBoxScene = GD.Load<PackedScene>("res://scenes/UI/TalentNodeBox.tscn");

        _pointsLabel = GetNode<Label>("MarginContainer/VBoxContainer/PointsLabel");
        _tabContainer = GetNode<TabContainer>("MarginContainer/VBoxContainer/TabContainer");
        _backButton = GetNode<Button>("MarginContainer/VBoxContainer/BackButton");

        _backButton.Pressed += () => _navigation.GoBackAsync();

        UpdatePointsLabel();
        LoadTrees();
    }

    private void UpdatePointsLabel()
    {
        if (_session.CurrentCharacter != null)
        {
            _pointsLabel.Text = $"Points Available: {_session.CurrentCharacter.TalentPoints}";
        }
    }

    private void LoadTrees()
    {
        if (_session.CurrentCharacter == null) return;

        int currentTab = _tabContainer.CurrentTab;

        // Clear existing tabs
        foreach (Node child in _tabContainer.GetChildren())
        {
            _tabContainer.RemoveChild(child);
            child.QueueFree();
        }

        var availableTrees = _talentService.GetAvailableTrees(_session.CurrentCharacter);

        foreach (var tree in availableTrees)
        {
            var scroll = new ScrollContainer
            {
                Name = tree.Name,
                SizeFlagsVertical = Control.SizeFlags.ExpandFill,
                HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled
            };

            var treeCanvas = new Control
            {
                CustomMinimumSize = new Vector2(CANVAS_WIDTH, 800),
                SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter | Control.SizeFlags.Expand
            };
            
            scroll.AddChild(treeCanvas);
            _tabContainer.AddChild(scroll);

            // Calculate Layout
            TalentLayoutHelper.CalculateLayout(tree.Nodes);

            // Draw Lines First (so they are behind nodes)
            DrawConnections(treeCanvas, tree.Nodes, tree.Id);

            // Create Nodes
            foreach (var node in tree.Nodes)
            {
                var nodeBox = _nodeBoxScene.Instantiate<TalentNodeBox>();
                treeCanvas.AddChild(nodeBox);

                var isUnlocked = _session.CurrentCharacter.UnlockedTalentIds.Contains(node.Id);
                var purchaseResult = _talentService.CanPurchaseTalent(_session.CurrentCharacter, tree.Id, node.Id);

                nodeBox.SetTalent(node, isUnlocked, purchaseResult);
                
                // Position node
                float x = node.Column * COL_GAP + (COL_GAP - NODE_WIDTH) / 2f;
                float y = node.Row * ROW_GAP + 50; // Top margin
                nodeBox.Position = new Vector2(x, y);

                nodeBox.PurchaseRequested += (talentId) => OnNodePressed(tree.Id, node);
            }

            // Update canvas height based on max row
            int maxRow = tree.Nodes.Any() ? tree.Nodes.Max(n => n.Row) : 0;
            treeCanvas.CustomMinimumSize = new Vector2(CANVAS_WIDTH, (maxRow + 1) * ROW_GAP + 100);
        }

        if (currentTab >= 0 && currentTab < _tabContainer.GetTabCount())
        {
            _tabContainer.CurrentTab = currentTab;
        }
    }

    private void DrawConnections(Control canvas, List<TalentNode> nodes, string treeId)
    {
        var nodeDict = nodes.ToDictionary(n => n.Id);

        foreach (var node in nodes)
        {
            foreach (var prereqId in node.PrerequisiteNodeIds)
            {
                if (nodeDict.TryGetValue(prereqId, out var parent))
                {
                    var line = new Line2D
                    {
                        Width = 4,
                        DefaultColor = new Color(0.3f, 0.3f, 0.3f, 1) // Gray line (Locked)
                    };

                    // Parent center (bottom)
                    float px = parent.Column * COL_GAP + COL_GAP / 2f;
                    float py = parent.Row * ROW_GAP + 50 + NODE_HEIGHT;

                    // Child center (top)
                    float cx = node.Column * COL_GAP + COL_GAP / 2f;
                    float cy = node.Row * ROW_GAP + 50;

                    line.AddPoint(new Vector2(px, py));
                    line.AddPoint(new Vector2(cx, cy));

                    // Color logic
                    bool childUnlocked = _session.CurrentCharacter!.UnlockedTalentIds.Contains(node.Id);
                    
                    if (childUnlocked)
                    {
                        line.DefaultColor = new Color(0.2f, 0.8f, 0.2f, 1); // Green (Unlocked)
                    }
                    else
                    {
                        var purchaseResult = _talentService.CanPurchaseTalent(_session.CurrentCharacter, treeId, node.Id);
                        if (purchaseResult.Success)
                        {
                            line.DefaultColor = new Color(1.0f, 0.9f, 0.2f, 1); // Yellow (Available)
                        }
                    }

                    canvas.AddChild(line);
                }
            }
        }
    }

    private async void OnNodePressed(string treeId, TalentNode node)
    {
        if (_session.CurrentCharacter == null) return;

        var confirmed = await _dialogService.DisplayConfirmAsync("Purchase Talent", $"Unlock {node.Name} for {node.PointsRequired} point(s)?", "Unlock", "Cancel");
        if (confirmed)
        {
            var result = _talentService.CanPurchaseTalent(_session.CurrentCharacter, treeId, node.Id);
            if (result.Success)
            {
                _talentService.PurchaseTalent(_session.CurrentCharacter, treeId, node.Id);
                _talentService.ApplyTalentPassives(_session.CurrentCharacter);
                await _characterService.SaveCharacterAsync(_session.CurrentCharacter);
                
                UpdatePointsLabel();
                LoadTrees();
            }
            else
            {
                await _dialogService.DisplayAlertAsync("Purchase Failed", result.FailureReason ?? "Unknown error", "OK");
            }
        }
    }
}
