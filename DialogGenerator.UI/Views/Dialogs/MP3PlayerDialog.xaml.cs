using DialogGenerator.Utilities;
using MaterialDesignThemes.Wpf;
using Prism.Commands;
using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace DialogGenerator.UI.Views.Dialogs
{
    /// <summary>
    /// Interaction logic for MP3PlayerDialog.xaml
    /// </summary>
    public partial class MP3PlayerDialog : UserControl,INotifyPropertyChanged
    {
        #region - fields -

        private string mFilePath;
        private NAudioEngine mSoundPlayer;
        private Visibility mStopBtnVisibility = Visibility.Collapsed;
        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region - ctor -

        public MP3PlayerDialog(string _filePath)
        {
            InitializeComponent();
            this.DataContext = this;

            SoundPlayer = NAudioEngine.Instance;
            SoundPlayer.PropertyChanged += _soundPlayer_PropertyChanged;
            StartPlayingCommand = new DelegateCommand(_startPlaying_Execute,_startPlaying_CanExecute);
            StopPlayingCommand = new DelegateCommand(_stopPlaying_Execute);
            CloseDialogCommand = new DelegateCommand(_closeDialog_Execute);
            FilePath = _filePath;
        }

        #endregion

        #region - commands - 

        public ICommand StartPlayingCommand { get; set; }
        public ICommand StopPlayingCommand { get; set; }
        public ICommand CloseDialogCommand { get; set; }

        #endregion

        #region - event handlers -

        private void _soundPlayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SoundPlayer.IsPlaying):
                    {
                        StopBtnVisibility = SoundPlayer.IsPlaying ? Visibility.Visible : Visibility.Collapsed;
                        break;
                    }
            }
        }

        #endregion

        #region - private methods -

        private void _stopPlaying_Execute()
        {
            if(SoundPlayer.CanStop)
                SoundPlayer.Stop();
        }

        private bool _startPlaying_CanExecute()
        {
            return !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);
        }

        private void _startPlaying_Execute()
        {
            SoundPlayer.OpenFile(FilePath);
            SoundPlayer.Play();
        }

        private void _closeDialog_Execute()
        {
            if (SoundPlayer.CanStop)
            {
                SoundPlayer.Stop();
            }

            DialogHost.CloseDialogCommand.Execute(null, CloseDialogBtn);
        }

        #endregion

        #region - public methods -

        public void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }

        #endregion

        #region - properties -

        public NAudioEngine SoundPlayer
        {
            get { return mSoundPlayer; }
            set
            {
                mSoundPlayer = value;
            }
        }

        public Visibility StopBtnVisibility
        {
            get { return mStopBtnVisibility; }
            set
            {
                mStopBtnVisibility = value;
                OnPropertyChanged(nameof(StopBtnVisibility));
            }
        }

        public string FilePath
        {
            get { return mFilePath; }
            set
            {
                mFilePath = value;
                ((DelegateCommand)StartPlayingCommand)?.RaiseCanExecuteChanged();
            }
        }

        #endregion
    }
}
