using System.Threading.Tasks;

namespace DialogGenerator.UI.Views.Services
{
    public interface IMessageDialogService
    {
        Task ShowMessage(string tittle, string message, string _dialogHostName = "MainDialogHost");
        Task<MessageDialogResult> ShowOKCancelDialogAsync(string message, string tittle, string _OKBtnContent = "OK",
            string _cancelBtnContent = "Cancel", string _dialogHostName = "MainDialogHost");
        Task<T> ShowDedicatedDialogAsync<T>(object content, string _dialogHostName = "MainDialogHost");
    }
}