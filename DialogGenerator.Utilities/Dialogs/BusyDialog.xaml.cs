using System.ComponentModel;
using System.Windows.Controls;

namespace DialogGenerator.Utilities.Dialogs
{
    /// <summary>
    /// Interaction logic for BusyDialog.xaml
    /// </summary>
    public partial class BusyDialog : UserControl,INotifyPropertyChanged
    {
        private string mMessage;

        public BusyDialog(string message)
        {
            this.DataContext = this;
            InitializeComponent();

            Message = message;
        }

        public string Message
        {
            get { return mMessage; }
            set
            {
                mMessage = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        public  void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
