using DialogGenerator.Core;
using DialogGenerator.UI.Wrapper;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

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
            CloseCommand = new DelegateCommand(_closeCommand_Execute);

            InitializeComponent();

            Settings = new ApplicationDataWrapper(ApplicationData.Instance);
        }

        public ICommand CloseCommand { get; set; }

        private void _closeCommand_Execute()
        {
            Settings.Model.Save();
            DialogHost.CloseDialogCommand.Execute(null,this.CloseBtn);
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
