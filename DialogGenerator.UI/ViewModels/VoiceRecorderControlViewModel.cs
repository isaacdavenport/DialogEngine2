using DialogGenerator.Core;
using DialogGenerator.UI.Workflow.MP3RecorderStateMachine;
using DialogGenerator.UI.Workflow.WizardWorkflow;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Mvvm;
using System.ComponentModel;
using System.IO;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class VoiceRecorderControlViewModel : BindableBase
    {
        #region - fields -

        private string mCurrentFilePath;
        private NAudioEngine mSoundPlayer;
        private WizardWorkflow mWizardWorkflow;

        #endregion

        #region - contructor -

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="player">Instance of <see cref="ISoundPlayer"/></param>
        public VoiceRecorderControlViewModel(NAudioEngine player, WizardWorkflow _wizardWorkflow)
        {
            SoundPlayer = player;
            StateMachine = new MP3RecorderStateMachine(() => { });
            WizardWorkflow = _wizardWorkflow;

            StateMachine.PropertyChanged += _stateMachine_PropertyChanged;
            mWizardWorkflow.PropertyChanged += _mWizardWorkflow_PropertyChanged;
            mSoundPlayer.PropertyChanged += _soundPlayer_PropertyChanged;
            this.PropertyChanged += _voiceRecorderControlViewModel_PropertyChanged;

            _configureStateMachine();
            _bindCommands();
        }

        #endregion

        #region - commands -State

        public ICommand StartRecordingCommand { get; set; }
        public ICommand StopRecorderCommand { get; set; }
        public ICommand StartPlayingCommand { get; set; }
        public ICommand PlayInContextCommand { get; set; }
        public ICommand StopPlayingInContextCommand { get; set; }

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

        #endregion

        #region - private functions -

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
            mSoundPlayer.OpenFile(Path.Combine(ApplicationData.Instance.AudioDirectory, CurrentFilePath + ".mp3"));
            mSoundPlayer.Play();
        }

        private void _startRecording()
        {
            mSoundPlayer.StartRecording(Path.Combine(ApplicationData.Instance.AudioDirectory, CurrentFilePath + ".mp3"));
        }

        private void _startPlaying_Execute()
        {
            StateMachine.Fire(Triggers.Play);
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
            StateMachine.Fire(Triggers.Record);
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
                        mSoundPlayer.StopRecording();
                        StateMachine.Fire(Triggers.On);
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

        #region - properties -

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
    }
}
