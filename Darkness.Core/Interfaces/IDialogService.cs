namespace Darkness.Core.Interfaces
{
    public interface IDialogService
    {
        Task DisplayAlertAsync(string title, string message, string cancel);
        Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel);
    }
}
