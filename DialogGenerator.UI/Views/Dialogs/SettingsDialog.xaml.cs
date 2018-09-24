using DialogGenerator.Core;
using DialogGenerator.UI.Wrapper;
using System.ComponentModel;
using System.Windows.Controls;

namespace DialogGenerator.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for SettingsDialog.xaml
    /// </summary>
    public partial class SettingsDialog : UserControl,INotifyPropertyChanged
    {
        private ApplicationDataWrapper mSettings;

        public SettingsDialog()
        {
            DataContext = this;
            InitializeComponent();

            Settings = new ApplicationDataWrapper(ApplicationData.Instance);
        }

        public virtual void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }

        public ApplicationDataWrapper Settings
        {
            get { return mSettings; }
            set
            {
                mSettings = value;
                OnPropertyChanged("Settings");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
