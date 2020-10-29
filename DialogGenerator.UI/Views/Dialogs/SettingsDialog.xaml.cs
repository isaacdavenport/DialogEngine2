using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.UI.Wrapper;
using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Speech.Synthesis;
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
        private IEventAggregator mEventAggregator;


        public SettingsDialog(IEventAggregator _EventAggregator)
        {
            DataContext = this;

            WebsiteCommand = new DelegateCommand(_websiteCommand_Execute);
            CloseCommand = new DelegateCommand(_closeCommand_Execute);
            SelectBacgroundCommand = new DelegateCommand(_selectBackgroundImage_Execute);
            mEventAggregator = _EventAggregator;

            InitializeComponent();

            Settings = new ApplicationDataWrapper(ApplicationData.Instance);
            using(SpeechSynthesizer _speech = new SpeechSynthesizer())
            {
                foreach(var _voiceType in _speech.GetInstalledVoices())
                {
                    VoiceTypesCollection.Add(_voiceType.VoiceInfo.Name);
                }
            }
        }

        
        public ICommand WebsiteCommand { get; set; }
        public ICommand CloseCommand { get; set; }

        public ICommand SelectBacgroundCommand { get; set; }

        private void _websiteCommand_Execute()
        {
            Process.Start(ApplicationData.Instance.WebsiteUrl);
        }

        private void _closeCommand_Execute()
        {
            Settings.Model.Save();
            DialogHost.CloseDialogCommand.Execute(null,this.CloseBtn);
        }

        private void _selectBackgroundImage_Execute()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = ApplicationData.Instance.ImagesDirectory;
            if(openFileDialog.ShowDialog() == true)
            {
                Settings.BackgroundImage = openFileDialog.FileName;
                mEventAggregator.GetEvent<ArenaBackgroundChangedEvent>().Publish();
            }
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

        public ObservableCollection<string> VoiceTypesCollection { get; set; } = new ObservableCollection<string>();

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
