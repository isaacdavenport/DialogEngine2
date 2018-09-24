using DialogGenerator.CharacterSelection;
using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine.Model;
using DialogGenerator.DialogEngine.Workflow;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.Model.Logger;
using DialogGenerator.Utilities;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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
        private IEventAggregator mEventAggregator;
        private IMP3Player mPlayer;
        private ICharacterSelection mCharacterSelection;
        private ICharacterRepository mCharacterRepository;
        private IDialogModelRepository mDialogModelRepository;
        private int mPriorCharacter1Num = 100;
        private int mPriorCharacter2Num = 100;
        private int mCharacter1Num = 0;
        private int mCharacter2Num = 1;
        private int mIndexOfCurrentDialogModel;
        private double mDialogModelPopularitySum;
        private bool mSameCharactersAsLast;
        private DialogEngineWorkflow mWorkflow;
        private States mCurrentState;
        private SelectedCharactersPairEventArgs mRandomSelectionDataCached;
        private CancellationTokenSource mCancellationTokenSource;
        private CancellationTokenSource mStateMachineTaskTokenSource = new CancellationTokenSource();
        private static EventWaitHandle mEventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private Random mRandom = new Random();
        private Queue<int> mRecentDialogs = new Queue<int>();
        private List<ModelDialog> mDialogModelsList = new List<ModelDialog>();
        private ObservableCollection<Character> mCharactersList = new ObservableCollection<Character>();
        private List<HistoricalDialog> mHistoricalDialogs = new List<HistoricalDialog>();
        private List<HistoricalPhrase> mHistoricalPhrases = new List<HistoricalPhrase>();

        #endregion

        #region - constructor -

        public DialogEngine(ILogger logger,IEventAggregator _eventAggregator, IMP3Player player,ICharacterSelection _characterSelection,
            ICharacterRepository _characterRepository,IDialogModelRepository _dialogModelRepository)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mPlayer = player;
            mCharacterSelection = _characterSelection;
            mCharacterRepository = _characterRepository;
            mDialogModelRepository = _dialogModelRepository;

            mWorkflow = new DialogEngineWorkflow(() => { });

            _configureWorkflow();
            _subscribeForEvents();
        }

        private void _subscribeForEvents()
        {
            //EventAggregator.Instance.GetEvent<DialogModelChangedEvent>().Subscribe(_onDialogModelChanged);
            mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Subscribe(_onSelectedCharactersPairChanged);
            //EventAggregator.Instance.GetEvent<ChangedCharactersStateEvent>().Subscribe(_onChangedCharacterState);

            mCharactersList.CollectionChanged += _charactersList_CollectionChanged;
            mWorkflow.PropertyChanged += _mWorkflow_PropertyChanged;
            Session.SessionPropertyChanged += _sessionPropertyChanged;
        }

        private void _sessionPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case Constants.SELECTED_DLG_MODEL:
                    {
                        break;
                    }
                case Constants.FORCED_CH_COUNT:
                    {
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
            mLogger.Debug("_onSelectedCharactersPairChanged");
            if (ApplicationData.Instance.UseSerialPort)
            {
                mCharacter1Num = obj.Character1Index;
                mCharacter2Num = obj.Character2Index;

                if (mCurrentState != States.PreparingDialogParameters)
                {
                    mStateMachineTaskTokenSource.Cancel();
                    mWorkflow.Fire(Triggers.PrepareDialogParameters);
                }
            }
            else
            {
                mRandomSelectionDataCached = obj;
            }
        }

        private void _configureWorkflow()
        {
            mWorkflow.Configure(States.Start)
                .Permit(Triggers.PrepareDialogParameters, States.PreparingDialogParameters);

            mWorkflow.Configure(States.PreparingDialogParameters)
                .PermitReentry(Triggers.PrepareDialogParameters)
                .Permit(Triggers.StartDialog, States.DialogStarted)
                .Permit(Triggers.FinishDialog, States.DialogFinished);

            mWorkflow.Configure(States.DialogStarted)
                .Permit(Triggers.PrepareDialogParameters, States.PreparingDialogParameters)
                .Permit(Triggers.FinishDialog, States.DialogFinished);

            mWorkflow.Configure(States.DialogFinished)
                .Permit(Triggers.PrepareDialogParameters, States.PreparingDialogParameters);
        }

        #endregion

        #region - event handlers -

        private void _charactersList_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                Character _newCharacter = e.NewItems[0] as Character;

                _initializeCharacter(_newCharacter);
            }
            else if (e.OldItems != null)
            {
                //TODO
            }
        }

        #endregion

        #region - private functions -

        private void _initializeCharacter(Character character)
        {
            if (string.IsNullOrEmpty(character.CharacterName)) // if character is not loaded correctly we will skip character
                return;

            character.PhraseTotals = new PhraseEntry();  // init PhraseTotals
            character.PhraseTotals.DialogStr = "phrase weights";
            character.PhraseTotals.FileName = "silence";
            character.PhraseTotals.PhraseRating = "G";
            character.PhraseTotals.PhraseWeights = new Dictionary<string, double>();
            character.PhraseTotals.PhraseWeights.Add("Greeting", 0.0f);

            _removePhrasesOverParentalRating(character);

            //Calculate Phrase Weight Totals here.
            foreach (var _curPhrase in character.Phrases)
            {
                foreach (var tag in _curPhrase.PhraseWeights.Keys)
                {
                    if (character.PhraseTotals.PhraseWeights.Keys.Contains(tag))
                    {
                        character.PhraseTotals.PhraseWeights[tag] += _curPhrase.PhraseWeights[tag];
                    }
                    else
                    {
                        character.PhraseTotals.PhraseWeights.Add(tag, _curPhrase.PhraseWeights[tag]);
                    }
                }
            }

            for (var i = 0; i < Character.RecentPhrasesQueueSize && i < character.Phrases.Count; i++)
            {
                // we always deque after enque so this sets que size
                character.RecentPhrases.Enqueue(character.Phrases[0]);
            }
        }

        private void _removePhrasesOverParentalRating(Character _inCharacter)
        {
            var _maxParentalRating = ParentalRatings.GetNumeric(ApplicationData.Instance.CurrentParentalRating);
            var _minParentalRating = ParentalRatings.GetNumeric("G");

            _inCharacter.Phrases.RemoveAll(_item =>
                                              ParentalRatings.GetNumeric(_item.PhraseRating) > _maxParentalRating
                                           || ParentalRatings.GetNumeric(_item.PhraseRating) < _minParentalRating);
        }


        private void _playAudio(string _pathAndFileName)
        {
            try
            {
                if (File.Exists(_pathAndFileName))
                {
                    var i = 0;
                    bool _isPlaying = false;
                    Thread.Sleep(300);

                    var _playSuccess = mPlayer.Play(_pathAndFileName);

                    if (_playSuccess != 0)
                    {
                        AddItem(new ErrorMessage("MP3 Play Error  ---  " + _playSuccess));
                    }

                    do
                    {
                        Thread.Sleep(800);

                        _isPlaying = mPlayer.IsPlaying();

                        Thread.Sleep(100);
                        i++;
                    }
                    while (_isPlaying && i < 400);  // don't get stuck,, 40 seconds max phrase

                    Thread.Sleep(Session.Get<int>(Constants.DIALOG_SPEED)); // wait around a second after the audio is done for between phrase pause
                }
                else
                {
                    AddItem(new ErrorMessage("Could not find: " + _pathAndFileName));
                    Thread.Sleep(Session.Get<int>(Constants.DIALOG_SPEED));
                }
            }
            catch (Exception ex)
            {
                mLogger.Error(" _playAudio " + ex.Message);
            }
        }


        private void _addPhraseToHistory(PhraseEntry _selectedPhrase, int _speakingCharacter)
        {
            mHistoricalPhrases.Add(new HistoricalPhrase
            {
                CharacterIndex = _speakingCharacter,
                CharacterPrefix = mCharactersList[_speakingCharacter].CharacterPrefix,
                PhraseIndex = mCharactersList[_speakingCharacter].Phrases.IndexOf(_selectedPhrase),
                PhraseFile = _selectedPhrase.FileName,
                StartedTime = DateTime.Now
            });

            mLogger.Info(ApplicationData.Instance.DialogLoggerKey, mCharactersList[_speakingCharacter].CharacterName + ": " + _selectedPhrase.DialogStr);
        }


        private void _addDialogModelToHistory(int _dialogModelIndex, int _ch1, int _ch2)
        {
            mHistoricalDialogs.Add(new HistoricalDialog
            {
                DialogIndex = _dialogModelIndex,
                DialogName = mDialogModelsList[_dialogModelIndex].Name,
                StartedTime = DateTime.Now,
                Completed = false,
                Character1 = _ch1,
                Character2 = _ch2
            });
        }

        private bool _setNextCharacters()
        {

            if (!ApplicationData.Instance.UseSerialPort)  // check is selection in random mode
            {
                SelectedCharactersPairEventArgs args = mRandomSelectionDataCached;
                if (args == null || args.Character1Index < 0 || args.Character1Index >= mCharactersList.Count
                    || args.Character2Index < 0 || args.Character2Index >= mCharactersList.Count ||
                    args.Character1Index == args.Character2Index)
                {
                    return false;
                }
                mCharacter1Num = args.Character1Index;
                mCharacter2Num = args.Character2Index;
            }

            var _tempChar1 = mCharacter1Num;
            var _tempChar2 = mCharacter2Num;

            if (_tempChar1 == _tempChar2 || _tempChar1 >= mCharactersList.Count || _tempChar2 >= mCharactersList.Count)
                return false;

            mSameCharactersAsLast = (_tempChar1 == mPriorCharacter1Num || _tempChar1 == mPriorCharacter2Num)
                                  && (_tempChar2 == mPriorCharacter1Num || _tempChar2 == mPriorCharacter2Num);

            mCharacter1Num = _tempChar1;
            mCharacter2Num = _tempChar2;
            mPriorCharacter1Num = mCharacter1Num;
            mPriorCharacter2Num = mCharacter2Num;

            return true;
        }


        private int _pickAWeightedDialog()
        {
            //TODO check that all characters/phrasetypes required for adventure are included before starting adventure?
            var _dialogModel = 0;
            var _dialogWeightIndex = 0.0;
            var _attempts = 0;
            var _dialogModelFits = false;
            var _mostRecentAdventureDialogIndexes = _findMostRecentAdventureDialogIndexes();

            // most recent will be in the 0 index of list which will be hit first in foreach
            if (_mostRecentAdventureDialogIndexes.Count > 0)
            {
                var _nextAdventureDialogIdx = _findNextAdventureDialogForCharacters(_mostRecentAdventureDialogIndexes);
                if (_nextAdventureDialogIdx > 0 && _nextAdventureDialogIdx < mDialogModelsList.Count)
                    return _nextAdventureDialogIdx; // we have an adventure dialog for these characters go with it
            }

            int _max_attempts = 30000;
            while (!_dialogModelFits && _attempts < _max_attempts)
            {
                _attempts++;
                _dialogWeightIndex = mRandom.NextDouble();
                _dialogWeightIndex *= mDialogModelPopularitySum;
                double _currentDialogWeightSum = 0;

                foreach (var _dialog in mDialogModelsList)
                {
                    _currentDialogWeightSum += _dialog.Popularity;

                    if (_currentDialogWeightSum > _dialogWeightIndex)
                    {
                        _dialogModel = mDialogModelsList.IndexOf(_dialog);
                        break;
                    }
                }

                var _dialogModelUsedRecently = _checkIfDialogModelUsedRecently(_dialogModel);
                var _charactersHavePhrases = _checkIfCharactersHavePhrasesForDialog(_dialogModel,
                    mCharacter1Num, mCharacter2Num);
                var _dialogPreRequirementsMet = _checkIfDialogPreRequirementMet(_dialogModel);
                // don't want a greeting with same characters as last
                var _inappropriateGreeting = mDialogModelsList[_dialogModel].PhraseTypeSequence[0].Equals("Greeting")
                                             && mSameCharactersAsLast;

                if (_dialogPreRequirementsMet && _charactersHavePhrases && !_inappropriateGreeting &&
                    !_dialogModelUsedRecently)
                {
                    _dialogModelFits = true;
                }
                if (_attempts == 100)
                {
                    var attemptsWarningMsg = "Characters " + mCharactersList[mCharacter1Num].CharacterPrefix +
                        " and " + mCharactersList[mCharacter2Num].CharacterPrefix +
                        " took over 100 attempts to find a workable dialog model.";
                    Debug.WriteLine(attemptsWarningMsg);
                    // DODO uncomment 
                    //DialogDataHelper.AddMessage(new WarningMessage(attemptsWarningMsg));
                    mLogger.Debug(attemptsWarningMsg);
                }
                if (_dialogPreRequirementsMet && _charactersHavePhrases && _attempts > 15000)
                {
                    _dialogModelFits = true;
                }
            }
            return _dialogModel;
        }


        private List<int> _findMostRecentAdventureDialogIndexes()
        {
            var _mostRecentAdventureDialogs = new List<int>();
            // most recent will be in the 0 index of list
            var _foundAdventures = new List<string>();
            var j = 0;

            for (var i = mHistoricalDialogs.Count - 1; i >= 0; i--)
            {
                var _dialog = mDialogModelsList[mHistoricalDialogs[i].DialogIndex];

                if (_dialog.Adventure.Length > 0 && !_foundAdventures.Contains(_dialog.Adventure))
                {
                    //if the dialog was part of an adventure and we haven't already found the most recent 
                    //from that adventure add the dialog to the most recent adventure list
                    _foundAdventures.Add(_dialog.Adventure);
                    _mostRecentAdventureDialogs.Add(mHistoricalDialogs[i].DialogIndex);
                }

                j++;

                if (j > 400) break; //don't go through all of time looking for active adventures
            }

            return _mostRecentAdventureDialogs;
        }


        private int _findNextAdventureDialogForCharacters(List<int> _mostRecentAdventureDialogIndexes)
        {
            var _ch1First = new bool();
            var _ch2First = new bool();

            //if we have recently done adventures give priority to adventure dialogs check them first
            foreach (var _recentAdventureIdx in _mostRecentAdventureDialogIndexes)
            {
                //given recent adventures
                foreach (var _possibleDialog in mDialogModelsList) //TODO probably a cleaner way to do this with Linq and lamda expressions
                {
                    //look for follow on adventure possibilities
                    var _possibleDialogIdx = mDialogModelsList.IndexOf(_possibleDialog);

                    if (mDialogModelsList[_recentAdventureIdx].Adventure == _possibleDialog.Adventure)
                    {
                        foreach (var _providedStringKey in mDialogModelsList[_recentAdventureIdx].Provides)
                        {
                            if (_possibleDialog.Requires.Contains(_providedStringKey))
                            {
                                //if a the most recent adventure dialog in the adventure provides what we require we won't 
                                //go backwards in adventures
                                _ch1First = _checkIfCharactersHavePhrasesForDialog(_possibleDialogIdx, mCharacter1Num, mCharacter2Num);

                                _ch2First = _checkIfCharactersHavePhrasesForDialog(_possibleDialogIdx, mCharacter2Num, mCharacter1Num);

                                if (_ch1First || _ch2First)
                                {
                                    if (_ch2First)
                                        _swapCharactersOneAndTwo();


                                    return _possibleDialogIdx;
                                }
                            }
                        }

                    }
                }
            }

            return -1; // code for no next adventure continuance found
        }


        public void _swapCharactersOneAndTwo()
        {
            var _tempCh1 = mCharacter1Num;
            mCharacter1Num = mCharacter2Num;
            mCharacter2Num = _tempCh1;
            // it doesn't appear we should update prior characters 1 and 2 here
        }


        private bool _checkIfDialogModelUsedRecently(int _dialogModel)
        {
            foreach (var _recentDialogQueueEntry in mRecentDialogs) // try again if dialog model recentlyused{
            {
                if (_recentDialogQueueEntry == _dialogModel)
                    return true;
            }

            return false;
        }


        private bool _checkIfCharactersHavePhrasesForDialog(int _dialogModel, int _character1Num, int _character2Num)
        {
            var _currentCharacter = _character1Num;

            foreach (var _element in mDialogModelsList[_dialogModel].PhraseTypeSequence)
            {
                //try again if characters lack phrases for this model
                if (mCharactersList[_currentCharacter].PhraseTotals.PhraseWeights.ContainsKey(_element))
                {
                    if (mCharactersList[_currentCharacter].PhraseTotals.PhraseWeights[_element] < 0.015f)
                        return false;

                    if (_currentCharacter == _character1Num)
                        _currentCharacter = _character2Num;
                    else
                        _currentCharacter = _character1Num;

                }
                else
                {
                    return false;
                }
            }

            return true;
        }


        private bool _checkIfDialogPreRequirementMet(int _dialogModel)
        {
            if (mDialogModelsList[_dialogModel].Requires == null || mDialogModelsList[_dialogModel].Requires.Count == 0)
                return true;

            if (!mHistoricalDialogs.Any())
                return false;

            var _lastHistoricalDialog = mHistoricalDialogs.Last();

            foreach (var _requiredTag in mDialogModelsList[_dialogModel].Requires)
            {
                var _currentRequiredTagSatisfied = false;

                foreach (var _histDialog in mHistoricalDialogs)
                {
                    // could speed by only going through unique historical dialog index #s
                    if (mDialogModelsList[_histDialog.DialogIndex].Adventure == mDialogModelsList[_dialogModel].Adventure)
                    {
                        foreach (var _providedTag in mDialogModelsList[_histDialog.DialogIndex].Provides)
                        {
                            if (_providedTag == _requiredTag)
                            {
                                _currentRequiredTagSatisfied = true;
                                break;
                            }
                        }
                    }

                    if (_currentRequiredTagSatisfied)
                        break;

                    if (_histDialog == _lastHistoricalDialog)
                        return false;
                }
            }
            return true;
        }


        private PhraseEntry _pickAWeightedPhrase(int _speakingCharacter, string _currentPhraseType)
        {
            PhraseEntry _selectedPhrase = null;

            try
            {
                _selectedPhrase = mCharactersList[_speakingCharacter].Phrases[0]; //initialize to unused phrase
                //Randomly select a phrase of correct Type
                var _phraseIsDuplicate = true;

                for (var k = 0; k < 6 && _phraseIsDuplicate; k++) //do retries if selected phrase is recently used
                {
                    _phraseIsDuplicate = false;
                    var _phraseTableWeightedIndex = mRandom.NextDouble(); // rand 0.0 - 1.0
                    _phraseTableWeightedIndex *= mCharactersList[_speakingCharacter].PhraseTotals.PhraseWeights[_currentPhraseType];
                    double _amountOfCurrentPhraseType = 0;

                    foreach (var _currentPhraseTableEntry in mCharactersList[_speakingCharacter].Phrases)
                    {
                        if (_currentPhraseTableEntry.PhraseWeights.ContainsKey(_currentPhraseType))
                        {
                            _amountOfCurrentPhraseType += _currentPhraseTableEntry.PhraseWeights[_currentPhraseType];
                        }

                        if (_amountOfCurrentPhraseType > _phraseTableWeightedIndex)
                        {
                            _selectedPhrase = _currentPhraseTableEntry;
                            break; //inner foreach since we have the phrase we want
                        }
                    }

                    foreach (var _recentPhraseQueueEntry in mCharactersList[_speakingCharacter].RecentPhrases)
                    {
                        if (_recentPhraseQueueEntry.Equals(_selectedPhrase))
                        {
                            _phraseIsDuplicate = true; //send through retry loop k again
                            break; // doesn't matter if duplicated more than once
                        }
                    }
                }

                //eventually overload enque to remove first to keep size same or create a replace
                mCharactersList[_speakingCharacter].RecentPhrases.Dequeue();
                mCharactersList[_speakingCharacter].RecentPhrases.Enqueue(_selectedPhrase);
            }
            catch (Exception ex)
            {
                mLogger.Error("PickAWeightedPhrase " + ex.Message);
            }

            return _selectedPhrase;
        }


        private bool _dialogTrackerAndSerialComsCharactersSame()
        {
            if ((mCharacter1Num == Session.Get<int>(Constants.NEXT_CH_1)
                 || mCharacter1Num == Session.Get<int>(Constants.NEXT_CH_2))
                 && (mCharacter2Num == Session.Get<int>(Constants.NEXT_CH_2)
                 || mCharacter2Num == Session.Get<int>(Constants.NEXT_CH_1)))
            {
                return true;
            }
            return false;
        }


        private Triggers _prepareDialogParameters(CancellationToken token)
        {
            try
            {
                token.ThrowIfCancellationRequested();

                if (!_setNextCharacters())
                    return Triggers.PrepareDialogParameters;

                token.ThrowIfCancellationRequested();

                mIndexOfCurrentDialogModel = Session.Get<int>(Constants.SELECTED_DLG_MODEL) >= 0?
                                             mIndexOfCurrentDialogModel
                                            : _pickAWeightedDialog();

                token.ThrowIfCancellationRequested();
                _addDialogModelToHistory(mIndexOfCurrentDialogModel, mCharacter1Num, mCharacter2Num);

                //TODO uncomment
                //WriteDialogInfo();

                return Triggers.StartDialog;
            }
            catch (OperationCanceledException) { }
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
                var _speakingCharacter = mCharacter1Num;
                var _selectedPhrase = mCharactersList[_speakingCharacter].Phrases[0]; //initialize to unused placeholder phrase

                mLogger.Debug("_startDialog " + mCharactersList[mCharacter1Num].CharacterPrefix + " and " +
                    mCharactersList[mCharacter2Num].CharacterPrefix + " " + mDialogModelsList[mIndexOfCurrentDialogModel].Name);

                mEventAggregator.GetEvent<ActiveCharactersEvent>().
                    Publish($" {mCharactersList[mCharacter1Num].CharacterName} , {mCharactersList[mCharacter2Num].CharacterName} ");

                foreach (var _currentPhraseType in mDialogModelsList[mIndexOfCurrentDialogModel].PhraseTypeSequence)
                {
                    token.ThrowIfCancellationRequested();

                    if (mCharactersList[_speakingCharacter].PhraseTotals.PhraseWeights.ContainsKey(_currentPhraseType))
                    {
                        AddItem(new InfoMessage(mCharactersList[_speakingCharacter].CharacterName + ": "));

                        if (mCharactersList[_speakingCharacter].PhraseTotals.PhraseWeights[_currentPhraseType] < 0.01f)
                        {
                            AddItem(new WarningMessage("Missing PhraseType: " + _currentPhraseType));
                        }

                        token.ThrowIfCancellationRequested();

                        _selectedPhrase = _pickAWeightedPhrase(_speakingCharacter, _currentPhraseType);

                        if (_selectedPhrase == null)
                        {
                            AddItem(new WarningMessage("Phrase type " + _currentPhraseType + " was not found."));
                            continue;
                        }

                        token.ThrowIfCancellationRequested();

                        AddItem(new InfoMessage(_selectedPhrase.DialogStr));

                        if (ApplicationData.Instance.TextDialogsOn)
                        {
                            mEventAggregator.GetEvent<NewDialogLineEvent>().Publish(new NewDialogLineEventArgs
                            {
                                Character = mCharactersList[_speakingCharacter],
                                DialogLine = _selectedPhrase.DialogStr
                            });
                        }

                        token.ThrowIfCancellationRequested();

                        _addPhraseToHistory(_selectedPhrase, _speakingCharacter);

                        var _pathAndFileName = Path.Combine(ApplicationData.Instance.AudioDirectory,
                                               mCharactersList[_speakingCharacter].CharacterPrefix
                                              + "_" + _selectedPhrase.FileName + ".mp3");
                        Debug.WriteLine(_selectedPhrase.DialogStr + " started");
                        _playAudio(_pathAndFileName); // vb: code stops here so commented out for debugging purpose


                        if (!_dialogTrackerAndSerialComsCharactersSame() && ApplicationData.Instance.UseSerialPort)
                        //&& DialogViewModel.NumberOfCharactersSetToOn == 0)
                        {
                            mSameCharactersAsLast = false;
                            return Triggers.PrepareDialogParameters; // the characters have moved  TODO break into charactersSame() and use also with prior
                        }
                        //Toggle character
                        if (_speakingCharacter == mCharacter1Num) //toggle which character is speaking next
                            _speakingCharacter = mCharacter2Num;
                        else
                            _speakingCharacter = mCharacter1Num;
                    }

                    mHistoricalDialogs[mHistoricalDialogs.Count - 1].Completed = true;

                    if (mHistoricalDialogs.Count > 2000)
                        mHistoricalDialogs.RemoveRange(0, 100);

                    if (mHistoricalPhrases.Count > 8000)
                        mHistoricalPhrases.RemoveRange(0, 100);

                    mRecentDialogs.Dequeue(); //move to use HistoricalDialogs
                    mRecentDialogs.Enqueue(mIndexOfCurrentDialogModel);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                mLogger.Error("_startDialog " + ex.Message);
            }

            return Triggers.PrepareDialogParameters;
        }

        #endregion

        #region - public functions -

        public void Initialize()
        {
            var _dialogModelInfoList = mDialogModelRepository.GetAll();
            mCharactersList = mCharacterRepository.GetAll();
            mCharactersList.CollectionChanged += _charactersList_CollectionChanged;

            foreach (ModelDialogInfo _modelDialogInfo in _dialogModelInfoList)
            {
                mDialogModelPopularitySum += _modelDialogInfo.ArrayOfDialogModels.Sum(_modelDialogItem => _modelDialogItem.Popularity);

                foreach (ModelDialog _dialogModel in _modelDialogInfo.ArrayOfDialogModels)
                {
                    mDialogModelsList.Add(_dialogModel);
                }
            }

            foreach (Character character in mCharactersList)
            {
                _initializeCharacter(character);
            }

            // Fill the queue with greeting dialogs
            for (var _i = 0; _i < DialogEngineConstants.RecentDialogsQueSize; _i++)
            {
                mRecentDialogs.Enqueue(0); // Fill the que with greeting dialogs
            }
        }

        public Task StartDialogEngine()
        {
            mCharacterSelection.StartCharacterSelection();

            mCancellationTokenSource = new CancellationTokenSource();

            if (mCurrentState != States.PreparingDialogParameters)
                mWorkflow.Fire(Triggers.PrepareDialogParameters);

            return Task.Run(() => {

                Thread.CurrentThread.Name = "DialogGeneratorThread";

                do
                {
                    mStateMachineTaskTokenSource = new CancellationTokenSource();

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
                                if (mWorkflow.CanFire(_nextTrigger))
                                    mWorkflow.Fire(_nextTrigger);
                                break;
                            }
                    }
                }
                while (!mCancellationTokenSource.Token.IsCancellationRequested);
            });
        }

        public void StopDialogEngine()
        {
            //stop .mp3 player if playing
            mEventAggregator.GetEvent<StopImmediatelyPlayingCurrentDialogLIne>().Publish();

            // stop character selection
            mCharacterSelection.StopCharacterSelection();

            mStateMachineTaskTokenSource.Cancel();
            mCancellationTokenSource.Cancel();

            if (mCurrentState != States.DialogFinished)
                mWorkflow.Fire(Triggers.FinishDialog);
        }



        public void AddItem(LogMessage _logMessage)
        {

        }

        #endregion
    }
}
