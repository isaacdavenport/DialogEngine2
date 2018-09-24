using System.Windows.Controls;

namespace DialogGenerator.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for MessageDialog.xaml
    /// </summary>
    public partial class MessageDialog : UserControl
    {
        public MessageDialog(string tittle,string message)
        {
            InitializeComponent();
            this.DataContext = this;

            Tittle = tittle;
            Message = message;
        }

        public string Message { get; set; }
        public string Tittle { get; set; }
    }
}
