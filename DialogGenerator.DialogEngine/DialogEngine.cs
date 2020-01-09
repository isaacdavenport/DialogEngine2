using DialogGenerator.CharacterSelection;
using DialogGenerator.Core;
using DialogGenerator.DialogEngine.Model;
using DialogGenerator.DialogEngine.Workflow;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using DialogGenerator.Utilities;
using Prism.Events;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DialogGenerator.DialogEngine
{
    public class DialogEngine:IDialogEngine
    {
        #region - fields -

        private ILogger mLogger;
        private IUserLogger mUserLogger;
        private IEventAggregator mEventAggregator;
        private IMP3Player mPlayer;
        private ICharacterSelectionFactory mCharacterSelectionFactory;
        private ICharacterSelection mCharacterSelection;
        private States mCurrentState;
        private int mPriorCharacter1Num = 100;
        private int mPriorCharacter2Num = 100;
        private int mIndexOfCurrentDialogModel;
        private bool mIsDialogCancelled;
        private DialogContext mContext;
        private DialogModelsManager mDialogModelsManager;
        private CharactersManager mCharactersManager;
        private DialogEngineWorkflow mWorkflow;
        private SelectedCharactersPairEventArgs mRandomSelectionDataCached;
        private CancellationTokenSource mCancellationTokenSource;
        private CancellationTokenSource mStateMachineTaskTokenSource = new CancellationTokenSource();

        #endregion

        #region - constructor -

        public DialogEngine(ILogger logger,IUserLogger _userLogger,IEventAggregator _eventAggregator, 
            IMP3Player player,
            ICharacterSelectionFactory _characterSelectionFactory,
            DialogContext context,
            DialogModelsManager _dialogModelsManager,
            CharactersManager _charactersManager)        
        {
            mLogger = logger;
            mUserLogger = _userLogger;
            mEventAggregator = _eventAggregator;
            mPlayer = player;
            mCharacterSelectionFactory = _characterSelectionFactory;
            mContext = context;
            mDialogModelsManager = _dialogModelsManager;
            mCharactersManager = _charactersManager;
            mWorkflow = new DialogEngineWorkflow(() => { });

            _configureWorkflow();
            _subscribeForEvents();
        }

        #endregion

        #region - private functions -

        private void _configureWorkflow()
        {
            mWorkflow.Configure(States.Start)
                .Permit(Triggers.PrepareDialogParameters, States.PreparingDialogParameters)
                .PermitReentry(Triggers.StartDialog);
                

            mWorkflow.Configure(States.PreparingDialogParameters)
                .PermitReentry(Triggers.PrepareDialogParameters)
                .Permit(Triggers.StartDialog, States.DialogStarted)
                .Permit(Triggers.FinishDialog, States.DialogFinished);

            mWorkflow.Configure(States.DialogStarted)
                .Permit(Triggers.PrepareDialogParameters, States.PreparingDialogParameters)
                .Permit(Triggers.FinishDialog, States.DialogFinished);

            mWorkflow.Configure(States.DialogFinished)
                .Permit(Triggers.PrepareDialogParameters, States.PreparingDialogParameters)
                .PermitReentry(Triggers.FinishDialog);
        }

        private void _subscribeForEvents()
        {
            mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Subscribe(_onSelectedCharactersPairChanged);            
            mEventAggregator.GetEvent<ChangedCharacterStateEvent>().Subscribe(_onChangedCharacterState);
            mEventAggregator.GetEvent<ChangedDialogModelStateEvent>().Subscribe(_onChangedDialogModelState);
            mEventAggregator.GetEvent<CharacterUpdatedEvent>().Subscribe(_onCharacterUpdated);

            mWorkflow.PropertyChanged += _mWorkflow_PropertyChanged;
            Session.SessionPropertyChanged += _sessionPropertyChanged;
        }

        /// <summary>
        /// S.Ristic Fix of the DLGEN-401 - 10/07/2019.
        /// Handler of the CharacterUpdatedEvent
        /// </summary>
        private void _onCharacterUpdated()
        {
            this.Initialize();
        }

        private void _onChangedDialogModelState()
        {
            mDialogModelsManager.Initialize();
        }

        private void _onChangedCharacterState()
        {
            mRandomSelectionDataCached = null;

            mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();
            mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Unsubscribe(_onSelectedCharactersPairChanged);
            Session.Set(Constants.COMPLETED_DLG_MODELS, 0);
            mStateMachineTaskTokenSource.Cancel();
        }

        private void _sessionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case Constants.SELECTED_DLG_MODEL:
                    {                        
                        mEventAggregator.GetEvent<StopPlayingCurrentDialogLineEvent>().Publish();

                        // S.Ristic - 12/13/2019 
                        // I have inserted this condition because if this happens while system
                        // is starting up the dialog engine is unsubcribed from this event before the
                        // character selection initialization and therefore the conversation doesn't start
                        // until one of the characters is changed.
                        int _completedDialogModels = Session.Get<int>(Constants.COMPLETED_DLG_MODELS);
                        if(_completedDialogModels != 0)
                        {
                            mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Unsubscribe(_onSelectedCharactersPairChanged);
                        }
                        
                        mRandomSelectionDataCached = null;
                        mStateMachineTaskTokenSource.Cancel();
                        break;
                    }
            }
        }

        private void _mWorkflow_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("State"))
            {
                mCurrentState = mWorkflow.State;
            }
        }

        private void _onSelectedCharactersPairChanged(SelectedCharactersPairEventArgs obj)
        {
            mRandomSelectionDataCached = obj;
            Session.Set(Constants.COMPLETED_DLG_MODELS, 0);

            if (Session.Get<bool>(Constants.BLE_MODE_ON) && mCurrentState != States.PreparingDialogParameters)
            {
                mStateMachineTaskTokenSource.Cancel();
                mWorkflow.Fire(Triggers.PrepareDialogParameters);
            }

            mLogger.Debug($"_onSelectedCharactersPairChanged- c_ch1 - {mRandomSelectionDataCached?.Character1Index} c_ch2-{mRandomSelectionDataCached?.Character2Index} " +
                $"- ch1:{obj.Character1Index} ch2: {obj.Character2Index} ", ApplicationData.Instance.DialogLoggerKey);
        }

        private void _playAudio(string _pathAndFileName)
        {
            try
            {
                string _fileName;
                var i = 0;
                bool _isPlaying = false;

                if (File.Exists(_pathAndFileName))
                {
                    _fileName = _pathAndFileName;
                }
                else
                {
                    _fileName = Path.Combine(ApplicationData.Instance.AudioDirectory, DialogEngineConstants.SilenceFileName);
                    mUserLogger.Warning("File: " + Path.GetFileName(_pathAndFileName) + " doesn't exist.");
                    mLogger.Warning("File: " + Path.GetFileName(_pathAndFileName) + " doesn't exist.", ApplicationData.Instance.DialogLoggerKey);
                }

                Thread.Sleep(300);

                var _playSuccess = mPlayer.Play(_fileName);

                if (_playSuccess != 0)
                {
                    mUserLogger.Error("MP3 Play Error  ---  " + _playSuccess);
                    mLogger.Info("MP3 Play Error  ---  " + _playSuccess);
                }

                do
                {
                    Thread.Sleep(800);

                    _isPlaying = mPlayer.IsPlaying();

                    Thread.Sleep(100);
                    i++;
                }
                while (_isPlaying && i < 400);  // don't get stuck,, 40 seconds max phrase

                Thread.Sleep((int)ApplicationData.Instance.DelayBetweenPhrases*1000); // wait around a second after the audio is done for between phrase pause
            }
            catch (Exception ex)
            {
                mLogger.Error(" _playAudio " + ex.Message);
            }
        }

        private void _addPhraseToHistory(PhraseEntry _selectedPhrase, int _speakingCharacter)
        {
            mContext.HistoricalPhrases.Add(new HistoricalPhrase
            {
                CharacterIndex = _speakingCharacter,
                CharacterPrefix = mContext.CharactersList[_speakingCharacter].CharacterPrefix,
                PhraseIndex = mContext.CharactersList[_speakingCharacter].Phrases.IndexOf(_selectedPhrase),
                PhraseFile = _selectedPhrase.FileName,
                StartedTime = DateTime.Now
            });

            mLogger.Info(mContext.CharactersList[_speakingCharacter].CharacterName + ": " + _selectedPhrase.DialogStr);
            mUserLogger.Info(mContext.CharactersList[_speakingCharacter].CharacterName + ": " + _selectedPhrase.DialogStr);
        }

        private void _addDialogModelToHistory(int _dialogModelIndex, int _ch1, int _ch2)
        {
            mContext.HistoricalDialogs.Add(new HistoricalDialog
            {
                DialogIndex = _dialogModelIndex,
                DialogName = mContext.DialogModelsList[_dialogModelIndex].Name,
                StartedTime = DateTime.Now,
                Completed = false,
                Character1 = _ch1,
                Character2 = _ch2
            });
        }

        private bool _setNextCharacters()
        {
            SelectedCharactersPairEventArgs args = mRandomSelectionDataCached;
            if (args == null || args.Character1Index < 0 || args.Character1Index >= mContext.CharactersList.Count
                || args.Character2Index < 0 || args.Character2Index >= mContext.CharactersList.Count ||
                args.Character1Index == args.Character2Index)
            {
                return false;
            }

            mContext.Character1Num = args.Character1Index;
            mContext.Character2Num = args.Character2Index;

            var _tempChar1 = mContext.Character1Num;
            var _tempChar2 = mContext.Character2Num;

            if (_tempChar1 == _tempChar2 || _tempChar1 >= mContext.CharactersList.Count || _tempChar2 >= mContext.CharactersList.Count)
                return false;

            mContext.SameCharactersAsLast = (_tempChar1 == mPriorCharacter1Num || _tempChar1 == mPriorCharacter2Num)
                                            && (_tempChar2 == mPriorCharacter1Num || _tempChar2 == mPriorCharacter2Num);

            mContext.Character1Num = _tempChar1;
            mContext.Character2Num = _tempChar2;
            mPriorCharacter1Num = mContext.Character1Num;
            mPriorCharacter2Num = mContext.Character2Num;

            return true;
        }

        private bool _dialogTrackerAndBLESelectedCharactersSame()
        {
            if ((mContext.Character1Num == Session.Get<int>(Constants.NEXT_CH_1)
                 || mContext.Character1Num == Session.Get<int>(Constants.NEXT_CH_2))
                 && (mContext.Character2Num == Session.Get<int>(Constants.NEXT_CH_2)
                 || mContext.Character2Num == Session.Get<int>(Constants.NEXT_CH_1)))
            {
                return true;
            }
            return false;
        }

        private Triggers _prepareDialogParameters(CancellationToken token)
        {
            try
            {
                if(!mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Contains(_onSelectedCharactersPairChanged))
                {
                    mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Subscribe(_onSelectedCharactersPairChanged);
                }

                token.ThrowIfCancellationRequested();

                if (mRandomSelectionDataCached == null)
                    return Triggers.PrepareDialogParameters;

                if (!_setNextCharacters())
                    return Triggers.PrepareDialogParameters;

                token.ThrowIfCancellationRequested();

                mIndexOfCurrentDialogModel = Session.Get<int>(Constants.SELECTED_DLG_MODEL) >= 0
                                            ? Session.Get<int>(Constants.SELECTED_DLG_MODEL)
                                            : mDialogModelsManager.PickAWeightedDialog();

                token.ThrowIfCancellationRequested();
                _addDialogModelToHistory(mIndexOfCurrentDialogModel, mContext.Character1Num, mContext.Character2Num);

                return Triggers.StartDialog;
            }
            catch (OperationCanceledException)
            {
                return Triggers.FinishDialog;
            }
            catch (Exception ex)
            {
                mLogger.Error("_prepareDialogParameters " + ex.Message);
            }

            return Triggers.PrepareDialogParameters;
        }

        private  Triggers _startDialog(CancellationToken token)
        {
            try
            {
                System.Console.WriteLine("Dialog {0} started", mIndexOfCurrentDialogModel);

                var _speakingCharacter = mContext.Character1Num;
                var _selectedPhrase = mContext.CharactersList[_speakingCharacter].Phrases[0]; //initialize to unused placeholder phrase

                string _debugMessage = "_startDialog " + mContext.CharactersList[mContext.Character1Num].CharacterPrefix + " and " +
                    mContext.CharactersList[mContext.Character2Num].CharacterPrefix + " " + string.Join("; --,",
                    mContext.DialogModelsList[mIndexOfCurrentDialogModel].PhraseTypeSequence.ToArray());

                mLogger.Debug(_debugMessage,ApplicationData.Instance.DialogLoggerKey);
                mUserLogger.Info(_debugMessage);

                if (ApplicationData.Instance.TextDialogsOn && !mContext.SameCharactersAsLast)
                {
                    mEventAggregator.GetEvent<ActiveCharactersEvent>().
                        Publish($" {mContext.CharactersList[mContext.Character1Num].CharacterName} , {mContext.CharactersList[mContext.Character2Num].CharacterName} ");
                }

                foreach (var _currentPhraseType in mContext.DialogModelsList[mIndexOfCurrentDialogModel].PhraseTypeSequence)
                {
                    token.ThrowIfCancellationRequested();

                    if (mContext.CharactersList[_speakingCharacter].PhraseTotals.PhraseWeights.ContainsKey(_currentPhraseType))
                    {
                        if (mContext.CharactersList[_speakingCharacter].PhraseTotals.PhraseWeights[_currentPhraseType] < 0.01f)
                        {
                            mUserLogger.Warning("Missing PhraseType: " + _currentPhraseType);
                            mLogger.Info("Missing PhraseType: " + _currentPhraseType);
                        }

                        token.ThrowIfCancellationRequested();

                        _selectedPhrase = mDialogModelsManager.PickAWeightedPhrase(_speakingCharacter, _currentPhraseType);

                        if (_selectedPhrase == null)
                        {
                            mUserLogger.Warning("Phrase type " + _currentPhraseType + " was not found.");
                            mLogger.Info("Phrase type " + _currentPhraseType + " was not found.");
                            continue;
                        }

                        token.ThrowIfCancellationRequested();

                        if (ApplicationData.Instance.TextDialogsOn)
                        {
                            mEventAggregator.GetEvent<NewDialogLineEvent>().Publish(new NewDialogLineEventArgs
                            {
                                Character = mContext.CharactersList[_speakingCharacter],
                                DialogLine = _selectedPhrase.DialogStr
                            });
                        }

                        token.ThrowIfCancellationRequested();

                        _addPhraseToHistory(_selectedPhrase, _speakingCharacter);

                        var _pathAndFileName = Path.Combine(ApplicationData.Instance.AudioDirectory,
                                               mContext.CharactersList[_speakingCharacter].CharacterPrefix
                                              + "_" + _selectedPhrase.FileName + ".mp3");

                        _playAudio(_pathAndFileName); // vb: code stops here so commented out for debugging purpose

                        if (!_dialogTrackerAndBLESelectedCharactersSame() && Session.Get<bool>(Constants.BLE_MODE_ON))
                        {
                            mContext.SameCharactersAsLast = false;
                            return Triggers.PrepareDialogParameters; // the characters have moved  TODO break into charactersSame() and use also with prior
                        }
                        //Toggle character
                        if (_speakingCharacter == mContext.Character1Num) //toggle which character is speaking next
                            _speakingCharacter = mContext.Character2Num;
                        else
                            _speakingCharacter = mContext.Character1Num;
                    }

                    mContext.HistoricalDialogs[mContext.HistoricalDialogs.Count - 1].Completed = true;

                    if (mContext.HistoricalDialogs.Count > 2000)
                        mContext.HistoricalDialogs.RemoveRange(0, 100);

                    if (mContext.HistoricalPhrases.Count > 8000)
                        mContext.HistoricalPhrases.RemoveRange(0, 100);

                    mContext.RecentDialogs.Dequeue(); //TODO move to use HistoricalDialogs
                    mContext.RecentDialogs.Enqueue(mIndexOfCurrentDialogModel);
                }

                int _completedDlgModels = Session.Get<int>(Constants.COMPLETED_DLG_MODELS);
                Session.Set(Constants.COMPLETED_DLG_MODELS, ++_completedDlgModels);
            }
            catch (OperationCanceledException)
            {
                return Triggers.FinishDialog;
            }
            catch (Exception ex)
            {
                mLogger.Error("_startDialog ended in exception " + ex.Message);
            }

            System.Console.WriteLine("Dialog {0} stopped regularly", mIndexOfCurrentDialogModel);

            return Triggers.PrepareDialogParameters;
        }

        private bool _checkIsCreateCharacterSession()
        {
            if (Session.Contains(Constants.CHARACTER_EDIT_MODE) && (bool)Session.Get(Constants.CHARACTER_EDIT_MODE))
            {
                return true;
            }

            return false;
        }

        #endregion

        #region - public functions -

        public void Initialize()
        {
            mDialogModelsManager.Initialize();
            mCharactersManager.Initialize();
        }

        public async Task StartDialogEngine()
        {
            System.Console.WriteLine("Dialog engine started");

            mIsDialogCancelled = false;
            Task _characterSelectionTask;


            // S.Ristic 12/15/2019
            // This additional check is consequence of adding of the new setting according
            // to request in DLGEN-420.
            if(!ApplicationData.Instance.IgnoreRadioSignals)
            {
                mCharacterSelection = Session.Get<bool>(Constants.BLE_MODE_ON)
                ? mCharacterSelectionFactory.Create(SelectionMode.BLESelectionMode)
                : mCharacterSelectionFactory.Create( SelectionMode.ArenaModel);
            } else
            {
                Session.Set(Constants.BLE_MODE_ON, false);
                mCharacterSelection = mCharacterSelectionFactory.Create(SelectionMode.ArenaModel);
                
            }

            mEventAggregator.GetEvent<CharacterSelectionModelChangedEvent>().Publish();
            
            mCancellationTokenSource = new CancellationTokenSource();

            if (mCurrentState != States.PreparingDialogParameters)
                mWorkflow.Fire(Triggers.PrepareDialogParameters);

            await Task.Run(async() =>
            {
                Thread.CurrentThread.Name = "DialogGeneratorThread";
                Console.WriteLine(Thread.CurrentThread.Name + " started!");
                mLogger.Debug(Thread.CurrentThread.Name + " started!");

                _characterSelectionTask= mCharacterSelection.StartCharacterSelection();
                mCharactersManager.Initialize();  //TODO Isaac added so that character updates recalculate phraseweights
                do
                {
                    if(Session.Contains(Constants.NEEDS_RESTART) && Session.Get<bool>(Constants.NEEDS_RESTART))
                    {
                        await _characterSelectionTask;

                        bool _radioModeOn = Session.Get<bool>(Constants.BLE_MODE_ON);
                        if (!ApplicationData.Instance.IgnoreRadioSignals)
                        {
                            mCharacterSelection = Session.Get<bool>(Constants.BLE_MODE_ON)
                            ? mCharacterSelectionFactory.Create(SelectionMode.BLESelectionMode)
                            : mCharacterSelectionFactory.Create(SelectionMode.ArenaModel);
                        }
                        else
                        {
                            Session.Set(Constants.BLE_MODE_ON, false);
                            mCharacterSelection = mCharacterSelectionFactory.Create(SelectionMode.ArenaModel);

                        }

                        mEventAggregator.GetEvent<CharacterSelectionModelChangedEvent>().Publish();
                        _characterSelectionTask = mCharacterSelection.StartCharacterSelection();
                        Session.Set(Constants.NEEDS_RESTART, false);
                    }

                    if(mStateMachineTaskTokenSource != null && mIsDialogCancelled
                       && mWorkflow.CanFire(Triggers.FinishDialog))
                    {
                        mWorkflow.Fire(Triggers.FinishDialog);
                    }
                    else
                    {
                        mStateMachineTaskTokenSource = new CancellationTokenSource();
                    }

                    switch (mWorkflow.State)
                    {
                        case States.PreparingDialogParameters:
                            {
                                Triggers _nextTrigger = _prepareDialogParameters(mStateMachineTaskTokenSource.Token);
                                if (mWorkflow.CanFire(_nextTrigger))
                                    mWorkflow.Fire(_nextTrigger);
                                break;
                            }
                        case States.DialogStarted:
                            {
                                Triggers _nextTrigger = _startDialog(mStateMachineTaskTokenSource.Token);
                                string _debugMessage = "Start dialog returned " + _nextTrigger.ToString();
                                mLogger.Debug(_debugMessage, ApplicationData.Instance.DialogLoggerKey);

                                if (mWorkflow.CanFire(_nextTrigger))
                                    mWorkflow.Fire(_nextTrigger);
                                else
                                {
                                    System.Console.WriteLine("Triger {0} cannot be started!", _nextTrigger);
                                }
                                break;
                            }
                        case States.DialogFinished:
                            {
                                // we need to track is "Stop dialog" btn pressed, because we can arrive in this 
                                // state if event with new characters fire
                                if (mIsDialogCancelled)
                                {
                                    mCancellationTokenSource.Cancel();
                                    mIsDialogCancelled = false;
                                }
                                else
                                {
                                    // if task is cancelled but we didn't stop dialog in case event with new characters occured
                                    if (mWorkflow.CanFire(Triggers.PrepareDialogParameters))
                                        mWorkflow.Fire(Triggers.PrepareDialogParameters);
                                }
                                break;
                            }
                    }
                }
                while (!mCancellationTokenSource.Token.IsCancellationRequested);

                await _characterSelectionTask;

                Console.WriteLine(Thread.CurrentThread.Name + " stopped!");
                mLogger.Debug(Thread.CurrentThread.Name + " stopped!");

            });
        }

        public void StopDialogEngine()
        {
            //stop .mp3 player if playing
            mEventAggregator.GetEvent<StopImmediatelyPlayingCurrentDialogLIne>().Publish();

            mIsDialogCancelled = true;
            mStateMachineTaskTokenSource.Cancel();
            mCharacterSelection.StopCharacterSelection();
            mRandomSelectionDataCached = null;
            Session.Set(Constants.COMPLETED_DLG_MODELS, 0);

            if (mCurrentState != States.DialogFinished)
                mWorkflow.Fire(Triggers.FinishDialog);

            System.Console.WriteLine("Dialog Engine Stopped");
        }

        #endregion
    }
}
