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
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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
        private SelectedCharactersPairEventArgs mCharacterPairSelectionDataCached;
        private CancellationTokenSource mCancellationTokenSource;
        private CancellationTokenSource mStateMachineTaskTokenSource = new CancellationTokenSource();
        private bool mRunning;
        private CancellationTokenSource mPauseCancellationTokenSource;
        private int mRunningDialogIndex = 0;
        private bool mCharacterSwapRequired = false;

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion

        #region - properties -

        public int DialogsCountBeforeSwap { get; set; } = 4;

        public CancellationTokenSource PauseCancellationTokenSource
        {
            get
            {
                return mPauseCancellationTokenSource;
            }

            set
            {
                mPauseCancellationTokenSource = value;
                OnPropertyChanged();
            }
        }

        public bool Running { 
            get
            {
                return mRunning;
            }

            set
            {
                mRunning = value;
                OnPropertyChanged();
            }
        }

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

            Running = false;
            PauseCancellationTokenSource = null;
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
            mCharacterPairSelectionDataCached = null;

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
                        
                        mCharacterPairSelectionDataCached = null;
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
            mCharacterSwapRequired = false;
            mRunningDialogIndex = 0;            
            mCharacterPairSelectionDataCached = obj;
            Session.Set(Constants.COMPLETED_DLG_MODELS, 0);

            if (Session.Get<bool>(Constants.BLE_MODE_ON) && mCurrentState != States.PreparingDialogParameters)
            {
                mStateMachineTaskTokenSource.Cancel();
                mWorkflow.Fire(Triggers.PrepareDialogParameters);
            }

            if(obj != null && mContext.CharactersList.Any())
            {
                var char1Name = obj.Character1Index != -1
                    ? mContext.CharactersList[obj.Character1Index].CharacterName
                    : "None";
                
                var char2Name = obj.Character2Index != -1
                    ? mContext.CharactersList[obj.Character2Index].CharacterName
                    : "None";
                
                mLogger.Info($"_onSelectedCharacterPairChanged - Character 1 is set to {obj.Character1Index} " +
                             $" {char1Name}, character 2 is set to {obj.Character2Index} {char2Name}");

            } else
            {
                mLogger.Debug("_onSelectedCharactersPairChanged was passed a null pair of selected characters");
            }
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
                } else
                {
                    mLogger.Info("MP3 _playAudio played " + _pathAndFileName);
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

            mLogger.Info("=== " + mContext.CharactersList[_speakingCharacter].CharacterName + ": _addPhraseToHistory - " +
                 (_selectedPhrase.DialogStr.Length <= 90 ? _selectedPhrase.DialogStr : (_selectedPhrase.DialogStr.Substring(0, 88)) + "..."));
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
            SelectedCharactersPairEventArgs args = mCharacterPairSelectionDataCached;
            if (args == null || args.Character1Index < 0 || args.Character1Index >= mContext.CharactersList.Count
                || args.Character2Index < 0 || args.Character2Index >= mContext.CharactersList.Count /* ||
                args.Character1Index == args.Character2Index *//* Sinisa 02/05/2020 - DLGEN-438 */)
            {
                mLogger.Info($"_setNextCharacters could not set characters from cached values");
                return false;
            }

            mContext.Character1Num = args.Character1Index;
            mContext.Character2Num = args.Character2Index;

            var _tempChar1 = mContext.Character1Num;
            var _tempChar2 = mContext.Character2Num;

            if (_tempChar1 >= mContext.CharactersList.Count || _tempChar2 >= mContext.CharactersList.Count)
                return false;  //this appears redundant to the first check of args

            mContext.SameCharactersAsLast = (_tempChar1 == mPriorCharacter1Num || _tempChar1 == mPriorCharacter2Num)
                                            && (_tempChar2 == mPriorCharacter1Num || _tempChar2 == mPriorCharacter2Num);

            if (mContext.SameCharactersAsLast) {
                mLogger.Info($"_setNextCharacters are same pair as before, potentially in a different order");
            } else
            {
                mLogger.Info($"_setNextCharacters set to a different pair of characters");

            }
            mContext.Character1Num = _tempChar1;
            mContext.Character2Num = _tempChar2;
            mPriorCharacter1Num = mContext.Character1Num;
            mPriorCharacter2Num = mContext.Character2Num;

            // Notify the interested parties that the selected characters are about 
            // to start the conversation.
            mEventAggregator.GetEvent<CharactersInConversationEvent>().Publish(args);

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

                if (mCharacterPairSelectionDataCached == null)
                {
                    mLogger.Info($"_prepareDialogParameters received mCharacterPairSelectionDataCached == null");
                    Thread.Sleep(500);  // prevents log from filling up with this message when other threads not ready
                    return Triggers.PrepareDialogParameters;
                }

                if (mCharacterPairSelectionDataCached.Character1Index == -1 || mCharacterPairSelectionDataCached.Character2Index == -1)
                {
                    mLogger.Info($"_prepareDialogParameters received mCharacterPairSelectionDataCached == -1");
                    Thread.Sleep(500);
                    return Triggers.PrepareDialogParameters;
                }

                if (Session.Get<bool>(Constants.NEEDS_RESTART) || Session.Get<bool>(Constants.CANCEL_DIALOG))
                {
                    mLogger.Info($"_prepareDialogParameters got cancellation request");
                    Session.Set(Constants.CANCEL_DIALOG, false);
                    return Triggers.PrepareDialogParameters;
                }

                if (!_setNextCharacters())
                {
                    mLogger.Info($"_prepareDialogParameters was not able to _setNextCharacters");
                    return Triggers.PrepareDialogParameters;
                }

                token.ThrowIfCancellationRequested();

                // TODO Isaac.  Why is this turnary here if it always resolves to -1 and always does PickAWeightedDialog?
                mIndexOfCurrentDialogModel = Session.Get<int>(Constants.SELECTED_DLG_MODEL) >= 0
                                            ? Session.Get<int>(Constants.SELECTED_DLG_MODEL)
                                            : mDialogModelsManager.PickAWeightedDialog();

                token.ThrowIfCancellationRequested();

                if(mIndexOfCurrentDialogModel != -1)
                {
                    _addDialogModelToHistory(mIndexOfCurrentDialogModel, mContext.Character1Num, mContext.Character2Num);

                    // If previous character pair had no common dialogs, cancel the old state and notify GUI.
                    if(mContext.NoDialogs)
                    {
                        mContext.NoDialogs = false;
                        mEventAggregator.GetEvent<CharactersHaveDialogsEvent>().Publish(true);

                        mLogger.Info($"The characters ({mContext.Character1Num}) and ({mContext.Character2Num}) have dialog models again.");
                    }

                } else
                {
                    if(!mCharacterSwapRequired)
                    {
                        // Swap the characters
                        mLogger.Info($"PREPARE DIALOG PARAMETERS - Trying to force the swapping of characters ({mContext.Character1Num}) and ({mContext.Character2Num})");

                        // swap the characters
                        mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Publish(new SelectedCharactersPairEventArgs
                        {
                            Character1Index = mContext.Character2Num,
                            Character2Index = mContext.Character1Num
                        });

                        // set the flag and call the PREPARE DIALOG PARAMETERS AGAIN
                        mCharacterSwapRequired = true;
                        return Triggers.PrepareDialogParameters;
                    } else
                    {
                        // Display the warning that the characters have no common dialogs.
                        if(!mContext.NoDialogs)
                        {
                            mContext.NoDialogs = true;
                            mLogger.Info($"The characters ({mContext.Character1Num}) and ({mContext.Character2Num}) still don't have dialogs after the second swap!");
                        }

                        // Notify GUI that the characters have no common dialog lines.
                        mEventAggregator.GetEvent<CharactersHaveDialogsEvent>().Publish(false);

                        // RETURN PREPARE DIALOG PARAMETERS AGAIN.
                        return Triggers.PrepareDialogParameters;

                    }
                }

                return Triggers.StartDialog;
            }
            catch (OperationCanceledException)
            {
                mLogger.Info($"_prepareDialogParameters received exception request for cancellation");
                return Triggers.FinishDialog;
            }
            catch (Exception ex)
            {
                mLogger.Error("_prepareDialogParameters " + ex.Message);
            }
            mLogger.Info($"_prepareDialogParameters did not terminate with an event re-triggering");

            return Triggers.PrepareDialogParameters;
        }

        private  async Task<Triggers> _startDialog(CancellationToken token)
        {
            if (mIndexOfCurrentDialogModel < 0 || mIndexOfCurrentDialogModel >= mContext.DialogModelsList.Count)
            {
                mLogger.Info("_startDialog has invalid mIndexOfCurrentDialogModel " + mIndexOfCurrentDialogModel);
                return Triggers.FinishDialog;
            }
            
            try
            {
                System.Console.WriteLine("Dialog {0} started", mIndexOfCurrentDialogModel);
                mLogger.Info("Dialog started number " + mIndexOfCurrentDialogModel + " " +
                    String.Join(" ", mContext.DialogModelsList[mIndexOfCurrentDialogModel].PhraseTypeSequence.ToArray()));


                if (ApplicationData.Instance.ForceCharacterSwap && !mCharacterSwapRequired)
                {
                    if (mRunningDialogIndex != 0 && mRunningDialogIndex % ApplicationData.Instance.CharacterSwapInterval == 0)
                    {
                        mLogger.Info("_startDialog is swapping characters after CharacterSwapInterval reached");
                        mUserLogger.Info("_startDialog is swapping characters after CharacterSwapInterval reached");

                        // Force swapping of the characters
                        mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Publish(new SelectedCharactersPairEventArgs
                        {
                            Character1Index = mContext.Character2Num,
                            Character2Index = mContext.Character1Num
                        });

                        // return PREPARE DIALOG PARAMETERS
                        return Triggers.PrepareDialogParameters;
                    }
                }

                mRunningDialogIndex++;

                //var _speakingCharacter = mFirstCharacterSpeaking ? mContext.Character1Num : mContext.Character2Num;
                var _speakingCharacter = mContext.Character1Num;
                var _selectedPhrase = mContext.CharactersList[_speakingCharacter].Phrases[0]; //initialize to unused placeholder phrase

                string _debugMessage = "___startDialog " + mContext.CharactersList[mContext.Character1Num].CharacterPrefix
                    + " and " + mContext.CharactersList[mContext.Character2Num].CharacterPrefix + " dialog count " + mRunningDialogIndex + 
                    " +++" + string.Join(" +++", mContext.DialogModelsList[mIndexOfCurrentDialogModel].PhraseTypeSequence.ToArray());

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
                    if(Session.Get<bool>(Constants.NEEDS_RESTART) || Session.Get<bool>(Constants.CANCEL_DIALOG))
                    {
                        Session.Set(Constants.CANCEL_DIALOG, false);
                        throw (new OperationCanceledException());
                    }

                    if (mContext.CharactersList[_speakingCharacter].PhraseTotals.PhraseWeights.ContainsKey(_currentPhraseType))
                    {
                        if (mContext.CharactersList[_speakingCharacter].PhraseTotals.PhraseWeights[_currentPhraseType] < 0.01f)
                        {
                            mUserLogger.Warning("Missing PhraseType: " + _currentPhraseType);
                            mLogger.Info("Missing PhraseType: " + _currentPhraseType);
                        }

                        token.ThrowIfCancellationRequested();
                        if (Session.Get<bool>(Constants.NEEDS_RESTART) || Session.Get<bool>(Constants.CANCEL_DIALOG))
                        {
                            Session.Set(Constants.CANCEL_DIALOG, false);
                            throw (new OperationCanceledException());
                        }

                        mLogger.Info(mRunningDialogIndex + ".. " + mContext.CharactersList[_speakingCharacter] + " selecting a " 
                            + _currentPhraseType);

                        _selectedPhrase = mDialogModelsManager.PickAWeightedPhrase(_speakingCharacter, _currentPhraseType);

                        if (_selectedPhrase == null || _selectedPhrase.DialogStr == " .... ")
                        {
                            mUserLogger.Warning("Phrase type " + _currentPhraseType + " was not found.");
                            mLogger.Info("Phrase type " + _currentPhraseType + " was not found.");
                            continue;
                        }

                        token.ThrowIfCancellationRequested();
                        if (Session.Get<bool>(Constants.NEEDS_RESTART) || Session.Get<bool>(Constants.CANCEL_DIALOG))
                        {
                            Session.Set(Constants.CANCEL_DIALOG, false);
                            throw (new OperationCanceledException());
                        }

                        if (ApplicationData.Instance.TextDialogsOn)
                        {
                            mEventAggregator.GetEvent<NewDialogLineEvent>().Publish(new NewDialogLineEventArgs
                            {
                                Character = mContext.CharactersList[_speakingCharacter],
                                DialogLine = _selectedPhrase.DialogStr
                            });
                        }

                        token.ThrowIfCancellationRequested();
                        if (Session.Get<bool>(Constants.NEEDS_RESTART) || Session.Get<bool>(Constants.CANCEL_DIALOG))
                        {
                            Session.Set(Constants.CANCEL_DIALOG, false);
                            throw (new OperationCanceledException());
                        }

                        mUserLogger.Info(mContext.CharactersList[_speakingCharacter].CharacterName + ":: " + 
                            _currentPhraseType + ": " + _selectedPhrase.DialogStr);
                        
                        _addPhraseToHistory(_selectedPhrase, _speakingCharacter);

                        var _pathAndFileName = Path.Combine(ApplicationData.Instance.AudioDirectory,
                                               mContext.CharactersList[_speakingCharacter].CharacterPrefix
                                              + "_" + _selectedPhrase.FileName + ".mp3");

                        _playAudio(_pathAndFileName); 

                        if(PauseCancellationTokenSource != null)
                        {
                            await PauseEngine(PauseCancellationTokenSource.Token);
                            PauseCancellationTokenSource = null;
                        }

                        if (!_dialogTrackerAndBLESelectedCharactersSame() && Session.Get<bool>(Constants.BLE_MODE_ON))
                        {
                            mContext.SameCharactersAsLast = false;
                            return Triggers.PrepareDialogParameters; 
                        }
                        //Toggle character
                        if (_speakingCharacter == mContext.Character1Num) //toggle which character is speaking next
                            _speakingCharacter = mContext.Character2Num;
                        else
                            _speakingCharacter = mContext.Character1Num;
                    } else
                    {
                        mLogger.Info(mContext.CharactersList[_speakingCharacter].CharacterName + " missing a " 
                            + _currentPhraseType);
                    }

                    if (mContext.HistoricalDialogs.Count > 0)
                    {
                        mContext.HistoricalDialogs[mContext.HistoricalDialogs.Count - 1].Completed = true;
                    }
                    

                    if (mContext.HistoricalDialogs.Count > 2000)
                        mContext.HistoricalDialogs.RemoveRange(0, 100);

                    if (mContext.HistoricalPhrases.Count > 8000)
                        mContext.HistoricalPhrases.RemoveRange(0, 100);
                    
                }

                if (!mContext.FirstRoundGone)
                    mContext.FirstRoundGone = true;

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

        #region - protected functions -

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
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
                Running = true;
                Thread.CurrentThread.Name = "DialogGeneratorThread";
                Console.WriteLine(Thread.CurrentThread.Name + " started!");
                mLogger.Debug(Thread.CurrentThread.Name + " started!");
                mLogger.Info("Starting Dialog Engine", ApplicationData.Instance.BLEVectorsLoggerKey);


                _characterSelectionTask = mCharacterSelection.StartCharacterSelection();
                mCharactersManager.Initialize();  
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
                                Triggers _nextTrigger = await _startDialog(mStateMachineTaskTokenSource.Token);
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

                Running = false;
                Console.WriteLine(Thread.CurrentThread.Name + " stopped!");
                mLogger.Info(Thread.CurrentThread.Name + " stopped!");

            });
        }

        public void StopDialogEngine()
        {
            //stop .mp3 player if playing
            mEventAggregator.GetEvent<StopImmediatelyPlayingCurrentDialogLIne>().Publish();

            mIsDialogCancelled = true;
            mStateMachineTaskTokenSource.Cancel();
            mCharacterSelection.StopCharacterSelection();
            mCharacterPairSelectionDataCached = null;
            Session.Set(Constants.COMPLETED_DLG_MODELS, 0);

            if (mCurrentState != States.DialogFinished)
                mWorkflow.Fire(Triggers.FinishDialog);

            System.Console.WriteLine(Thread.CurrentThread.Name + " Stopped requested");
            mLogger.Info(Thread.CurrentThread.Name + " stop requested");
        }

        #endregion

        public async Task PauseEngine(CancellationToken cancellationToken)
        {
            bool isWaiting = true;
            Running = false;
            while(isWaiting)
            {
                try
                {
                    await Task.Delay(10000, cancellationToken);
                } 
                catch(TaskCanceledException)
                {
                    isWaiting = false;
                }
            }
            Running = true;
        }
    }
}
