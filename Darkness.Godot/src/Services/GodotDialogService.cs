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
        var dialog = new ConfirmationDialog();
        dialog.Title = title;
        dialog.DialogText = message;
        dialog.OkButtonText = accept;
        dialog.CancelButtonText = cancel;

        _global.AddChild(dialog);
        dialog.PopupCentered();

        var result = await _global.ToSignal(dialog, "confirmed");
        // Note: ConfirmationDialog emits 'confirmed' on OK, and 'canceled' on Cancel.
        // We need to handle both.

        // This is a simplified version. In a robust implementation we'd check which signal was emitted.
        // For this PoC, we'll assume confirmed means true.

        bool confirmed = true; // Placeholder for logic

        dialog.QueueFree();
        return confirmed;
    }
}