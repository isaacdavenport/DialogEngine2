using System.Threading.Tasks;

namespace DialogGenerator.UI.View.Services
{
    public interface IMessageDialogService
    {
        MessageDialogResult ShowOKCancelDialog(string message, string tittle, string _OKBtnContent = "OK", string _cancelBtnContent = "Cancel");
        MessageDialogResult ShowDedicatedDialog(string _dialogType);
    }
}