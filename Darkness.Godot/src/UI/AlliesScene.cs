using Godot;
using Darkness.Godot.Core;
using Darkness.Core.Interfaces;
using Darkness.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Darkness.Godot.UI;

public partial class AlliesScene : Control
{
    private IAllyService _allyService = null!;
    private ISessionService _session = null!;
    private INavigationService _navigation = null!;

    private VBoxContainer _pendingList = null!;
    private VBoxContainer _acceptedList = null!;

    public override async void _Ready()
    {
        if (!IsInsideTree()) return;
        var global = GetNode<Global>("/root/Global");
        _allyService = global.Services!.GetRequiredService<IAllyService>();
        _session = global.Services!.GetRequiredService<ISessionService>();
        _navigation = global.Services!.GetRequiredService<INavigationService>();

        _pendingList =
            GetNode<VBoxContainer>("ScrollContainer/MarginContainer/VBoxContainer/PendingSection/PendingList");
        _acceptedList =
            GetNode<VBoxContainer>("ScrollContainer/MarginContainer/VBoxContainer/AcceptedSection/AcceptedList");

        GetNode<Button>("ScrollContainer/MarginContainer/VBoxContainer/BackButton").Pressed +=
            () => _navigation.GoBackAsync();

        await RefreshAllies();
    }

    private async Task RefreshAllies()
    {
        if (_session.CurrentUser == null) return;

        var allies = await _allyService.GetAlliesForUserAsync(_session.CurrentUser.Id);

        foreach (Node child in _pendingList.GetChildren()) child.QueueFree();
        foreach (Node child in _acceptedList.GetChildren()) child.QueueFree();

        foreach (var ally in allies)
        {
            if (ally.Status.Equals("Pending", System.StringComparison.OrdinalIgnoreCase))
            {
                AddPendingAllyUI(ally);
            }
            else if (ally.Status.Equals("Accepted", System.StringComparison.OrdinalIgnoreCase))
            {
                AddAcceptedAllyUI(ally);
            }
        }
    }

    private void AddPendingAllyUI(Ally ally)
    {
        var hbox = new HBoxContainer();
        hbox.CustomMinimumSize = new Vector2(0, 100);
        hbox.AddThemeConstantOverride("separation", 20);

        var label = new Label { Text = ally.AllyUsername, SizeFlagsHorizontal = SizeFlags.ExpandFill };
        label.AddThemeFontSizeOverride("font_size", 28);
        hbox.AddChild(label);

        var acceptBtn = new Button { Text = "ACCEPT", CustomMinimumSize = new Vector2(150, 0) };
        acceptBtn.AddThemeFontSizeOverride("font_size", 24);
        acceptBtn.Pressed += () => RespondToRequest(ally, true);
        hbox.AddChild(acceptBtn);

        var declineBtn = new Button { Text = "DECLINE", CustomMinimumSize = new Vector2(150, 0) };
        declineBtn.AddThemeFontSizeOverride("font_size", 24);
        declineBtn.Pressed += () => RespondToRequest(ally, false);
        hbox.AddChild(declineBtn);

        _pendingList.AddChild(hbox);
    }

    private void AddAcceptedAllyUI(Ally ally)
    {
        var label = new Label
        {
            Text = ally.AllyUsername,
            CustomMinimumSize = new Vector2(0, 80)
        };
        label.AddThemeFontSizeOverride("font_size", 28);
        _acceptedList.AddChild(label);
    }

    private async void RespondToRequest(Ally ally, bool accept)
    {
        var success = await _allyService.RespondToAllyRequestAsync(ally.Id, accept);
        if (success)
        {
            await RefreshAllies();
        }
    }
}