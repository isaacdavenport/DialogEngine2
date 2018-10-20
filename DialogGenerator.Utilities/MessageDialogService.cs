using DialogGenerator.Utilities.Dialogs;
using MaterialDesignThemes.Wpf;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace DialogGenerator.Utilities
{
    public enum MessageDialogResult
    {
        OK,
        Cancel
    }

    public class MessageDialogService : IMessageDialogService
    {
        public async Task<MessageDialogResult> ShowOKCancelDialogAsync(string message, string tittle,
            string _OKBtnContent = "OK", string _cancelBtnContent = "Cancel",string _dialogHostName = "MainDialogHost")
        {
            MessageDialogResult result = MessageDialogResult.Cancel;

            if (Application.Current.Dispatcher.CheckAccess())
            {
                result = (MessageDialogResult)await DialogHost.
                    Show(new OKCancelDialog(message, tittle, _OKBtnContent, _cancelBtnContent),_dialogHostName);
            }
            else
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    result = (MessageDialogResult)await DialogHost.
                        Show(new OKCancelDialog(message, tittle, _OKBtnContent, _cancelBtnContent),_dialogHostName);
                });
            }

            return result;
        }

        public async Task<T> ShowDedicatedDialogAsync<T>(object content,string _dialogHostName = "MainDialogHost")
        {
            T result = Activator.CreateInstance<T>();

            if (Application.Current.Dispatcher.CheckAccess())
            {
                result = (T)await DialogHost.Show(content,_dialogHostName);
            }
            else
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    result = (T)await DialogHost.Show(content,_dialogHostName);
                });
            }

            return result;
        }

        public async Task ShowMessage(string tittle, string message,string _dialogHostName = "MainDialogHost")
        {
            var dialog = new MessageDialog(tittle, message);

            if (Application.Current.Dispatcher.CheckAccess())
            {
                await DialogHost.Show(dialog,_dialogHostName);
            }
            else
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    await DialogHost.Show(dialog,_dialogHostName);
                });
            }
        }
    }
}
