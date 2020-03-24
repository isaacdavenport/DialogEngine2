using DialogGenerator.Events;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DialogGenerator.UI.ViewModels
{
    public class MediaRecorderControlViewModel : BindableBase
    {
        private string mFilePath;
        NAudioEngine mSoundPlayer;
        IMessageDialogService mMessageDialogService;
        IEventAggregator mEventAggregator;
        bool mIsPlaying;
        bool mIsRecording;
        bool mRecordingEnabled;
        Visibility mPlayBtnVisibility;
        Visibility mStopBtnVisibility;
        Visibility mStartRecordingBtnVisibility;
        Visibility mStopRecordingBtnVisibility;
        SpeechRecognitionEngine mSpeechRecognizer;


        public MediaRecorderControlViewModel(NAudioEngine _Player
            ,IMessageDialogService _MessageDialogService
            ,IEventAggregator _EventAggregator)
        {
            SoundPlayer = _Player;
            SoundPlayer.PropertyChanged += SoundPlayer_PropertyChanged;
            mMessageDialogService = _MessageDialogService;
            mEventAggregator = _EventAggregator;

            RecordingEnabled = true;
            IsPlaying = false;
            IsRecording = false;

            PlayBtnVisibility = Visibility.Visible;
            StopBtnVisibility = Visibility.Collapsed;
            StartRecordingBtnVisibility = Visibility.Visible;
            StopRecordingBtnVisibility = Visibility.Collapsed;

            _bindCommands();
        }        

        #region properties

        public NAudioEngine SoundPlayer
        {
            get { return mSoundPlayer; }
            set
            {
                mSoundPlayer = value;
                RaisePropertyChanged();
            }
        }

        public bool IsPlaying
        {
            get
            {
                return mIsPlaying;
            }

            set
            {
                mIsPlaying = value;
                RaisePropertyChanged();
            }
        }

        public bool IsRecording
        {
            get
            {
                return mIsRecording;
            }

            set
            {
                mIsRecording = value;
                RaisePropertyChanged();
            }
        }

        public bool RecordingEnabled
        {
            get
            {
                return mRecordingEnabled;
            }

            set
            {
                mRecordingEnabled = value;
                RaisePropertyChanged();
            }
        }

        public Visibility PlayBtnVisibility
        {
            get
            {
                return mPlayBtnVisibility;
            } 

            set
            {
                mPlayBtnVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility StopBtnVisibility
        {
            get
            {
                return mStopBtnVisibility;
            }

            set
            {
                mStopBtnVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility StartRecordingBtnVisibility
        {
            get
            {
                return mStartRecordingBtnVisibility;
            }

            set
            {
                mStartRecordingBtnVisibility = value;
                RaisePropertyChanged();
            }
        }

        public Visibility StopRecordingBtnVisibility
        {
            get
            {
                return mStopRecordingBtnVisibility;
            }

            set
            {
                mStopRecordingBtnVisibility = value;
                RaisePropertyChanged();
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
                RaisePropertyChanged(nameof(HasFileName));
                ((DelegateCommand)StartPlayingFileCommand)?.RaiseCanExecuteChanged();
                ((DelegateCommand)StartRecordingCommand)?.RaiseCanExecuteChanged();                
            }
        }

        #endregion

        #region commands 

        public DelegateCommand StartPlayingFileCommand { get; set; }
        public DelegateCommand StopPlayingFileCommand { get; set; }
        public DelegateCommand StartRecordingCommand { get; set; }
        public DelegateCommand StopRecordingCommand { get; set; }
        public DelegateCommand LoadedCommand { get; set; }
        public DelegateCommand UnloadedCommand { get; set; }

        #endregion

        #region Event handlers and private methods

        private void SoundPlayer_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SoundPlayer.IsPlaying):
                case nameof(SoundPlayer.IsRecording):
                    {                        
                        PlayBtnVisibility = SoundPlayer.IsPlaying || SoundPlayer.IsRecording ? Visibility.Collapsed : Visibility.Visible;
                        StopBtnVisibility = SoundPlayer.IsPlaying && !SoundPlayer.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                        StartRecordingBtnVisibility = SoundPlayer.IsPlaying || SoundPlayer.IsRecording ? Visibility.Collapsed : Visibility.Visible;
                        StopRecordingBtnVisibility = !SoundPlayer.IsPlaying && SoundPlayer.IsRecording ? Visibility.Visible : Visibility.Collapsed;
                        IsPlaying = SoundPlayer.IsPlaying;
                        IsRecording = SoundPlayer.IsRecording;                        
                        break;
                    }
            }
        }

        private void _bindCommands()
        {
            StartPlayingFileCommand = new DelegateCommand(_startPlayingCommand_Execute, _startPlayingCommand_CanExecute);
            StopPlayingFileCommand = new DelegateCommand(_stopPlayingCommand_Execute);
            StartRecordingCommand = new DelegateCommand(_startRecordingCommand_Execute, _startRecordingCommand_CanExecute);
            StopRecordingCommand = new DelegateCommand(_stopRecordingCommand_Execute);
            LoadedCommand = new DelegateCommand(_onLoaded_Execute);
            UnloadedCommand = new DelegateCommand(_onUnloadedExecute);
        }

        private void _onUnloadedExecute()
        {
            mSpeechRecognizer.SpeechRecognized -= MSpeechRecognizer_SpeechRecognized;
        }

        private void _onLoaded_Execute()
        {
            mSpeechRecognizer = new SpeechRecognitionEngine(System.Globalization.CultureInfo.GetCultureInfo("en-US"));
            mSpeechRecognizer.LoadGrammar(new DictationGrammar());
            mSpeechRecognizer.SetInputToDefaultAudioDevice();
            mSpeechRecognizer.SpeechRecognized += MSpeechRecognizer_SpeechRecognized;
            //mSpeechRecognizer.AudioLevelUpdated += MSpeechRecognizer_AudioLevelUpdated;
        }

        private void MSpeechRecognizer_AudioLevelUpdated(object sender, AudioLevelUpdatedEventArgs e)
        {            
            Console.WriteLine("Audio level is {0}", e.AudioLevel);
        }

        private void MSpeechRecognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            mEventAggregator.GetEvent<SpeechConvertedEvent>().Publish(e.Result.Text);
        }

        private bool _startPlayingCommand_CanExecute()
        {
            return !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);
        }

        private void _startPlayingCommand_Execute()
        {
            SoundPlayer.OpenFile(FilePath);
            SoundPlayer.Play();
        }

        private void _stopPlayingCommand_Execute()
        {
            if (SoundPlayer.CanStop)
                SoundPlayer.Stop();
        }

        private void _startRecordingCommand_Execute()
        {
            if (RecordingEnabled)
            {
                mSpeechRecognizer.RecognizeAsync(RecognizeMode.Multiple);
                SoundPlayer.StartRecording(FilePath);
            } else
            {
                mEventAggregator.GetEvent<RequestTranslationEvent>().Publish(this.GetType().Name);
            }
        }

        private bool _startRecordingCommand_CanExecute()
        {
            return !string.IsNullOrEmpty(FilePath) && File.Exists(FilePath);
        }

        private void _stopRecordingCommand_Execute()
        {
            SoundPlayer.StopRecording();
            mSpeechRecognizer.RecognizeAsyncStop();
        }


        #endregion
    }
}
