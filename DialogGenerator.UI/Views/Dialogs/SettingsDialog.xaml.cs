using DialogGenerator.Core;
using DialogGenerator.UI.Wrapper;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
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

            WebsiteCommand = new DelegateCommand(_websiteCommand_Execute);
            CloseCommand = new DelegateCommand(_closeCommand_Execute);

            InitializeComponent();

            Settings = new ApplicationDataWrapper(ApplicationData.Instance);
        }

        public ICommand WebsiteCommand { get; set; }
        public ICommand CloseCommand { get; set; }

        private void _websiteCommand_Execute()
        {
            Process.Start(ApplicationData.Instance.WebsiteUrl);
        }

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

        public string Website
        {
            get { return ApplicationData.Instance.WebsiteUrl; }
        }

        public string Version
        {
            get { return $"v: { FileVersionInfo.GetVersionInfo(Path.Combine(ApplicationData.Instance.RootDirectory, "DialogGenerator.exe")).FileVersion.ToString()}"; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
