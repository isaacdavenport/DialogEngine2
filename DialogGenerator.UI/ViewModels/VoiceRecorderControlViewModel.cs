using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.UI.Workflow.MP3RecorderStateMachine;
using DialogGenerator.UI.Workflow.WizardWorkflow;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using System;
using System.ComponentModel;
using System.IO;
using System.Speech.Recognition;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public enum LoggingTypes
    {
        Error,
        Info,
        Debug,
    }

    public class VoiceRecorderControlViewModel : BindableBase
    {
        #region - fields -

        private IMessageDialogService mMessageDialogService;
        private TimeSpan mMaxTimeForRecording = TimeSpan.FromMinutes(5);
        private DateTime mRecordingStartedTime;
        private string mCurrentFilePath;
        private NAudioEngine mSoundPlayer;
        private WizardWorkflow mWizardWorkflow;
        private readonly Timer mTimer;
        private bool mEnableRecording = true;
        private IEventAggregator mEventAggregator;
        private SpeechRecognitionEngine mSoundRecognizer;
        private ILogger mLogger;

        #endregion

        #region - contructor -

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="player">Instance of <see cref="ISoundPlayer"/></param>
        public VoiceRecorderControlViewModel(NAudioEngine player
                                            , WizardWorkflow _wizardWorkflow
                                            , IMessageDialogService _messageDialogService
                                            , IEventAggregator _EventAggregator
                                            , ILogger _Logger)
        {
            SoundPlayer = player;
            StateMachine = new MP3RecorderStateMachine(() => { });
            WizardWorkflow = _wizardWorkflow;
            mMessageDialogService = _messageDialogService;
            mEventAggregator = _EventAggregator;
            mTimer = new Timer(_timer_Elapsed, null, Timeout.Infinite, Timeout.Infinite);
            mLogger = _Logger;

            StateMachine.PropertyChanged += _stateMachine_PropertyChanged;
            mWizardWorkflow.PropertyChanged += _mWizardWorkflow_PropertyChanged;
            this.PropertyChanged += _voiceRecorderControlViewModel_PropertyChanged;

            _configureStateMachine();
            _bindCommands();
        }

        #endregion

        #region - properties -

        public bool EnableRecording
        {
            get
            {
                return mEnableRecording;
            }

            set
            {
                mEnableRecording = value;
                RaisePropertyChanged();
            }
        }

        public MP3RecorderStateMachine StateMachine { get; set; }

        public WizardWorkflow WizardWorkflow
        {
            get { return mWizardWorkflow; }
            set
            {
                mWizardWorkflow = value;
            }
        }

        public NAudioEngine SoundPlayer
        {
            get { return mSoundPlayer; }
            set
            {
                mSoundPlayer = value;
                RaisePropertyChanged();
            }
        }

        /// <summary>
        /// Path of last recorded .mp3 file
        /// </summary>
        public string CurrentFilePath
        {
            get { return mCurrentFilePath; }
            set
            {
                mCurrentFilePath = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region - commands -State

        public DelegateCommand StartRecordingCommand { get; set; }
        public DelegateCommand StopRecorderCommand { get; set; }
        public DelegateCommand StartPlayingCommand { get; set; }
        public DelegateCommand PlayInContextCommand { get; set; }
        public DelegateCommand StopPlayingInContextCommand { get; set; }
        public DelegateCommand LoadedCommand { get; set; }
        public DelegateCommand UnloadedCommand { get; set; }

        #endregion

        #region - event handlers-
        private void _stateMachine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(StateMachine.State)))
            {
                ((DelegateCommand)StartPlayingCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)StartRecordingCommand).RaiseCanExecuteChanged();
            }
        }

        private void _soundPlayer_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(SoundPlayer.IsPlaying):
                    {
                        if (SoundPlayer.IsPlaying)
                        {
                            if (StateMachine.CanFire(Triggers.Play))
                                StateMachine.Fire(Triggers.Play);
                        }
                        else
                        {
                            if (StateMachine.CanFire(Triggers.On))
                                StateMachine.Fire(Triggers.On);
                        }
                        break;
                    }
            }
        }

        private void _mWizardWorkflow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (mWizardWorkflow.State)
            {
                case WizardStates.WaitingForUserAction:
                    {
                        if (StateMachine.CanFire(Triggers.On))
                            StateMachine.Fire(Triggers.On);

                        ((DelegateCommand)StartPlayingCommand).RaiseCanExecuteChanged();
                        ((DelegateCommand)StartRecordingCommand).RaiseCanExecuteChanged();
                        break;
                    }
                case WizardStates.UserActionStarted:
                    {
                        if (StateMachine.State == States.Ready)
                            StateMachine.Fire(Triggers.Off);

                        break;
                    }
                case WizardStates.PlayingInContext:
                    {
                        ((DelegateCommand)StartPlayingCommand).RaiseCanExecuteChanged();
                        ((DelegateCommand)StartRecordingCommand).RaiseCanExecuteChanged();
                        break;
                    }
            }
        }

        private void _voiceRecorderControlViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentFilePath):
                    {
                        ((DelegateCommand)StartPlayingCommand).RaiseCanExecuteChanged();
                        ((DelegateCommand)StartRecordingCommand).RaiseCanExecuteChanged();
                        break;
                    }
            }
        }

        private async void _timer_Elapsed(object state)
        {
            if((DateTime.Now - mRecordingStartedTime)> mMaxTimeForRecording)
            {
                mTimer.Change(Timeout.Infinite, Timeout.Infinite);

                MessageDialogResult result = await mMessageDialogService
                    .ShowExpirationDialogAsync(TimeSpan.FromSeconds(10),"Recording will be stopped in ","Warning","Continue recording");

                if(result == MessageDialogResult.Cancel)
                {
                    await Application.Current.Dispatcher.BeginInvoke((Action)(() =>
                    {
                        _stopRecorder_Execute();
                    }));
                }
                else
                {
                    mTimer.Change(0, 1000);
                    mRecordingStartedTime = DateTime.Now;
                }
            }
        }

        public void Log(LoggingTypes type, string message)
        {
            switch(type)
            {
                case LoggingTypes.Error:
                    mLogger.Error(message);
                    break;
                case LoggingTypes.Info:
                    mLogger.Info(message);
                    break;
                default:
                    mLogger.Debug(message);
                    break;                        
            }
        }
        
        #endregion

        #region - private functions -

        private void _loadSoundEngine()
        {
            mSoundRecognizer = new SpeechRecognitionEngine(System.Globalization.CultureInfo.GetCultureInfo("en-US"));
            mSoundRecognizer.LoadGrammar(new DictationGrammar());
            mSoundRecognizer.SetInputToDefaultAudioDevice();
            mSoundRecognizer.SpeechRecognized += MSoundRecognizer_SpeechRecognized;
        }

        private void _unloadSoundEngine()
        {
            mSoundRecognizer.SpeechRecognized -= MSoundRecognizer_SpeechRecognized;
        }

        private void MSoundRecognizer_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            mEventAggregator.GetEvent<SpeechConvertedEvent>().Publish(e.Result.Text);
        }
       
        private void _configureStateMachine()
        {
            StateMachine.Configure(States.Idle)
                .Permit(Triggers.On, States.Ready);

            StateMachine.Configure(States.Ready)
                .Permit(Triggers.Off, States.Idle)
                .Permit(Triggers.Record, States.Recording)
                .Permit(Triggers.Play, States.Playing);

            StateMachine.Configure(States.Stopped)
                .Permit(Triggers.On, States.Ready);

            StateMachine.Configure(States.Recording)
                .OnActivate(() => _startRecording())
                .Permit(Triggers.On, States.Ready);

            StateMachine.Configure(States.Playing)
                .OnActivate(() => _startPlaying())
                .Permit(Triggers.On,States.Ready)
                .Permit(Triggers.Stop, States.Stopped);
        }

        private void _bindCommands()
        {
            StartRecordingCommand = new DelegateCommand(_startRecording_Execute,_startRecording_CanExecute);
            StartPlayingCommand = new DelegateCommand(_startPlaying_Execute, _startPlaying_CanExecute);
            StopRecorderCommand = new DelegateCommand(_stopRecorder_Execute);
            PlayInContextCommand = new DelegateCommand(_playInContext_Execute, _playInContext_CanExecute);
            StopPlayingInContextCommand = new DelegateCommand(_stopPlayingInContext_Execute,_stopPlayingInContext_CanExecute);
            LoadedCommand = new DelegateCommand(_loaded_Execute);
            UnloadedCommand = new DelegateCommand(_unloaded_Execute);
        }

        private void _unloaded_Execute()
        {            
            _unloadSoundEngine();
            mSoundPlayer.PropertyChanged -= _soundPlayer_PropertyChanged;
        }

        private void _loaded_Execute()
        {
            _loadSoundEngine();
            mSoundPlayer.PropertyChanged += _soundPlayer_PropertyChanged;
        }

        private bool _playInContext_CanExecute()
        {
            return  StateMachine.State == States.Ready
                    && !string.IsNullOrEmpty(CurrentFilePath)
                    && File.Exists(Path.Combine(ApplicationData.Instance.AudioDirectory, CurrentFilePath + ".mp3"));
        }

        private bool _stopPlayingInContext_CanExecute()
        {
            return StateMachine.State == States.Playing
                   && WizardWorkflow.State == WizardStates.PlayingInContext;
        }

        private void _stopPlayingInContext_Execute()
        {
            if (mSoundPlayer.CanStop)
                mSoundPlayer.Stop();
        }

        private void _playInContext_Execute()
        {
            _startPlaying_Execute();
        }

        private void _startPlaying()
        {
            try
            {
                mSoundPlayer.OpenFile(Path.Combine(ApplicationData.Instance.AudioDirectory, CurrentFilePath + ".mp3"));
                mSoundPlayer.Play();
            } catch(Exception e)
            {
                //MessageBox.Show(e.Message, "Playback Error", MessageBoxButton.OK, MessageBoxImage.Error);
                mLogger.Error("Voice recorder exception - (m)_startPlaying " + e.Message);
            }
            
        }

        private void _startRecording()
        {
            try
            {
                mSoundPlayer.StartRecording(Path.Combine(ApplicationData.Instance.AudioDirectory, CurrentFilePath + ".mp3"));
                mRecordingStartedTime = DateTime.Now;
                mTimer.Change(0, 1000);
            } catch (Exception e)
            {
                //MessageBox.Show(e.Message, "Recording Error", MessageBoxButton.OK, MessageBoxImage.Error);
                mLogger.Error("Voice recorder exception - (m)_startRecording " + e.Message);
            }
                   
        }

        private void _startPlaying_Execute()
        {
            try
            {
                StateMachine.Fire(Triggers.Play);
            }
            catch (Exception e)
            {
                //MessageBox.Show(e.Message, "Start Playing Error", MessageBoxButton.OK, MessageBoxImage.Error);
                mLogger.Error("Voice recorder exception - (m)_startPlaying_Execute " + e.Message);
            }
            
        }

        private bool _startPlaying_CanExecute()
        {            
            return StateMachine.State == States.Ready
                   && mWizardWorkflow.State != WizardStates.PlayingInContext
                   && !string.IsNullOrEmpty(CurrentFilePath)
                   && File.Exists(Path.Combine(ApplicationData.Instance.AudioDirectory, CurrentFilePath + ".mp3"));
        }

        private void _startRecording_Execute()
        { 
            try
            {
                if (EnableRecording)
                {
                    StateMachine.Fire(Triggers.Record);
                    mSoundRecognizer.RecognizeAsync(RecognizeMode.Multiple);
                }
                else
                {
                    mEventAggregator.GetEvent<RequestTranslationEvent>().Publish(this.GetType().Name);
                }
            } catch (Exception e)
            {
                //MessageBox.Show(e.Message, "Start Recording Error", MessageBoxButton.OK, MessageBoxImage.Error);
                mLogger.Error("Voice recorder exception - (m)_startRecording_Execute " + e.Message);
            }
                        
        }

        private bool _startRecording_CanExecute()
        {
            return StateMachine.State == States.Ready
                   && mWizardWorkflow.State != WizardStates.PlayingInContext
                   && !string.IsNullOrEmpty(CurrentFilePath);
        }

        private void _stopRecorder_Execute()
        {
            switch (StateMachine.State)
            {
                case States.Recording:
                    {
                        mTimer.Change(Timeout.Infinite, Timeout.Infinite);
                        mSoundPlayer.StopRecording();                                                    
                        StateMachine.Fire(Triggers.On);
                        mSoundRecognizer.RecognizeAsyncStop();
                        break;
                    }
                case States.Playing:
                    {
                        if (mSoundPlayer.CanStop)
                            mSoundPlayer.Stop();

                        if (StateMachine.CanFire(Triggers.Stop))
                            StateMachine.Fire(Triggers.Stop);
                        break;
                    }
            }
        }

        #endregion

        
    }
}
