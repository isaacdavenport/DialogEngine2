using DialogGenerator.Core;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Views;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.UI.Workflow.CreateCharacterWorkflow;
using DialogGenerator.UI.Workflow.WizardWorkflow;
using DialogGenerator.Utilities;
using NAudio.Lame;
using NAudio.Wave;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
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
        private IDialogModelDataProvider mDialogModelDataProvider;
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
        private string mOutMp3File = string.Empty;
        private string mOutWavFile = string.Empty;
        private SpeechSynthesizer mSpeechSyntesizer;

        #endregion

        #region - constructor -

        public WizardViewModel(ILogger logger,IUserLogger _userLogger
            ,IEventAggregator _eventAggregator
            ,IWizardDataProvider _wizardDataProvider
            ,ICharacterDataProvider _characterDataProvider
            ,IDialogModelDataProvider _dialogModelDataProvider
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
            mDialogModelDataProvider = _dialogModelDataProvider;
            mRegionManager = _regionManager;
            mEventAggregator = _eventAggregator;
            
            Workflow = new WizardWorkflow(action: () => { });
            MediaPlayerControlViewModel = new MediaPlayerControlViewModel(Workflow, mLogger);

            mLogger.Info("Before creating of VoiceRecorderControlViewModel");
            VoiceRecorderControlViewModel = new VoiceRecorderControlViewModel(NAudioEngine.Instance,Workflow,mMessageDialogService, _eventAggregator);
            mLogger.Info("After creating of VoiceRecorderControlViewModel");

            mLogger.Info("Before calling of the speech synthesizer");
            mSpeechSyntesizer = new SpeechSynthesizer();
            mLogger.Info("After calling of the speech synthesizer");

            Workflow.PropertyChanged += _workflow_PropertyChanged;
            this.PropertyChanged += _wizardViewModel_PropertyChanged;
            _configureWorkflow();
            _bindEvents();
            _bindCommands();
        }



        #endregion

        #region - commands -

        public ICommand DialogHostLoaded { get; set; }
        public ICommand DialogHostUnloaded { get; set; }
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
            DialogHostUnloaded = new DelegateCommand(_view_Unloaded);
            SaveAndNext = new DelegateCommand(() => { Workflow.Fire(WizardTriggers.SaveAndLoadNextStep); },_saveAndNext_CanExecute);
            SkipStep = new DelegateCommand(() => { Workflow.Fire(WizardTriggers.LoadNextStep); },_skipStep_CanExecute);
            PlayInContext = new DelegateCommand(_playDialogLineInContext_Execute,_playInContext_CanExecute);
            StopPlayingInContext = new DelegateCommand(_stopPlayingDialogLineInContext_Execute, _stopPlayingDialogLineInContext_CanExecute);
            Cancel = new DelegateCommand(() => { Workflow.Fire(WizardTriggers.LeaveWizard); },_cancel_CanExecute);
        }



        private void _bindEvents()
        {
            mEventAggregator.GetEvent<RequestTranslationEvent>().Subscribe(_onTranslationRequired);
            mEventAggregator.GetEvent<SpeechConvertedEvent>().Subscribe(_onSpeechRecognized);
        }

        private void _onSpeechRecognized(string _recognizedText)
        {
            if(string.IsNullOrEmpty(DialogStr))
            {
                DialogStr = _recognizedText;
            }
        }

        private async void _onTranslationRequired(string _Caller)
        {            
            if(_Caller.Equals("VoiceRecorderControlViewModel"))
            {
                if (!string.IsNullOrEmpty(DialogStr))
                {
                    GenerateSpeech(mDialogStr);
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
            catch (Exception exp){
                mLogger.Error("Playing in context can execute exception - " + exp.Message);
            }

            return false;
        }

        private bool _skipStep_CanExecute()
        {
            try
            {
                return (CurrentWizard != null && CurrentStepIndex < CurrentWizard.TutorialSteps.Count - 1)
                        && Workflow.State == WizardStates.WaitingForUserAction;
            }
            catch (Exception exp) {
                mLogger.Error("Skip step can exception - " + exp.Message);
            }

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
            catch (Exception exp)
            {
                mLogger.Error("SaveAndNext can execute exception - " + exp.Message);
            }

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
                                mLogger.Info(string.Format("Playing in context - Character ({0}), about to play in context the dialog line - {1}", Character.CharacterName, _dialogLine));
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
                                    mLogger.Info(string.Format("Playing in context - {0} is about to be played!", MediaPlayerControlViewModel.CurrentVideoFilePath));

                                    if (MediaPlayerControlViewModel.PlayInContextCommand.CanExecute(null))
                                    {
                                        MediaPlayerControlViewModel.PlayInContextCommand.Execute(null);
                                        mLogger.Info(string.Format("Playing in context - {0} playing!", MediaPlayerControlViewModel.CurrentVideoFilePath));
                                    }
                                        
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

            mSpeechSyntesizer.SpeakCompleted += _synth_SpeakCompleted;

        }

        private void _view_Unloaded()
        {
            mSpeechSyntesizer.SpeakCompleted -= _synth_SpeakCompleted;
            if(!Session.Get<bool>(Constants.CHARACTER_EDIT_MODE))
            {
                mEventAggregator.GetEvent<CharacterUpdatedEvent>().Publish();
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
            string _fileName = string.Empty;
            for(int i = 0; i < _fileNameParts.Length; i++)
            {
                if(i > 1)
                {
                    if(i != 2)
                    {
                        _fileName += "_";
                    }

                    _fileName += _fileNameParts[i];
                }
            }

            var _phraseEntry = new PhraseEntry
            {
                PhraseRating = CurrentTutorialStep.PhraseRating,
                DialogStr = DialogStr,
                PhraseWeights = new Dictionary<string, double>(),
                FileName = /* $"{_fileNameParts[_fileNameParts.Length-2]}_{_fileNameParts.Last()}" */ _fileName
            };

            foreach(KeyValuePair<string, double> entry in CurrentTutorialStep.PhraseWeights)
            {
                _phraseEntry.PhraseWeights.Add(entry.Key, entry.Value);
            }

            //by adding the mp3 filename for a phrase as a phraseweight we can access an exact phrase
            // from an existing character without editing that character, for instance a built in character
            if(string.IsNullOrEmpty(mCurrentWizard.Commands))
            {
                _phraseEntry.PhraseWeights.Add(mCharacter.CharacterPrefix + "_" + _phraseEntry.FileName, 1.0);
            }
            
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
                if(mCurrentWizard != null && mCharacter != null)
                {
                    _inspectForCommandWizardAndChangeCharacter();
                }
                
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

        private void GenerateSpeech(string value)
        {
            string _outfile = string.Empty;
            string _outFileOriginal = string.Empty;

            try
            {
                mSpeechSyntesizer.Volume = 100;
                mSpeechSyntesizer.Rate = Character.SpeechRate;
                if (mSpeechSyntesizer.GetInstalledVoices().Count() == 0)
                {
                    return;
                }

                if (mSpeechSyntesizer.GetInstalledVoices().Where(iv => iv.VoiceInfo.Name.Equals(Character.Voice)).Count() > 0)
                {
                    mSpeechSyntesizer.SelectVoice(Character.Voice);
                }

                mOutMp3File = ApplicationData.Instance.AudioDirectory + "\\" + VoiceRecorderControlViewModel.CurrentFilePath + ".mp3";
                mOutWavFile = mOutMp3File.Replace(".mp3", ".wav");

                MemoryStream _ms = new MemoryStream();
                mSpeechSyntesizer.SetOutputToWaveStream(_ms);
                mSpeechSyntesizer.Speak(value);
                mSpeechSyntesizer.SetOutputToNull();

                _ms.Position = 0;

                using (var _rdr = new WaveFileReader(_ms))
                using (var _outFileStream = File.OpenWrite(mOutMp3File))
                using (var _wtr = new LameMP3FileWriter(_outFileStream, _rdr.WaveFormat, 128))
                {
                    _rdr.CopyTo(_wtr);
                }
                    
                VoiceRecorderControlViewModel.StartPlayingCommand.RaiseCanExecuteChanged();
                PlayInContext.RaiseCanExecuteChanged();
            } catch (Exception exp)
            {
                mLogger.Info(exp.Message);
                mLogger.Error(exp.Message);
            }
            

        }

        private void _synth_SpeakCompleted(object sender, SpeakCompletedEventArgs e)
        {
            try
            {
                mLogger.Info("Entered handler");
                mLogger.Debug("Entered handler");

                mSpeechSyntesizer.SetOutputToNull();

                var _fs = File.OpenRead(mOutWavFile);

                //using (var _fs = File.OpenRead(mOutWavFile))
                //using (var _rdr = new WaveFileReader(_fs))
                //using (var _outFileStream = File.OpenWrite(mOutMp3File))
                //using (var _wtr = new LameMP3FileWriter(_outFileStream, _rdr.WaveFormat, 128))
                //{
                //    _rdr.CopyTo(_wtr);
                //}

                mLogger.Info("Conversion is done");
                mLogger.Debug("Conversion is done");

                if (!string.IsNullOrEmpty(mOutWavFile) && File.Exists(mOutWavFile))
                {
                    File.Delete(mOutWavFile);
                }

                mLogger.Info("File deleted");
                mLogger.Debug("File deleted");

                VoiceRecorderControlViewModel.StartPlayingCommand.RaiseCanExecuteChanged();
                PlayInContext.RaiseCanExecuteChanged();
            } catch (Exception excep)
            {
                mLogger.Error(excep.Message);
                mLogger.Error(excep.Message);
            }
            
        }

        public static byte[] ConvertWavToMp3(byte[] wavFile)
        {

            using (var retMs = new MemoryStream())
            using (var ms = new MemoryStream(wavFile))
            using (var rdr = new WaveFileReader(ms))
            using (var wtr = new LameMP3FileWriter(retMs, rdr.WaveFormat, 128))
            {
                rdr.CopyTo(wtr);
                return retMs.ToArray();
            }
        }

        private void _inspectForCommandWizardAndChangeCharacter()
        {
            if (!string.IsNullOrEmpty(mCurrentWizard.Commands) && mCurrentWizard.Commands.Contains("ContextualDialog"))
            {
                // Make a copy of the wizard and change it

                // We have to clone the wizard because we are going 
                // to change it's phraseweights collections.
                mCurrentWizard = (Wizard)mCurrentWizard.Clone();

                // This is the list of phrases which fill be used later in the creation 
                // of custom dialog.
                List<string> _phrasesForDialog = new List<string>();

                // Identifier used for the new phrases and the dialog.
                string _identifier = Guid.NewGuid().ToString();
                _identifier = _identifier.Substring(0, 4);

                // Dialog popularity.
                int _popularity = -1;

                // Now change the PhraseWeights
                string _wizardName = mCurrentWizard.WizardName;
                string[] _tokens = mCurrentWizard.Commands.Split(' ');
                int _counter = 0;
                foreach (var _token in _tokens)
                {
                    var _phraseWeights = mCurrentWizard.TutorialSteps[_counter].PhraseWeights;

                    if (_token.Equals("ContextualDialog"))
                        continue;
                    if (_token.Equals("{" + string.Format("{0}", _counter) + "}"))
                    {
                        if (_phraseWeights.Where(pw => pw.Key.Contains(_wizardName)).Count() > 0)
                        {
                            var _phraseWeight = _phraseWeights.Where(pw => pw.Key.Contains(_wizardName)).First();
                            string _phraseWeightKey = _phraseWeight.Key;
                            double _phraseWeightValue = _phraseWeight.Value;
                            _phraseWeights.Remove(_phraseWeightKey);
                            string _newKey = _wizardName + "_" + _counter + "_" + _identifier;
                            _phraseWeights.Add(_newKey, _phraseWeightValue);
                            _phrasesForDialog.Add(_newKey);
                            _counter++;
                        }

                    }
                    else
                    {
                        if (!Int32.TryParse(_token, out _popularity))
                        {
                            _phrasesForDialog.Add(_token);
                        }
                    }
                }

                // Now, create the custom dialog.
                ModelDialog _modelDialog = new ModelDialog
                {
                    Name = mCharacter.CharacterPrefix + "_" + _wizardName + "_" + _identifier,
                    Popularity = 14,
                };

                _modelDialog.PhraseTypeSequence = new List<string>();
                _modelDialog.PhraseTypeSequence.AddRange(_phrasesForDialog);

                // Find or create the dialog collection
                var _dialogsCollection = mDialogModelDataProvider.GetByName(mCharacter.CharacterPrefix + "_" + _wizardName + "Dialogs");
                if (_dialogsCollection == null)
                {
                    _dialogsCollection = new ModelDialogInfo
                    {
                        Editable = true,
                        FileName = Character.FileName,
                        ModelsCollectionName = mCharacter.CharacterPrefix + "_" + _wizardName + "Dialogs",
                    };

                    _dialogsCollection.ArrayOfDialogModels = new List<ModelDialog>();
                    mDialogModelDataProvider.GetAll().Add(_dialogsCollection);
                }

                if (!_dialogsCollection.ArrayOfDialogModels.Contains(_modelDialog))
                {
                    _dialogsCollection.ArrayOfDialogModels.Add(_modelDialog);
                }

            }
        }

        #endregion
    }
}
