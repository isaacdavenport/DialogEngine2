using DialogGenerator.UI.View.Dialogs;
using GalaSoft.MvvmLight.Threading;
using MaterialDesignThemes.Wpf;
using System;

namespace DialogGenerator.UI.View.Services
{
    public enum MessageDialogResult
    {
        OK,
        Cancel
    }

    public class MessageDialogService : IMessageDialogService
    {
        public MessageDialogResult ShowOKCancelDialog(string message, string tittle, string _OKBtnContent = "OK", string _cancelBtnContent = "Cancel")
        {
            MessageDialogResult result = MessageDialogResult.Cancel;

            DispatcherHelper.CheckBeginInvokeOnUI(async() =>
            {
                result =(MessageDialogResult) await DialogHost.Show(new OKCancelDialog(message, tittle, _OKBtnContent, _cancelBtnContent));
            });

            return result;
        }

        public MessageDialogResult ShowDedicatedDialog(string _dialogType)
        {
            MessageDialogResult result = MessageDialogResult.Cancel;

            DispatcherHelper.CheckBeginInvokeOnUI(async () =>
            {
                Type type = Type.GetType(_dialogType); 
                result = (MessageDialogResult) await DialogHost.Show(Activator.CreateInstance(type));
            });

            return result;
        }
    }
}
