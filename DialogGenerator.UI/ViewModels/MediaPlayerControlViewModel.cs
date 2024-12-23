﻿using DialogGenerator.Core;
using DialogGenerator.UI.Workflow.VideoPlayerStateMachine;
using DialogGenerator.UI.Workflow.WizardWorkflow;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.IO;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class MediaPlayerControlViewModel:BindableBase
    {

        #region - fields -

        private string mCurrentVideoFilePath;
        private ILogger mLogger;
        private WizardWorkflow mWizardWorkflow;
        public event EventHandler PlayRequested;
        public event EventHandler PauseRequested;
        public event EventHandler StopRequested;
        public event EventHandler ShiftForwardRequested;
        public event EventHandler ShiftBackwardsRequested;

        #endregion

        #region - constructor -

        public MediaPlayerControlViewModel(WizardWorkflow _wizardWorkflow, ILogger _Logger)
        {
            StateMachine = new VideoPlayerStateMachine(() => { });
            mWizardWorkflow = _wizardWorkflow;
            mLogger = _Logger;

            this.PropertyChanged += _mediaPlayerControlViewModel_PropertyChanged;
            mWizardWorkflow.PropertyChanged += _mWizardWorkflow_PropertyChanged;
            StateMachine.PropertyChanged += _stateMachine_PropertyChanged;

            _configureStateMachine();
            _bindCommands();
        }

        #endregion
        
        public void LogMessage(int _MessageType, string _Message)
        {
            switch(_MessageType)
            {
                case 0:
                    mLogger.Error(_Message);
                    break;
                case 1:
                    mLogger.Info(_Message);
                    break;
                default:
                    mLogger.Debug(_Message);
                    break;
            }
        }

        #region - event handlers -

        private void _stateMachine_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(StateMachine.State)))
            {
                ((DelegateCommand)StartVideoCommand).RaiseCanExecuteChanged();
                ((DelegateCommand)PauseVideoCommand).RaiseCanExecuteChanged();
            }
        }

        private void _mWizardWorkflow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(mWizardWorkflow.State)))
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
                            ((DelegateCommand)StartVideoCommand).RaiseCanExecuteChanged();
                            break;
                        }
                }
            }
        }

        private void _mediaPlayerControlViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentVideoFilePath):
                    {
                        ((DelegateCommand)StartVideoCommand).RaiseCanExecuteChanged();                        
                        break;
                    }
            }
        }

        #endregion

        #region - commands-

        /// <summary>
        /// Starts video
        /// </summary>
        public ICommand StartVideoCommand { get; set; }

        /// <summary>
        /// Stops video
        /// </summary>
        public ICommand PauseVideoCommand { get; set; }

        public ICommand PlayInContextCommand { get; set; }

        public ICommand StopPlayingInContextCommand { get; set; }

        public DelegateCommand ShiftForwardCommand { get; set; }
        public DelegateCommand ShiftBackwardCommand { get; set; }

        #endregion

        #region - private functions -

        private void _configureStateMachine()
        {
            StateMachine.Configure(States.Idle)
                .Permit(Triggers.On, States.Ready);

            StateMachine.Configure(States.Ready)
                .Permit(Triggers.Off, States.Idle)
                .Permit(Triggers.Play, States.Playing);

            StateMachine.Configure(States.Playing)
                .Permit(Triggers.On,States.Ready);
        }

        private void _stopPlaying()
        {
            StopRequested(this, EventArgs.Empty);
        }

        private void _bindCommands()
        {
            StartVideoCommand = new DelegateCommand(_startMediaPlayer_Execute,_startMediaPlayer_CanExecute);
            PauseVideoCommand = new DelegateCommand(_pauseMediaPlayer_Execute,_pauseMediaPlayer_CanExecute);
            PlayInContextCommand = new DelegateCommand(_playInContext_Execute, _playInContext_CanExecute);
            StopPlayingInContextCommand = new DelegateCommand(_stopPlayingInContext_Execute, _stopPlayingInContext_CanExecute);
            ShiftForwardCommand = new DelegateCommand(_shiftForwardCommand_Execute, _shiftForwardCommand_CanExecute);
            ShiftBackwardCommand = new DelegateCommand(_shiftBackwardCommand_Execute, _shiftBackwardCommand_CanExecute);
        }

        private bool _shiftBackwardCommand_CanExecute()
        {
            return StateMachine.State == States.Playing;
        }

        private void _shiftBackwardCommand_Execute()
        {
            ShiftBackwardsRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool _shiftForwardCommand_CanExecute()
        {
            return StateMachine.State == States.Playing;
        }

        private void _shiftForwardCommand_Execute()
        {
            ShiftForwardRequested?.Invoke(this, EventArgs.Empty);
        }

        private bool _stopPlayingInContext_CanExecute()
        {
            return StateMachine.State == States.Playing
                   && mWizardWorkflow.State == WizardStates.PlayingInContext;
        }

        private void _stopPlayingInContext_Execute()
        {
            StopRequested(this, EventArgs.Empty);
        }

        private void _playInContext_Execute()
        {
            _startMediaPlayer_Execute();
        }

        private bool _playInContext_CanExecute()
        {
            return File.Exists(CurrentVideoFilePath);
        }

        private void _startMediaPlayer_Execute()
        {
            PlayRequested(this, EventArgs.Empty);
            StateMachine.Fire(Triggers.Play);
            ShiftBackwardCommand.RaiseCanExecuteChanged();
            ShiftForwardCommand.RaiseCanExecuteChanged();
        }

        private bool _startMediaPlayer_CanExecute()
        {
            return StateMachine.State == States.Ready
                   && mWizardWorkflow.State != WizardStates.PlayingInContext
                   && !string.IsNullOrEmpty(CurrentVideoFilePath)
                   && File.Exists(Path.Combine(ApplicationData.Instance.VideoDirectory,CurrentVideoFilePath));
        }

        private bool _pauseMediaPlayer_CanExecute()
        {
            return StateMachine.State == States.Playing
                   && mWizardWorkflow.State != WizardStates.PlayingInContext;
        }

        private void _pauseMediaPlayer_Execute()
        {
            PauseRequested(this, EventArgs.Empty);
            StateMachine.Fire(Triggers.On);
            ShiftBackwardCommand.RaiseCanExecuteChanged();
            ShiftForwardCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region - properties -

        public VideoPlayerStateMachine StateMachine { get; set; }

        public string CurrentVideoFilePath
        {
            get { return mCurrentVideoFilePath; }
            set
            {
                mCurrentVideoFilePath = value;
                RaisePropertyChanged();
            }
        }

        #endregion
    }
}
