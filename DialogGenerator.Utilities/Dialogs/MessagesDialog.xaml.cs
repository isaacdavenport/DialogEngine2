using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace DialogGenerator.Utilities.Dialogs
{
    /// <summary>
    /// Interaction logic for MessagesDialog.xaml
    /// </summary>
    public partial class MessagesDialog : UserControl
    {
        public MessagesDialog(string tittle,string message, IList<string> messages,string _okBtnContent,bool _isOKCancelBtn, string _cancelBtnContent)
        {
            InitializeComponent();
            DataContext = this;

            Tittle = tittle;
            Message = message;
            Messages = messages;
            OkBtnContent = _okBtnContent;
            CancelBtnContent = _cancelBtnContent;
            IsCancelBtnVisible = _isOKCancelBtn ? Visibility.Visible : Visibility.Collapsed;
        }

        public string Tittle { get; set; }
        public string Message { get; set; }
        public IList<string> Messages { get; set; }
        public string OkBtnContent { get; set; }
        public string CancelBtnContent { get; set; }
        public Visibility IsCancelBtnVisible { get; set; }
    }
}
