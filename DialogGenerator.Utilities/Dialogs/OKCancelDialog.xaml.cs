using System.Windows.Controls;

namespace DialogGenerator.Utilities.Dialogs
{
    /// <summary>
    /// Interaction logic for OKCancelDialog.xaml
    /// </summary>
    public partial class OKCancelDialog : UserControl
    {
        public OKCancelDialog(string message, string tittle, string _okBtnContent, string _cancelBtnContent)
        {
            InitializeComponent();
            DataContext = this;

            Message = message;
            Tittle = tittle;
            OKBtnContent = _okBtnContent;
            CancelBtnContent = _cancelBtnContent;
        }

        public string Tittle { get; set; }
        public string Message { get; set; }
        public string OKBtnContent { get; set; }
        public string CancelBtnContent { get; set; }
    }
}
