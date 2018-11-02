using DialogGenerator.Core;
using DialogGenerator.Model;
using DialogGenerator.UI.Controls.VoiceRecorder;
using DialogGenerator.UI.Data;
using DialogGenerator.UI.Views;
using DialogGenerator.UI.Views.Dialogs;
using DialogGenerator.UI.Workflow.WizardWorkflow;
using DialogGenerator.Utilities;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
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
        private const string mcWordForSplitting = "Wiz"; // "VideoFileName" : "xxWizIntroduction" used to get step name(Introduction) from "VideoFileName" by splitting 

        #endregion

        private ILogger mLogger;
        private IUserLogger mUserLogger;
        private IWizardDataProvider mWizardDataProvider;
        private IMessageDialogService mMessageDialogService;
        private ICharacterDataProvider mCharacterDataProvider;
        private IRegionManager mRegionManager;
        private int mCurrentStepIndex;
        private string mCurrentVideoFilePath;
        private string mDialogStr;
        private bool mIsPlayingLineInContext;
        private bool mIsPhraseEditable;
        private States mCurrentState;
        private PhraseEntry mCurrentPhrase;
        private WizardFormDialog mWizardFormDialog;
        private MediaPlayerControlViewModel mMediaPlayerControlViewModel;
        private VoiceRecorderControlViewModel mVoiceRecorderControlViewModel;
        private Character mCharacter;
        private Wizard mCurrentWizard;
        private TutorialStep mCurrentTutorialStep;
        private CancellationTokenSource mCancellationTokenSource;

        #endregion

        #region - constructor -

        public WizardViewModel(ILogger logger,IUserLogger _userLogger,IWizardDataProvider _wizardDataProvider
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
            MediaPlayerControlViewModel = new MediaPlayerControlViewModel();
            VoiceRecorderControlViewModel = new VoiceRecorderControlViewModel(NAudioEngine.Instance);

            Workflow.PropertyChanged += _Workflow_PropertyChanged;

            _configureWorkflow();
            _bindCommands();
            _registerListeners();
        }

        #endregion

        #region - commands -

        public ICommand DialogHostLoaded { get; set; }
        public ICommand SaveAndNext { get; set; }
        public ICommand SkipStep { get; set; }
        public ICommand CreateNewCharacter { get; set; }
        public ICommand PlayInContext { get; set; }
        public ICommand Cancel { get; set; }

        #endregion

        #region - event handlers -

        private void _Workflow_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("State"))
                CurrentState = Workflow.State;
        }

        private void _voiceRecorderControlViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "IsPlaying":
                    {
                        if (mVoiceRecorderControlViewModel.IsPlaying)
                        {
                            if (Workflow.CanFire(Triggers.VoiceRecorderPlaying))
                                Workflow.Fire(Triggers.VoiceRecorderPlaying);
                        }
                        else
                        {
                            if (Workflow.CanFire(Triggers.ReadyForUserAction))
                                Workflow.Fire(Triggers.ReadyForUserAction);
                        }

                        break;
                    }

                case "IsRecording":
                    {
                        if (mVoiceRecorderControlViewModel.IsRecording)
                        {
                            if (Workflow.CanFire(Triggers.VoiceRecorderRecording))
                                Workflow.Fire(Triggers.VoiceRecorderRecording);
                        }
                        else
                        {
                            if (Workflow.CanFire(Triggers.ReadyForUserAction))
                                Workflow.Fire(Triggers.ReadyForUserAction);
                        }

                        break;
                    }
            }
        }

        private void _mediaPlayerControlViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsPlaying"))
            {
                if (mMediaPlayerControlViewModel.IsPlaying)
                {
                    Workflow.Fire(Triggers.VideoPlayerPlaying);
                }
                else
                {
                    Workflow.Fire(Triggers.ReadyForUserAction);
                }
            }
        }

        #endregion

        #region - private functions -

        #region - helper functions -

        private void _configureWorkflow()
        {
            Workflow.Configure(States.Start)
                .Permit(Triggers.ShowFormDialog, States.ShowFormDialog);

            Workflow.Configure(States.ShowFormDialog)
                .OnEntry(t => _view_Loaded())
                .Permit(Triggers.ReadyForUserAction, States.ReadyForUserAction)
                .Permit(Triggers.LeaveWizard, States.LeaveWizard);

            Workflow.Configure(States.ReadyForUserAction)
                .Permit(Triggers.SaveAndNext, States.SaveAndNext)
                .Permit(Triggers.SkipStep, States.SkipStep)
                .Permit(Triggers.LeaveWizard, States.LeaveWizard)
                .Permit(Triggers.VoiceRecorderPlaying, States.VoiceRecorderPlaying)
                .Permit(Triggers.VoiceRecorderRecording, States.VoiceRecorderRecording)
                .Permit(Triggers.VoiceRecorderPlayingInContext, States.VoiceRecorderPlayingInContext)
                .Permit(Triggers.VideoPlayerPlaying, States.VideoPlayerPlaying);

            // State VoiceRecorderAction is added to be able to disable others controls if any of its substates is active
            Workflow.Configure(States.VoiceRecorderAction)
                .Permit(Triggers.ReadyForUserAction, States.ReadyForUserAction);

            Workflow.Configure(States.VoiceRecorderRecording)
                .SubstateOf(States.VoiceRecorderAction);

            Workflow.Configure(States.VoiceRecorderPlaying)
                .SubstateOf(States.VoiceRecorderAction);

            Workflow.Configure(States.VoiceRecorderPlayingInContext)
                .OnExit(t => _registerListeners())
                .SubstateOf(States.VoiceRecorderAction);

            Workflow.Configure(States.VideoPlayerAction)
                .Permit(Triggers.ReadyForUserAction, States.ReadyForUserAction);

            Workflow.Configure(States.SaveAndNext)
                .OnEntry(t => _saveAndNextStep())
                .Permit(Triggers.ReadyForUserAction, States.ReadyForUserAction)
                .Permit(Triggers.SkipStep, States.SkipStep)
                .Permit(Triggers.Finish, States.Finish);

            Workflow.Configure(States.Finish)
                .OnEntry(t => _finish())
                .Permit(Triggers.LeaveWizard, States.LeaveWizard)
                .Permit(Triggers.ShowFormDialog, States.ShowFormDialog);

            Workflow.Configure(States.SkipStep)
                .OnEntry(t => _skipStep())
                .Permit(Triggers.ReadyForUserAction, States.ReadyForUserAction);

            Workflow.Configure(States.LeaveWizard)
                .OnEntry(t => _leaveVizard())
                .Permit(Triggers.Start, States.Start);

            Workflow.Configure(States.VideoPlayerPlaying)
                .SubstateOf(States.VideoPlayerAction);

        }

        private void _clearListeners()
        {
            mMediaPlayerControlViewModel.PropertyChanged -= _mediaPlayerControlViewModel_PropertyChanged;
            mVoiceRecorderControlViewModel.PropertyChanged -= _voiceRecorderControlViewModel_PropertyChanged;
        }

        private void _registerListeners()
        {
            mMediaPlayerControlViewModel.PropertyChanged += _mediaPlayerControlViewModel_PropertyChanged;
            mVoiceRecorderControlViewModel.PropertyChanged += _voiceRecorderControlViewModel_PropertyChanged;
        }

        private void _bindCommands()
        {
            DialogHostLoaded = new DelegateCommand(() => { Workflow.Fire(Triggers.ShowFormDialog); });
            SaveAndNext = new DelegateCommand(() => { Workflow.Fire(Triggers.SaveAndNext); });
            SkipStep = new DelegateCommand(() => { Workflow.Fire(Triggers.SkipStep); });
            PlayInContext = new DelegateCommand(() => _playOrStopDialogLineInContext());
            Cancel = new DelegateCommand(() => { Workflow.Fire(Triggers.LeaveWizard); });
        }

        private async void _playOrStopDialogLineInContext()
        {
            if (IsPlayingLineInContext)
                _stopPlayingDialogLineInContext();
            else
                await _playDialogLineInContext();
        }

        private void _stopPlayingDialogLineInContext()
        {
            mCancellationTokenSource.Cancel();

            if (VoiceRecorderControlViewModel.IsPlaying)
            {
                VoiceRecorderControlViewModel.PlayOrStop(VoiceRecorderControlViewModel.CurrentFilePath);
            }
            else if (MediaPlayerControlViewModel.IsPlaying)
            {
                MediaPlayerControlViewModel.StopMediaPlayer();
            }
        }

        private async Task _playDialogLineInContext()
        {
            string _tutorialStepVideoFilePathCache = CurrentVideoFilePath;

            await Task.Run(async () =>
            {
                try
                {
                    _clearListeners();

                    IsPlayingLineInContext = true;
                    mCancellationTokenSource = new CancellationTokenSource();
                    int index = 0;
                    List<List<string>> _dialogsList = CurrentTutorialStep.PlayUserRecordedAudioInContext;
                    int _dialogLength = _dialogsList.Count;

                    foreach (List<string> dialog in _dialogsList)
                    {
                        mCancellationTokenSource.Token.ThrowIfCancellationRequested();

                        foreach (string _dialogLine in dialog)
                        {
                            mCancellationTokenSource.Token.ThrowIfCancellationRequested();

                            if (_dialogLine.Equals(mcCurrentLineName))
                            {
                                Application.Current.Dispatcher.Invoke((Action)(() =>
                                {
                                    mVoiceRecorderControlViewModel.PlayOrStop(mVoiceRecorderControlViewModel.CurrentFilePath);
                                }));
                            }
                            else
                            {
                                string path = Path.Combine(ApplicationData.Instance.VideoDirectory, _dialogLine + ".avi");

                                if (File.Exists(path))
                                {
                                    CurrentVideoFilePath = path;

                                    Application.Current.Dispatcher.Invoke((Action)(() =>
                                    {
                                        mMediaPlayerControlViewModel.StartMediaPlayer();
                                    }));
                                }
                                else
                                {
                                    mUserLogger.Error(Path.GetFileName(path) +" doesn't exist");
                                    await Task.Delay(1000);
                                }
                            }

                            mCancellationTokenSource.Token.ThrowIfCancellationRequested();

                            do
                            {
                                if (mVoiceRecorderControlViewModel.IsPlaying || mMediaPlayerControlViewModel.IsPlaying)
                                    await Task.Delay(500);   //task.delay will only logical blocks thread instead of 
                                                             //thread.sleep which blocks thread
                                }
                            while (mVoiceRecorderControlViewModel.IsPlaying || mMediaPlayerControlViewModel.IsPlaying);

                            Thread.Sleep(500);
                        }

                        if (index < _dialogLength - 1)
                            Thread.Sleep(500);

                        index++;
                    }
                }
                catch (OperationCanceledException){}
                catch (Exception ex)
                {
                    mLogger.Error("_playDialogLineInContext" + ex.Message);
                }
            });

            IsPlayingLineInContext = false;
            CurrentVideoFilePath = _tutorialStepVideoFilePathCache;
        }

        private PhraseEntry _findPhraseInCharacterForTutorialStep(TutorialStep _tutorialStep)
        {
            foreach (PhraseEntry phrase in Character.Phrases)
            {
                if (!string.IsNullOrEmpty(phrase.FileName))
                {
                    // file name is formed as BO_Greeting_xxxx(date).mp3
                    string _userRecordedFileName = phrase.FileName;
                    string _userRecordedTagName = _userRecordedFileName.Split('_')[1];
                    string _tagName = _tutorialStep.PhraseWeights.Keys.First();

                    if (_userRecordedTagName.Equals(_tagName))
                        return phrase;
                }
            }

            return null;
        }

        private void _setDataForTutorialStep(int _currentStepIndex)
        {
            try
            {
                CurrentTutorialStep = CurrentWizard.TutorialSteps[_currentStepIndex];
                CurrentVideoFilePath = Path.Combine(ApplicationData.Instance.VideoDirectory, CurrentTutorialStep.VideoFileName + ".avi");

                if (CurrentTutorialStep.CollectUserInput)
                {
                    VoiceRecorderControlViewModel.CurrentFilePath = $"{Character.CharacterPrefix}_{CurrentTutorialStep.PhraseWeights.Keys.First()}_{DateTime.Now.ToString("yyyy-dd-MM-HH-mm-ss")}";
                    PhraseEntry _currentPhrase = _findPhraseInCharacterForTutorialStep(CurrentTutorialStep);

                    if (_currentPhrase != null)
                    {
                        DialogStr = _currentPhrase.DialogStr;
                        VoiceRecorderControlViewModel.CurrentFilePath = Character.CharacterPrefix + "_" + _currentPhrase.FileName;
                        VoiceRecorderControlViewModel.IsLineRecorded = true;
                        mCurrentPhrase = _currentPhrase;
                        mIsPhraseEditable = true;
                    }
                    else
                    {
                        mIsPhraseEditable = false;
                        DialogStr = "";
                        mVoiceRecorderControlViewModel.ResetData();
                    }
                }
                else
                {
                    VoiceRecorderControlViewModel.ResetData();
                    DialogStr = "";
                }
            }
            catch (Exception ex)
            {
                mLogger.Error("_setDataForTutorialStep " + ex.Message);
            }
        }

        #endregion

        #region - state machine functions -

        private void _leaveVizard()
        {
            try
            {
                Workflow.Fire(Triggers.Start);

                mRegionManager.RequestNavigate(Constants.NavigationRegion, typeof(CharactersNavigationView).FullName);
                var _contentRegion = mRegionManager.Regions[Constants.ContentRegion];
                _contentRegion.NavigationService.Journal.GoBack();

                Reset();
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

                Workflow.Fire(Triggers.ReadyForUserAction);
            }
            else
            {
                Workflow.Fire(Triggers.LeaveWizard);
            }
        }

        private void _skipStep()
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

            Workflow.Fire(Triggers.ReadyForUserAction);
        }

        private async void _saveAndNextStep()
        {
            if (CurrentStepIndex < CurrentWizard.TutorialSteps.Count - 1
                && mCurrentTutorialStep.CollectUserInput)
            {
                if (string.IsNullOrEmpty(DialogStr))
                {
                    var result = await mMessageDialogService.ShowOKCancelDialogAsync("You didn't write text for this dialog line. Do you want to save step without it?"
                        , "Warning", "Yes", "No");

                    if (result == MessageDialogResult.Cancel)
                    {
                        Workflow.Fire(Triggers.ReadyForUserAction);
                        return;
                    }
                }

                if (mIsPhraseEditable)
                {
                    mCurrentPhrase.DialogStr = DialogStr;
                }
                else
                {
                    string[] mFileNameArray = VoiceRecorderControlViewModel.CurrentFilePath.Split('_');

                    PhraseEntry entry = new PhraseEntry
                    {
                        PhraseRating = CurrentTutorialStep.PhraseRating,
                        DialogStr = DialogStr,
                        PhraseWeights = CurrentTutorialStep.PhraseWeights,
                        FileName = $"{mFileNameArray[1]}_{mFileNameArray[2]}"
                    };

                    mCharacter.Phrases.Add(entry);
                }

                await mCharacterDataProvider.SaveAsync(Character);

                Workflow.Fire(Triggers.SkipStep);
                return;
            }

            Workflow.Fire(Triggers.Finish);
        }

        private async  void _finish()
        {
            var result = await mMessageDialogService.ShowOKCancelDialogAsync("Character successfully updated!", "Info",
                "Run another wizard", "Close wizard");

            if (result != MessageDialogResult.Cancel)
            {
                Reset();
                Workflow.Fire(Triggers.ShowFormDialog);
            }
            else
            {
                Workflow.Fire(Triggers.LeaveWizard);
            }
        }

        #endregion

        #endregion

        #region - public functions -

        public void Reset()
        {
            mCharacter = null;
            CurrentStepIndex = 0;
            DialogStr = "";
        }

        #endregion

        #region - properties -

        public WizardWorkflow Workflow { get; set; }

        public States CurrentState
        {
            get { return Workflow.State; }
            private set
            {
                mCurrentState = value;
                RaisePropertyChanged();
            }
        }

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

        public string CurrentVideoFilePath
        {
            get { return mCurrentVideoFilePath; }
            set
            {
                mCurrentVideoFilePath = value;
                RaisePropertyChanged();
            }
        }

        public bool IsPlayingLineInContext
        {
            get { return mIsPlayingLineInContext; }
            set
            {
                bool _oldValue = mIsPlayingLineInContext;
                if (_oldValue == value)
                    return;

                mIsPlayingLineInContext = value;
                RaisePropertyChanged();

                if (mIsPlayingLineInContext)
                {
                    Workflow.Fire(Triggers.VoiceRecorderPlayingInContext);
                }
                else
                {
                    Workflow.Fire(Triggers.ReadyForUserAction);
                }
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
