using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Views;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.UI.Workflow.WizardWorkflow;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Events;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
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
        private int mCurrentStepIndex;
        private string mDialogStr;
        private WizardFormDialog mWizardFormDialog;
        private MediaPlayerControlViewModel mMediaPlayerControlViewModel;
        private VoiceRecorderControlViewModel mVoiceRecorderControlViewModel;
        private Character mCharacter;
        private Wizard mCurrentWizard;
        private TutorialStep mCurrentTutorialStep;
        private CancellationTokenSource mCancellationTokenSource;

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

            Workflow = new WizardWorkflow(action: () => { });
            MediaPlayerControlViewModel = new MediaPlayerControlViewModel(Workflow);
            VoiceRecorderControlViewModel = new VoiceRecorderControlViewModel(NAudioEngine.Instance,Workflow,mMessageDialogService);

            Workflow.PropertyChanged += _workflow_PropertyChanged;
            this.PropertyChanged += _wizardViewModel_PropertyChanged;
            _configureWorkflow();
            _bindCommands();
        }

        #endregion

        #region - commands -

        public ICommand DialogHostLoaded { get; set; }
        public ICommand SaveAndNext { get; set; }
        public ICommand SkipStep { get; set; }
        public ICommand PlayInContext { get; set; }
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
                var dialogs = CurrentTutorialStep.PlayUserRecordedAudioInContext;

                foreach (var dialog in dialogs)
                {
                    foreach (var _dialogLine in dialog)
                    {
                        if (!_dialogLine.Equals(mcCurrentLineName)
                            && !File.Exists(Path.Combine(ApplicationData.Instance.VideoDirectory, _dialogLine + ".avi")))
                            return false;
                    }
                }

                return Workflow.State == WizardStates.WaitingForUserAction
                       && VoiceRecorderControlViewModel.StartPlayingCommand.CanExecute(null);
            }
            catch (Exception){}
            return false;
        }

        private bool _skipStep_CanExecute()
        {
            try
            {
                return (CurrentStepIndex < CurrentWizard.TutorialSteps.Count - 1)
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

            if (VoiceRecorderControlViewModel.StopPlayingInContextCommand.CanExecute(null))
            {
                VoiceRecorderControlViewModel.StopPlayingInContextCommand.Execute(null);
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
                                    if (VoiceRecorderControlViewModel.PlayInContextCommand.CanExecute(null))
                                        VoiceRecorderControlViewModel.PlayInContextCommand.Execute(null);
                                });
                            }
                            else
                            {

                                MediaPlayerControlViewModel.CurrentVideoFilePath = Path.Combine(ApplicationData.Instance.VideoDirectory, _dialogLine + ".avi");
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
            MediaPlayerControlViewModel.CurrentVideoFilePath = Path.Combine(ApplicationData.Instance.VideoDirectory, CurrentTutorialStep.VideoFileName + ".avi");
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
            MediaPlayerControlViewModel.CurrentVideoFilePath = Path.Combine(ApplicationData.Instance.VideoDirectory, CurrentTutorialStep.VideoFileName + ".avi");
            DialogStr = "";

            if (!CurrentTutorialStep.CollectUserInput)
            {
                VoiceRecorderControlViewModel.CurrentFilePath = "";
                return;
            }

            VoiceRecorderControlViewModel.
                CurrentFilePath = $"{Character.CharacterPrefix}_{CurrentTutorialStep.PhraseWeights.Keys.First()}_{DateTime.Now.ToString("yyyy-dd-MM-HH-mm-ss")}";
            //var _currentPhrase = _findPhraseInCharacterForTutorialStep(CurrentTutorialStep);

            //if (_currentPhrase == null)
            //{
            //    mIsPhraseEditable = false;
            //    DialogStr = "";
            //    //VoiceRecorderControlViewModel.CurrentFilePath = "";

            //    return;
            //}
            //mIsPhraseEditable = false;  //TODO make 
            //DialogStr = _currentPhrase.DialogStr;
            //VoiceRecorderControlViewModel.CurrentFilePath = $"{Character.CharacterPrefix}_{_currentPhrase.FileName}";
            //mCurrentPhrase = _currentPhrase;
            //mIsPhraseEditable = true;
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
        }

        private void _leaveWizard()
        {
            try
            {
                VoiceRecorderControlViewModel.StateMachine.PropertyChanged -= _vrc_stateMachine_PropertyChanged;
                MediaPlayerControlViewModel.StateMachine.PropertyChanged -= _mpc_stateMachine_PropertyChanged;
                var _contentRegion = mRegionManager.Regions[Constants.ContentRegion];
                _contentRegion.NavigationService.Journal.GoBack();

                Workflow.Fire(WizardTriggers.Start);
            }
            catch (Exception ex)
            {
                mLogger.Error("_leaveVizard" + ex.Message);
            }
        }

        private async void _view_Loaded()
        {
            var result =  await mMessageDialogService.ShowDedicatedDialogAsync<int?>(mWizardFormDialog);

            if (result.HasValue)
            {
                CurrentWizard = mWizardDataProvider.GetByIndex(result.Value);
                Character = mRegionManager.Regions[Constants.ContentRegion].Context as Character;

                _setDataForTutorialStep(CurrentStepIndex);

                Workflow.Fire(WizardTriggers.ReadyForUserAction);
            }
            else
            {
                Workflow.Fire(WizardTriggers.LeaveWizard);
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
            if (CurrentStepIndex >= CurrentWizard.TutorialSteps.Count - 1 || !mCurrentTutorialStep.CollectUserInput)
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

            //if (mIsPhraseEditable)
            //{
            //    mCurrentPhrase.DialogStr = DialogStr;
            //}
            //else
            //{
            string[] _fileNameParts = VoiceRecorderControlViewModel.CurrentFilePath.Split('_');

            var _phraseEntry = new PhraseEntry
            {
                PhraseRating = CurrentTutorialStep.PhraseRating,
                DialogStr = DialogStr,
                PhraseWeights = CurrentTutorialStep.PhraseWeights,
                FileName = $"{_fileNameParts[_fileNameParts.Length-2]}_{_fileNameParts.Last()}"
            };

            mCharacter.Phrases.Add(_phraseEntry);
            //}

            await mCharacterDataProvider.SaveAsync(Character);

            Workflow.Fire(WizardTriggers.LoadNextStep);
        }

        private async  void _finish()
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
                RaisePropertyChanged();
            }
        }

        public string DialogStr
        {
            get { return mDialogStr; }
            set
            {
                mDialogStr = value;
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
    }
}
