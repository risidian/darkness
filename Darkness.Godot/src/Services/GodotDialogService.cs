using Darkness.Core.Interfaces;
using Darkness.Godot.Core;
using Godot;
using System.Threading.Tasks;

namespace Darkness.Godot.Services;

public class GodotDialogService : IDialogService
{
    private readonly Global _global;

    public GodotDialogService(Global global)
    {
        _global = global;
    }

    public async Task DisplayAlertAsync(string title, string message, string cancel)
    {
        var dialog = new AcceptDialog();
        dialog.Title = title;
        dialog.DialogText = message;
        dialog.OkButtonText = cancel;

        _global.AddChild(dialog);
        dialog.PopupCentered();

        await _global.ToSignal(dialog, "confirmed");
        dialog.QueueFree();
    }

    public async Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel)
    {
        var tcs = new TaskCompletionSource<bool>();
        var dialog = new ConfirmationDialog();
        dialog.Title = title;
        dialog.DialogText = message;
        dialog.OkButtonText = accept;
        dialog.CancelButtonText = cancel;

        dialog.Confirmed += () => tcs.TrySetResult(true);
        dialog.Canceled += () => tcs.TrySetResult(false);

        _global.AddChild(dialog);
        dialog.PopupCentered();

        var confirmed = await tcs.Task;
        dialog.QueueFree();
        return confirmed;
    }
}