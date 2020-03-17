﻿using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Views;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.UI.Workflow.CreateCharacterWorkflow;
using DialogGenerator.UI.Workflow.WizardWorkflow;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Speech.Synthesis;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace DialogGenerator.UI.ViewModels
{
    public class WizardViewModel : BindableBase
    {
        #region - fields -

        #region - constants-

        // "PlayUserRecordedAudioInContext" : [
        //          [ "WZ_SkylarGreeting1", "USR_CurrentLine" ],
        //          [ "USR_CurrentLine", "WZ_BjornGreeting1" ]
        //       ]
        private const string mcCurrentLineName = "USR_CurrentLine"; 

        #endregion

        private ILogger mLogger;
        private IUserLogger mUserLogger;
        private IWizardDataProvider mWizardDataProvider;
        private IMessageDialogService mMessageDialogService;
        private ICharacterDataProvider mCharacterDataProvider;
        private IRegionManager mRegionManager;
        private IEventAggregator mEventAggregator;
        private int mCurrentStepIndex;
        private string mDialogStr;
        private WizardFormDialog mWizardFormDialog;
        private MediaPlayerControlViewModel mMediaPlayerControlViewModel;
        private VoiceRecorderControlViewModel mVoiceRecorderControlViewModel;
        private Character mCharacter;
        private Wizard mCurrentWizard;
        private TutorialStep mCurrentTutorialStep;
        private CancellationTokenSource mCancellationTokenSource;
        private bool mIsFinished = false;

        #endregion

        #region - constructor -

        public WizardViewModel(ILogger logger,IUserLogger _userLogger
            ,IEventAggregator _eventAggregator
            ,IWizardDataProvider _wizardDataProvider
            ,ICharacterDataProvider _characterDataProvider
            ,IMessageDialogService _messageDialogService
            ,WizardFormDialog _wizardFormDialog
            ,IRegionManager _regionManager)
        {
            mLogger = logger;
            mUserLogger = _userLogger;
            mWizardDataProvider = _wizardDataProvider;
            mMessageDialogService = _messageDialogService;
            mWizardFormDialog = _wizardFormDialog;
            mCharacterDataProvider = _characterDataProvider;
            mRegionManager = _regionManager;
            mEventAggregator = _eventAggregator;

            Workflow = new WizardWorkflow(action: () => { });
            MediaPlayerControlViewModel = new MediaPlayerControlViewModel(Workflow);
            VoiceRecorderControlViewModel = new VoiceRecorderControlViewModel(NAudioEngine.Instance,Workflow,mMessageDialogService, _eventAggregator);

            Workflow.PropertyChanged += _workflow_PropertyChanged;
            this.PropertyChanged += _wizardViewModel_PropertyChanged;
            _configureWorkflow();
            _bindEvents();
            _bindCommands();
        }

       

        #endregion

        #region - commands -

        public ICommand DialogHostLoaded { get; set; }
        public DelegateCommand SaveAndNext { get; set; }
        public ICommand SkipStep { get; set; }
        public DelegateCommand PlayInContext { get; set; }
        public ICommand StopPlayingInContext { get; set; }
        public ICommand Cancel { get; set; }

        #endregion

        #region - event handlers -

        private void _mpc_stateMachine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Workflow.State == WizardStates.PlayingInContext)
                return;

            switch (MediaPlayerControlViewModel.StateMachine.State)
            {
                case UI.Workflow.VideoPlayerStateMachine.States.Playing:
                    {
                        if(Workflow.CanFire(WizardTriggers.UserStartedAction))
                            Workflow.Fire(WizardTriggers.UserStartedAction);
                        break;
                    }
                case UI.Workflow.VideoPlayerStateMachine.States.Ready:
                    {
                        if(Workflow.CanFire(WizardTriggers.ReadyForUserAction))
                            Workflow.Fire(WizardTriggers.ReadyForUserAction);
                        break;
                    }
            }
        }

        private void _vrc_stateMachine_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (Workflow.State == WizardStates.PlayingInContext)
                return;

            switch (VoiceRecorderControlViewModel.StateMachine.State)
            {
                case UI.Workflow.MP3RecorderStateMachine.States.Playing:
                    {
                        if (Workflow.CanFire(WizardTriggers.UserStartedAction))
                            Workflow.Fire(WizardTriggers.UserStartedAction);
                        break;
                    }
                case UI.Workflow.MP3RecorderStateMachine.States.Recording:
                    {
                        Workflow.Fire(WizardTriggers.UserStartedAction);
                        break;
                    }
                case UI.Workflow.MP3RecorderStateMachine.States.Ready:
                    {
                        if (Workflow.CanFire(WizardTriggers.ReadyForUserAction))
                            Workflow.Fire(WizardTriggers.ReadyForUserAction);
                        break;
                    }
            }
        }

        private void _workflow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals(nameof(Workflow.State)))
            {
                ((DelegateCommand)Cancel).RaiseCanExecuteChanged();
                ((DelegateCommand)SaveAndNext).RaiseCanExecuteChanged();
                ((DelegateCommand)SkipStep).RaiseCanExecuteChanged();
                ((DelegateCommand)PlayInContext).RaiseCanExecuteChanged();
                ((DelegateCommand)StopPlayingInContext).RaiseCanExecuteChanged();
            }
        }

        private void _wizardViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(CurrentTutorialStep):
                    {
                        ((DelegateCommand)PlayInContext).RaiseCanExecuteChanged();
                        break;
                    }
            }
        }

        #endregion

        #region - private functions -

        #region - helper functions -

        private void _configureWorkflow()
        {
            Workflow.Configure(WizardStates.Started)
                .OnExit(() =>_startExited())
                .Permit(WizardTriggers.ShowChooseWizardDialog, WizardStates.ChooseWizardDialogShown);

            Workflow.Configure(WizardStates.ChooseWizardDialogShown)
                .OnActivate(() => _view_Loaded())
                .Permit(WizardTriggers.ReadyForUserAction, WizardStates.WaitingForUserAction)
                .Permit(WizardTriggers.LeaveWizard, WizardStates.LeavingWizard);

            Workflow.Configure(WizardStates.WaitingForUserAction)
                .Permit(WizardTriggers.SaveAndLoadNextStep, WizardStates.SavingAndLoadingNextStep)
                .Permit(WizardTriggers.LoadNextStep, WizardStates.LoadingNextStep)
                .Permit(WizardTriggers.LeaveWizard, WizardStates.LeavingWizard)
                .Permit(WizardTriggers.UserStartedAction, WizardStates.UserActionStarted)
                .Permit(WizardTriggers.PlayInContext,WizardStates.PlayingInContext);

            Workflow.Configure(WizardStates.PlayingInContext)
                .Permit(WizardTriggers.ReadyForUserAction, WizardStates.WaitingForUserAction);

            Workflow.Configure(WizardStates.UserActionStarted)
                .Permit(WizardTriggers.ReadyForUserAction, WizardStates.WaitingForUserAction);

            Workflow.Configure(WizardStates.SavingAndLoadingNextStep)
                .OnEntry(t => _saveStep())
                .Permit(WizardTriggers.ReadyForUserAction, WizardStates.WaitingForUserAction)
                .Permit(WizardTriggers.LoadNextStep, WizardStates.LoadingNextStep)
                .Permit(WizardTriggers.Finish, WizardStates.Finished);

            Workflow.Configure(WizardStates.Finished)
                .OnEntry(t => _finish())
                .Permit(WizardTriggers.LeaveWizard, WizardStates.LeavingWizard)
                .Permit(WizardTriggers.ShowChooseWizardDialog, WizardStates.ChooseWizardDialogShown);

            Workflow.Configure(WizardStates.LoadingNextStep)
                .OnEntry(t => _nextStep())
                .Permit(WizardTriggers.ReadyForUserAction, WizardStates.WaitingForUserAction);

            Workflow.Configure(WizardStates.LeavingWizard)
                .OnEntry(t => _leaveWizard())
                .OnExit(t => _resetData())
                .Permit(WizardTriggers.Start, WizardStates.Started);
        }

        private void _startExited()
        {
            VoiceRecorderControlViewModel.StateMachine.PropertyChanged += _vrc_stateMachine_PropertyChanged;
            MediaPlayerControlViewModel.StateMachine.PropertyChanged += _mpc_stateMachine_PropertyChanged;
        }

        private void _bindCommands()
        {
            DialogHostLoaded = new DelegateCommand(() => { Workflow.Fire(WizardTriggers.ShowChooseWizardDialog);});
            SaveAndNext = new DelegateCommand(() => { Workflow.Fire(WizardTriggers.SaveAndLoadNextStep); },_saveAndNext_CanExecute);
            SkipStep = new DelegateCommand(() => { Workflow.Fire(WizardTriggers.LoadNextStep); },_skipStep_CanExecute);
            PlayInContext = new DelegateCommand(_playDialogLineInContext_Execute,_playInContext_CanExecute);
            StopPlayingInContext = new DelegateCommand(_stopPlayingDialogLineInContext_Execute, _stopPlayingDialogLineInContext_CanExecute);
            Cancel = new DelegateCommand(() => { Workflow.Fire(WizardTriggers.LeaveWizard); },_cancel_CanExecute);
        }

        private void _bindEvents()
        {
            mEventAggregator.GetEvent<RequestTranslationEvent>().Subscribe(_onTranslationRequired);
        }

        private async void _onTranslationRequired(string _Caller)
        {            
            if(_Caller.Equals("VoiceRecorderControlViewModel"))
            {
                if (!string.IsNullOrEmpty(DialogStr))
                {
                    _generateSpeech(mDialogStr);
                    await mMessageDialogService.ShowMessage("Notification", "The sound was generated from the text box");
                    SaveAndNext.RaiseCanExecuteChanged();

                }
                else
                {
                    await mMessageDialogService.ShowMessage("Error", "Since the recording was disabled, the text box should contain some meaningfull text which will be converted to speech and must not be empty!");
                }
            }            
        }

        private bool _stopPlayingDialogLineInContext_CanExecute()
        {
            return Workflow.State == WizardStates.PlayingInContext;
        }

        private bool _cancel_CanExecute()
        {
            return Workflow.State == WizardStates.WaitingForUserAction;
        }

        private bool _playInContext_CanExecute()
        {
            try
            {
                if (CurrentTutorialStep == null)
                    return false;

                var dialogs = CurrentTutorialStep.PlayUserRecordedAudioInContext;

                foreach (var dialog in dialogs)
                {
                    foreach (var _dialogLine in dialog)
                    {
                        if (!_dialogLine.Equals(mcCurrentLineName)
                            && !(File.Exists(Path.Combine(ApplicationData.Instance.VideoDirectory, _dialogLine + ".avi"))
                            || File.Exists(Path.Combine(ApplicationData.Instance.VideoDirectory, _dialogLine))))
                            return false;
                    }
                }

                return Workflow.State == WizardStates.WaitingForUserAction
                       && VoiceRecorderControlViewModel.StartPlayingCommand.CanExecute();
            }
            catch (Exception){}
            return false;
        }

        private bool _skipStep_CanExecute()
        {
            try
            {
                return (CurrentWizard != null && CurrentStepIndex < CurrentWizard.TutorialSteps.Count - 1)
                        && Workflow.State == WizardStates.WaitingForUserAction;
            }
            catch (Exception) { }

            return false;
        }

        private bool _saveAndNext_CanExecute()
        {
            try
            {
                return Workflow.State == WizardStates.WaitingForUserAction
                       && (File.Exists(Path.Combine(ApplicationData.Instance.AudioDirectory, VoiceRecorderControlViewModel.CurrentFilePath + ".mp3"))
                       || CurrentStepIndex == CurrentWizard.TutorialSteps.Count - 1);
            }
            catch (Exception) { }

            return false;
        }

        private void _stopPlayingDialogLineInContext_Execute()
        {
            mCancellationTokenSource.Cancel();

            if (VoiceRecorderControlViewModel.StopPlayingInContextCommand.CanExecute())
            {
                VoiceRecorderControlViewModel.StopPlayingInContextCommand.Execute();
            }
            else if (MediaPlayerControlViewModel.StopPlayingInContextCommand.CanExecute(null))
            {
                MediaPlayerControlViewModel.StopPlayingInContextCommand.Execute(null);
            }
        }

        private async void _playDialogLineInContext_Execute()
        {
            Workflow.Fire(WizardTriggers.PlayInContext);

            await Task.Run(async () =>
            {
                try
                {
                    mCancellationTokenSource = new CancellationTokenSource();
                    var dialogs = CurrentTutorialStep.PlayUserRecordedAudioInContext;

                    for(int i=0;i<dialogs.Count;i++)
                    {
                        mCancellationTokenSource.Token.ThrowIfCancellationRequested();

                        foreach (var _dialogLine in dialogs[i])
                        {
                            mCancellationTokenSource.Token.ThrowIfCancellationRequested();

                            if (_dialogLine.Equals(mcCurrentLineName))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (VoiceRecorderControlViewModel.PlayInContextCommand.CanExecute())
                                        VoiceRecorderControlViewModel.PlayInContextCommand.Execute();
                                });
                            }
                            else
                            {

                                if (Path.HasExtension(_dialogLine))
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        MediaPlayerControlViewModel.CurrentVideoFilePath =
                                       Path.Combine(ApplicationData.Instance.VideoDirectory, _dialogLine);
                                    });
                                    
                                }
                                else
                                {
                                    Application.Current.Dispatcher.Invoke(() => {
                                        MediaPlayerControlViewModel.CurrentVideoFilePath =
                                           Path.Combine(ApplicationData.Instance.VideoDirectory, _dialogLine + ".avi");
                                    });
                                    
                                }
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    if (MediaPlayerControlViewModel.PlayInContextCommand.CanExecute(null))
                                        MediaPlayerControlViewModel.PlayInContextCommand.Execute(null);
                                });
                            }

                            mCancellationTokenSource.Token.ThrowIfCancellationRequested();

                            do
                            {
                                if (VoiceRecorderControlViewModel.StateMachine.State == UI.Workflow.MP3RecorderStateMachine.States.Playing
                                   || MediaPlayerControlViewModel.StateMachine.State == UI.Workflow.VideoPlayerStateMachine.States.Playing)
                                {
                                    await Task.Delay(500);
                                }
                            }
                            while (VoiceRecorderControlViewModel.StateMachine.State == UI.Workflow.MP3RecorderStateMachine.States.Playing 
                                  || MediaPlayerControlViewModel.StateMachine.State == UI.Workflow.VideoPlayerStateMachine.States.Playing);

                            Thread.Sleep(500);
                        }

                        if (i < dialogs.Count - 1)
                            Thread.Sleep(500);
                    }
                }                
                catch (OperationCanceledException)
                {
                    mLogger.Debug("_playDialogLineInContext method cancelled");
                }
                catch (Exception ex)
                {
                    mLogger.Error(ex.Message);
                }
            });

            CurrentTutorialStep = CurrentWizard.TutorialSteps[CurrentStepIndex];
            if(Path.HasExtension(CurrentTutorialStep.VideoFileName))
            {
                MediaPlayerControlViewModel.CurrentVideoFilePath =
                    Path.Combine(ApplicationData.Instance.VideoDirectory, CurrentTutorialStep.VideoFileName);
            }
            else
            {
                MediaPlayerControlViewModel.CurrentVideoFilePath =
                    Path.Combine(ApplicationData.Instance.VideoDirectory, CurrentTutorialStep.VideoFileName + ".avi");
            }
            Workflow.Fire(WizardTriggers.ReadyForUserAction);
        }

        private PhraseEntry _findPhraseInCharacterForTutorialStep(TutorialStep _tutorialStep)
        {
            string _tagName = _tutorialStep.PhraseWeights.Keys.First();
            PhraseEntry phrase =Character.Phrases.Where(p => p.PhraseWeights.Keys.First().Equals(_tagName))
                                                 .FirstOrDefault();

            return phrase;
        }

        private void _setDataForTutorialStep(int _currentStepIndex)
        {
            CurrentTutorialStep = CurrentWizard.TutorialSteps[_currentStepIndex];

            if (Path.HasExtension(CurrentTutorialStep.VideoFileName))
            {
                MediaPlayerControlViewModel.CurrentVideoFilePath =
                   Path.Combine(ApplicationData.Instance.VideoDirectory, CurrentTutorialStep.VideoFileName);
            }
            else
            {
                MediaPlayerControlViewModel.CurrentVideoFilePath =
                   Path.Combine(ApplicationData.Instance.VideoDirectory, CurrentTutorialStep.VideoFileName + ".avi");
            }

            DialogStr = "";

            if (!CurrentTutorialStep.CollectUserInput)
            {
                VoiceRecorderControlViewModel.CurrentFilePath = "";
                return;
            }

            VoiceRecorderControlViewModel.
                CurrentFilePath = $"{Character.CharacterPrefix}_{CurrentTutorialStep.PhraseWeights.Keys.First()}_{DateTime.Now.ToString("yyyy-dd-MM-HH-mm-ss")}";
        }

        #endregion

        #region - state machine functions -

        public void _resetData()
        {
            mCharacter = null;
            CurrentTutorialStep = null;
            CurrentWizard = null;
            CurrentStepIndex = 0;
            DialogStr = "";
            VoiceRecorderControlViewModel.EnableRecording = true;
        }

        private void _leaveWizard()
        {
            try
            {                
                VoiceRecorderControlViewModel.StateMachine.PropertyChanged -= _vrc_stateMachine_PropertyChanged;
                MediaPlayerControlViewModel.StateMachine.PropertyChanged -= _mpc_stateMachine_PropertyChanged;
                var _contentRegion = mRegionManager.Regions[Constants.ContentRegion];
                if (_checkIsCreateCharacterSession())
                {
                    // Get parent view oGo to play view
                    CreateCharacterViewModel viewModel = Session.Get(Constants.CREATE_CHARACTER_VIEW_MODEL) as CreateCharacterViewModel;

                    // S.Ristic (12/10/2019) - Commented that so that the user doesn't skip play mode if the 
                    // >>
                    // wizard has not finished yet.
                    //if (mIsFinished)
                    //    viewModel.Workflow.Fire(Triggers.GoPlay);
                    //else
                    //{
                    //    _contentRegion.NavigationService.Journal.GoBack();
                    //    viewModel.Workflow.Fire(Triggers.CheckCounter);
                    //}
                    // <<
                    // End of commented part                    

                    if(mIsFinished)
                    {
                        viewModel.Workflow.Fire(Triggers.GoPlay);
                        
                    } else
                    {
                        viewModel.Workflow.Fire(Triggers.Finish);
                        mRegionManager.Regions[Constants.ContentRegion].NavigationService.RequestNavigate("DialogView");
                    }
                    
                } else
                {                    
                    _contentRegion.NavigationService.Journal.GoBack();                    
                }

                Workflow.Fire(WizardTriggers.Start);
            }
            catch (Exception ex)
            {
                mLogger.Error("_leaveVizard" + ex.Message);
            }
        }

        private async void _view_Loaded()
        {
            if(Session.Contains(Constants.CHARACTER_EDIT_MODE) && (bool) Session.Get(Constants.CHARACTER_EDIT_MODE))
            {
                CreateCharacterViewModel createCharacterViewModel = Session.Get(Constants.CREATE_CHARACTER_VIEW_MODEL) as CreateCharacterViewModel;
                CurrentWizard = mWizardDataProvider.GetByName(createCharacterViewModel.CurrentDialogWizard);
                Character = Session.Get(Constants.NEW_CHARACTER) as Character;
                _setDataForTutorialStep(CurrentStepIndex);
                Workflow.Fire(WizardTriggers.ReadyForUserAction);
            } else
            {
                var result = await mMessageDialogService.ShowDedicatedDialogAsync<int?>(mWizardFormDialog);

                if (result.HasValue)
                {
                    CurrentWizard = mWizardDataProvider.GetByIndex(result.Value);

                    if (Session.Contains(Constants.CHARACTER_EDIT_MODE) && (bool)Session.Get(Constants.CHARACTER_EDIT_MODE) == true)
                    {
                        Character = Session.Get(Constants.NEW_CHARACTER) as Character;
                    }
                    else
                    {
                        Character = mRegionManager.Regions[Constants.ContentRegion].Context as Character;
                    }

                    _setDataForTutorialStep(CurrentStepIndex);

                    Workflow.Fire(WizardTriggers.ReadyForUserAction);
                }
                else
                {
                    Workflow.Fire(WizardTriggers.LeaveWizard);
                }
            }
            
        }

        private void _nextStep()
        {
            try
            {
                ++CurrentStepIndex;

                _setDataForTutorialStep(CurrentStepIndex);
            }
            catch (Exception ex)
            {
                mLogger.Error("_skipStep" + ex.Message);
            }

            Workflow.Fire(WizardTriggers.ReadyForUserAction);
        }

        private async void _saveStep()
        {
            if (!mCurrentTutorialStep.CollectUserInput)
            {
                Workflow.Fire(WizardTriggers.Finish);
                return;
            }

            if (string.IsNullOrEmpty(DialogStr))
            {
                var result = await mMessageDialogService.
                    ShowOKCancelDialogAsync("You didn't write text for this dialog line. Do you want to save step without it?"
                    , "Warning", "Yes", "No");

                if (result == MessageDialogResult.Cancel)
                {
                    Workflow.Fire(WizardTriggers.ReadyForUserAction);
                    return;
                }
            }
            string[] _fileNameParts = VoiceRecorderControlViewModel.CurrentFilePath.Split('_');
            
            var _phraseEntry = new PhraseEntry
            {
                PhraseRating = CurrentTutorialStep.PhraseRating,
                DialogStr = DialogStr,
                PhraseWeights = new Dictionary<string,double>(), 
                FileName = $"{_fileNameParts[_fileNameParts.Length-2]}_{_fileNameParts.Last()}"
            };

            foreach(KeyValuePair<string, double> entry in CurrentTutorialStep.PhraseWeights)
            {
                _phraseEntry.PhraseWeights.Add(entry.Key, entry.Value);
            }


            //by adding the mp3 filename for a phrase as a phraseweight we can access an exact phrase
            // from an existing character without editing that character, for instance a built in character
            _phraseEntry.PhraseWeights.Add(mCharacter.CharacterPrefix + "_" + _phraseEntry.FileName, 1.0);
            mCharacter.Phrases.Add(_phraseEntry);

            await mCharacterDataProvider.SaveAsync(Character);

            if (CurrentStepIndex >= CurrentWizard.TutorialSteps.Count - 1)  
            {
                Workflow.Fire(WizardTriggers.Finish);
                return;
            }

            Workflow.Fire(WizardTriggers.LoadNextStep);
        }

        private async  void _finish()
        {
            if(_checkIsCreateCharacterSession())
            {
                var result = await mMessageDialogService.ShowOKCancelDialogAsync("Character successfully updated!", "Info",
                "Next", "Cancel");

                if(result == MessageDialogResult.Cancel)
                {
                    CreateCharacterViewModel cvwModel = Session.Get<CreateCharacterViewModel>(Constants.CREATE_CHARACTER_VIEW_MODEL);
                    if(cvwModel != null)
                    {
                        cvwModel.Workflow.Fire(Triggers.Finish);
                    }
                }

                mIsFinished = true;
                Workflow.Fire(WizardTriggers.LeaveWizard);                
            } else
            {
                var result = await mMessageDialogService.ShowOKCancelDialogAsync("Character successfully updated!", "Info",
                "Run another wizard", "Close wizard");

                if (result != MessageDialogResult.Cancel)
                {
                    CurrentStepIndex = 0;
                    DialogStr = "";
                    Workflow.Fire(WizardTriggers.ShowChooseWizardDialog);
                }
                else
                {
                    Workflow.Fire(WizardTriggers.LeaveWizard);
                }
            }
                        
        }

        private bool _checkIsCreateCharacterSession()
        {
            if(Session.Contains(Constants.CHARACTER_EDIT_MODE) && (bool)Session.Get(Constants.CHARACTER_EDIT_MODE))
            {
                return true;
            }

            return false;
        }

        #endregion

        #endregion

        #region - properties -

        public WizardWorkflow Workflow { get; set; }

        public TutorialStep CurrentTutorialStep
        {
            get { return mCurrentTutorialStep; }
            set
            {
                mCurrentTutorialStep = value;
                RaisePropertyChanged();
            }
        }

        public Wizard CurrentWizard
        {
            get { return mCurrentWizard; }
            set
            {
                mCurrentWizard = value;
                RaisePropertyChanged();
            }
        }

        public int CurrentStepIndex
        {
            get { return mCurrentStepIndex; }
            set
            {
                mCurrentStepIndex = value;
                RaisePropertyChanged();
            }
        }

        public Character Character
        {
            get { return mCharacter; }
            set
            {
                mCharacter = value;
                VoiceRecorderControlViewModel.EnableRecording = !mCharacter.HasNoVoice;
                RaisePropertyChanged();
            }
        }

        public string DialogStr
        {
            get { return mDialogStr; }
            set
            {
                mDialogStr = value;
                //if(!string.IsNullOrEmpty(mDialogStr) && ApplicationData.Instance.Text2SpeechEnabled)
                //{
                //    _generateSpeech(mDialogStr);
                //}
                
                RaisePropertyChanged();
            }
        }

        

        public MediaPlayerControlViewModel MediaPlayerControlViewModel
        {
            get { return mMediaPlayerControlViewModel; }
            set
            {
                mMediaPlayerControlViewModel = value;
                RaisePropertyChanged();
            }
        }

        public VoiceRecorderControlViewModel VoiceRecorderControlViewModel
        {
            get { return mVoiceRecorderControlViewModel; }
            set
            {
                mVoiceRecorderControlViewModel = value;
                RaisePropertyChanged();
            }
        }

        #endregion

        #region Private methods

        private void _generateSpeech(string value)
        {
            string _outfile = string.Empty;

            using (SpeechSynthesizer _synth = new SpeechSynthesizer())
            {
                _synth.Volume = 100;
                _synth.Rate = Character.SpeechRate;
                if (_synth.GetInstalledVoices().Count() == 0)
                {
                    return;
                }
                
                if (_synth.GetInstalledVoices().Where(iv => iv.VoiceInfo.Name.Equals(Character.Voice)).Count() > 0)
                {
                    _synth.SelectVoice(Character.Voice);
                }

                string _outfile_original = ApplicationData.Instance.AudioDirectory + "\\" + VoiceRecorderControlViewModel.CurrentFilePath + ".mp3";
                _outfile = _outfile_original.Replace(".mp3", ".wav");
                _synth.SetOutputToWaveFile(_outfile);
                _synth.Speak(value);
                cs_ffmpeg_mp3_converter.FFMpeg.Convert2Mp3(_outfile, _outfile_original);
                VoiceRecorderControlViewModel.StartPlayingCommand.RaiseCanExecuteChanged();
                PlayInContext.RaiseCanExecuteChanged();
            }

            if (!string.IsNullOrEmpty(_outfile) && File.Exists(_outfile))
            {
                File.Delete(_outfile);
            }
        }

        #endregion
    }
}
