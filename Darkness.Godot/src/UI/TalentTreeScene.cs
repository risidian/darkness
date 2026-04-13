using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Linq;

namespace Darkness.Godot.UI;

public partial class TalentTreeScene : Control
{
    private ITalentService _talentService = null!;
    private ISessionService _session = null!;
    private INavigationService _navigation = null!;
    private IDialogService _dialogService = null!;
    private ICharacterService _characterService = null!;

    private Label _pointsLabel = null!;
    private TabContainer _tabContainer = null!;
    private Button _backButton = null!;

    public override void _Ready()
    {
        if (!IsInsideTree()) return;
        var global = GetNode<Global>("/root/Global");
        _talentService = global.Services!.GetRequiredService<ITalentService>();
        _session = global.Services!.GetRequiredService<ISessionService>();
        _navigation = global.Services!.GetRequiredService<INavigationService>();
        _dialogService = global.Services!.GetRequiredService<IDialogService>();
        _characterService = global.Services!.GetRequiredService<ICharacterService>();

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

            var grid = new GridContainer
            {
                Columns = 1,
                SizeFlagsHorizontal = Control.SizeFlags.ExpandFill
            };
            grid.AddThemeConstantOverride("v_separation", 10);

            scroll.AddChild(grid);
            _tabContainer.AddChild(scroll);

            foreach (var node in tree.Nodes)
            {
                var hBox = new HBoxContainer();
                grid.AddChild(hBox);

                var isUnlocked = _session.CurrentCharacter.UnlockedTalentIds.Contains(node.Id);
                var canPurchase = _talentService.CanPurchaseTalent(_session.CurrentCharacter, tree.Id, node.Id);

                var button = new Button
                {
                    Text = node.Name,
                    CustomMinimumSize = new Vector2(250, 60),
                    Disabled = isUnlocked || !canPurchase,
                    TooltipText = node.Description
                };

                if (isUnlocked)
                {
                    button.Text += " (Unlocked)";
                }

                button.Pressed += () => OnNodePressed(tree.Id, node);
                hBox.AddChild(button);

                var descLabel = new Label
                {
                    Text = node.Description,
                    SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
                    AutowrapMode = TextServer.AutowrapMode.WordSmart
                };
                hBox.AddChild(descLabel);
            }
        }
    }

    private async void OnNodePressed(string treeId, TalentNode node)
    {
        if (_session.CurrentCharacter == null) return;

        var confirmed = await _dialogService.DisplayConfirmAsync("Purchase Talent", $"Unlock {node.Name} for {node.PointsRequired} point(s)?", "Unlock", "Cancel");
        if (confirmed)
        {
            _talentService.PurchaseTalent(_session.CurrentCharacter, treeId, node.Id);
            _talentService.ApplyTalentPassives(_session.CurrentCharacter);
            await _characterService.SaveCharacterAsync(_session.CurrentCharacter);
            
            UpdatePointsLabel();
            LoadTrees();
        }
    }
}
