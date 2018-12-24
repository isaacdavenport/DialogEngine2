using DialogGenerator.Utilities.Dialogs;
using MaterialDesignThemes.Wpf;
using System;
using System.Collections.Generic;
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
        private BusyDialog mBusyDialog = new BusyDialog("");

        public async Task<MessageDialogResult> ShowOKCancelDialogAsync(string message, string tittle
            , string _OKBtnContent = "OK"
            , string _cancelBtnContent = "Cancel"
            ,string _dialogHostName = "MainDialogHost")
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

        public async Task<MessageDialogResult> ShowMessage(string tittle, string message,string _dialogHostName = "MainDialogHost")
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                await DialogHost.Show(new MessageDialog(tittle, message), _dialogHostName);
            }
            else
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    await DialogHost.Show(new MessageDialog(tittle, message), _dialogHostName);
                });
            }

            return MessageDialogResult.OK;
        }

        public async Task<MessageDialogResult> ShowBusyDialog(string message= "Working ...", string _dialogHostName = "MainDialogHost")
        {

            if (Application.Current.Dispatcher.CheckAccess())
            {
                mBusyDialog.Message = message;
                await DialogHost.Show(mBusyDialog, _dialogHostName);
            }
            else
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    mBusyDialog.Message = message;
                    await DialogHost.Show(mBusyDialog, _dialogHostName);
                });
            }

            return MessageDialogResult.OK;
        }

        public  void CloseBusyDialog()
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                DialogHost.CloseDialogCommand.Execute(null, mBusyDialog);
            }
            else
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DialogHost.CloseDialogCommand.Execute(null, mBusyDialog);

                });
            }
        }

        public async Task<MessageDialogResult> ShowMessagesDialogAsync(string tittle, string message
            , IList<string> messages
            , string _OkBtnContent = "OK"
            , bool _isOKCancel = true
            , string _cancelBtnContent = "Cancel"
            , string _dialogHostName = "MainDialogHost")
        {
            MessageDialogResult result = MessageDialogResult.Cancel;

            if (Application.Current.Dispatcher.CheckAccess())
            {
                result = (MessageDialogResult)await DialogHost
                    .Show(new MessagesDialog(tittle,message,messages,_OkBtnContent,_isOKCancel,_cancelBtnContent),_dialogHostName);
            }
            else
            {
                await Application.Current.Dispatcher.Invoke(async() =>
                {
                   result = (MessageDialogResult)await DialogHost
                    .Show(new MessagesDialog(tittle, message, messages, _OkBtnContent,_isOKCancel, _cancelBtnContent), _dialogHostName);
                });
            }

            return result;
        }

        public async Task<MessageDialogResult> ShowExpirationDialogAsync(TimeSpan _exprationTime, string message
            , string tittle
            , string _okBtnContent = "Continue"
            , string _cancelBtnContent = "Cancel"
            , string _dialogHostName = "MainDialogHost")
        {
            MessageDialogResult result = MessageDialogResult.Cancel;

            if (Application.Current.Dispatcher.CheckAccess())
            {
                result = (MessageDialogResult)await DialogHost
                    .Show(new ExpirationDialog(_exprationTime, message, tittle,_okBtnContent, _cancelBtnContent), _dialogHostName);
            }
            else
            {
                await Application.Current.Dispatcher.Invoke(async () =>
                {
                    result = (MessageDialogResult)await DialogHost
                     .Show(new ExpirationDialog(_exprationTime, message, tittle, _okBtnContent, _cancelBtnContent), _dialogHostName);
                });
            }

            return result;
        }
    }
}
