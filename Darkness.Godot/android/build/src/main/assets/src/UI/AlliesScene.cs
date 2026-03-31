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
	private IAllyService _allyService;
	private ISessionService _session;
	private INavigationService _navigation;

	private VBoxContainer _pendingList;
	private VBoxContainer _acceptedList;

	public override async void _Ready()
	{
		var global = GetNode<Global>("/root/Global");
		_allyService = global.Services!.GetRequiredService<IAllyService>();
		_session = global.Services!.GetRequiredService<ISessionService>();
		_navigation = global.Services!.GetRequiredService<INavigationService>();

		_pendingList = GetNode<VBoxContainer>("VBoxContainer/PendingSection/PendingList");
		_acceptedList = GetNode<VBoxContainer>("VBoxContainer/AcceptedSection/AcceptedList");

		GetNode<Button>("VBoxContainer/BackButton").Pressed += () => _navigation.GoBackAsync();

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
		hbox.AddChild(new Label { Text = ally.AllyUsername, SizeFlagsHorizontal = SizeFlags.ExpandFill });
		
		var acceptBtn = new Button { Text = "Accept" };
		acceptBtn.Pressed += () => RespondToRequest(ally, true);
		hbox.AddChild(acceptBtn);

		var declineBtn = new Button { Text = "Decline" };
		declineBtn.Pressed += () => RespondToRequest(ally, false);
		hbox.AddChild(declineBtn);

		_pendingList.AddChild(hbox);
	}

	private void AddAcceptedAllyUI(Ally ally)
	{
		var label = new Label { Text = ally.AllyUsername };
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
