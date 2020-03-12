using DialogGenerator.Utilities;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DialogGenerator.UI.Controls
{
    /// <summary>
    /// Interaction logic for MediaRecorderControl.xaml
    /// </summary>
    public partial class MediaRecorderControl : UserControl
    {
        private string mFilePath;
        private NAudioEngine mSoundPlayer;

        public static readonly DependencyProperty PlayBtnVisibilityProperty = DependencyProperty.Register("PlayBtnVisibility", typeof(Visibility), typeof(MediaRecorderControl), new UIPropertyMetadata(Visibility.Collapsed));
        public static readonly DependencyProperty StopBtnVisibilityProperty = DependencyProperty.Register("StopBtnVisibility", typeof(Visibility), typeof(MediaRecorderControl), new UIPropertyMetadata(Visibility.Collapsed));
        public static readonly DependencyProperty StartRecordingBtnVisibilityProperty = DependencyProperty.Register("StartRecordingBtnVisibility", typeof(Visibility), typeof(MediaRecorderControl), new UIPropertyMetadata(Visibility.Collapsed));
        public static readonly DependencyProperty StopRecordingBtnVisibilityProperty = DependencyProperty.Register("StopRecordingBtnVisibility", typeof(Visibility), typeof(MediaRecorderControl), new UIPropertyMetadata(Visibility.Collapsed));
        public static readonly DependencyProperty IsPlayingProperty = DependencyProperty.Register("IsPlaying", typeof(bool), typeof(MediaRecorderControl), new UIPropertyMetadata(false));
        public static readonly DependencyProperty IsRecordingProperty = DependencyProperty.Register("IsRecording", typeof(bool), typeof(MediaRecorderControl), new UIPropertyMetadata(false));
        public static readonly DependencyProperty RecordingEnabledProperty = DependencyProperty.Register("RecordingEnabled", typeof(bool), typeof(MediaRecorderControl), new UIPropertyMetadata(true));

        public event PropertyChangedEventHandler PropertyChanged;

        public MediaRecorderControl()
        {
            InitializeComponent();
            this.DataContext = this;

            SoundPlayer = NAudioEngine.Instance;
            SoundPlayer.PropertyChanged += _soundPlayer_PropertyChanged;
            StartPlayingFileCommand = new DelegateCommand(_startPlayingFile_Execute, _startPlayingFile_CanExecute);
            StopPlayingFileCommand = new DelegateCommand(_stopPlayingFile_Execute);
            StartRecordingCommand = new DelegateCommand(_startRecording_Execute, _startRecording_CanExecute);
            StopRecordingCommand = new DelegateCommand(_stopRecordingExecute);

            PlayBtnVisibility = Visibility.Visible;
            StopBtnVisibility = Visibility.Collapsed;
            StartRecordingBtnVisibility = Visibility.Visible;
            StopRecordingBtnVisibility = Visibility.Collapsed;
            
        }

        #region Properties 

        public NAudioEngine SoundPlayer
        {
            get { return mSoundPlayer; }
            set
            {
                mSoundPlayer = value;
            }
        }

        public Visibility PlayBtnVisibility
        {
            get { return (Visibility)GetValue(PlayBtnVisibilityProperty); }
            set { SetValue(PlayBtnVisibilityProperty, value); }
        }

        public Visibility StopBtnVisibility
        {
            get { return (Visibility)GetValue(StopBtnVisibilityProperty); }
            set
            {
                SetValue(StopBtnVisibilityProperty, value);
            }
        }

        public Visibility StartRecordingBtnVisibility
        {
            get { return (Visibility)GetValue(StartRecordingBtnVisibilityProperty); }
            set { SetValue(StartRecordingBtnVisibilityProperty, value); }
        }

        public Visibility StopRecordingBtnVisibility
        {
            get { return (Visibility)GetValue(StopRecordingBtnVisibilityProperty); }
            set { SetValue(StopRecordingBtnVisibilityProperty, value); }
        }

        public bool IsPlaying
        {
            get { return (bool)GetValue(IsPlayingProperty); }
            set { SetValue(IsPlayingProperty, value); }
        }

        public bool IsRecording
        {
            get { return (bool)GetValue(IsRecordingProperty); }
            set { SetValue(IsRecordingProperty, value); }
        }

        public bool RecordingEnabled
        {
            get { return (bool)GetValue(RecordingEnabledProperty); }
            set
            {
                SetValue(RecordingEnabledProperty, value);
            }
        }

        public bool HasFileName
        {
            get
            {
                return !string.IsNullOrEmpty(this.FilePath);
            }
        }

        public string FilePath
        {
            get { return mFilePath; }
            set
            {
                mFilePath = value;
                OnPropertyChanged(nameof(HasFileName));
                ((DelegateCommand)StartPlayingFileCommand)?.RaiseCanExecuteChanged();
                ((DelegateCommand)StartRecordingCommand)?.RaiseCanExecuteChanged();
            }
        }

        public ICommand StartPlayingFileCommand { get; set; }
        public ICommand StopPlayingFileCommand { get; set; }     
        public ICommand StartRecordingCommand { get; set; }
        public ICommand StopRecordingCommand { get; set; }

        #endregion


        #region Public methods

        public void OnPropertyChanged(string _propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(_propertyName));
        }

        #endregion

        #region Private methods

        private void _soundPlayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SoundPlayer.IsPlaying):
                case nameof(SoundPlayer.IsRecording):
                    {
                        Dispatcher.Invoke(() => {
                            PlayBtnVisibility = SoundPlayer.IsPlaying || SoundPlayer.IsRecording ? Visibility.Collapsed : Visibility.Visible;
                            StopBtnVisibility = SoundPlayer.IsPlaying && !SoundPlayer.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                            StartRecordingBtnVisibility = SoundPlayer.IsPlaying || SoundPlayer.IsRecording ? Visibility.Collapsed : Visibility.Visible;
                            StopRecordingBtnVisibility = !SoundPlayer.IsPlaying && SoundPlayer.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                            IsPlaying = SoundPlayer.IsPlaying;
                            IsRecording = SoundPlayer.IsRecording;
                        });                        
                        break;
                    }                
            }
        }

        private bool _startPlayingFile_CanExecute()
        {
            return !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);
        }

        private void _startPlayingFile_Execute()
        {
            SoundPlayer.OpenFile(FilePath);
            SoundPlayer.Play();
        }

        private void _stopPlayingFile_Execute()
        {
            if (SoundPlayer.CanStop)
                SoundPlayer.Stop();
        }

        private void _stopRecordingExecute()
        {
            SoundPlayer.StopRecording();
        }

        private bool _startRecording_CanExecute()
        {
            return !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);
        }

        private void _startRecording_Execute()
        {
            if(RecordingEnabled)
            {
                SoundPlayer.StartRecording(FilePath);
            }
            
        }

        #endregion
    }
}
