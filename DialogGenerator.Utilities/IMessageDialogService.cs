using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DialogGenerator.Utilities
{
    public interface IMessageDialogService
    {
        Task<MessageDialogResult> ShowMessage(string tittle, string message, string _dialogHostName = "MainDialogHost");
        Task<MessageDialogResult> ShowOKCancelDialogAsync(string message, string tittle
            , string _OKBtnContent = "OK"
            ,string _cancelBtnContent = "Cancel"
            , string _dialogHostName = "MainDialogHost");
        Task<T> ShowDedicatedDialogAsync<T>(object content, string _dialogHostName = "MainDialogHost");
        Task<MessageDialogResult> ShowMessagesDialogAsync(string tittle, string message, IList<string> messages
            , string _OkBtnContent = "OK"
            , bool _isOKCancel = true
            , string _cancelBtnContent = "Cancel"
            , string _dialogHostName = "MainDialogHost");
        Task<MessageDialogResult> ShowBusyDialog(string message = "Working ...", string _dialogHostName = "MainDialogHost");
        void CloseBusyDialog();
        Task<MessageDialogResult> ShowExpirationDialogAsync(TimeSpan _exprationTime, string message
            , string tittle 
            , string _okBtnContent = "Continue"
            , string _cancelBtnContent = "Cancel"
            , string _dialogHostName = "MainDialogHost");
    }
}