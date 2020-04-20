using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine.Model;
using DialogGenerator.Events;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DialogGenerator.DialogEngine
{
    public class DialogModelsManager
    {
        #region - fields -

        private ILogger mLogger;
        private IEventAggregator mEventAggregator;
        private IDialogModelRepository mDialogModelRepository;
        private double mDialogModelPopularitySum;
        private DialogContext mContext;
        private Random mRandom;

        #endregion

        #region - constructor -

        public DialogModelsManager(ILogger logger,IEventAggregator _eventAggregator
            ,IDialogModelRepository _dialogModelRepository
            ,DialogContext context, Random _Random)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mDialogModelRepository = _dialogModelRepository;
            mContext = context;
            mRandom = _Random;

            mEventAggregator.GetEvent<InitializeDialogModelEvent>().Subscribe(_onInitializeDialogModel);
        }

        #endregion

        #region - private functions -

        private void _onInitializeDialogModel()
        {
            Initialize();
        }

        private bool _checkIfDialogPreRequirementMet(int _dialogModel)
        {
            if (mContext.DialogModelsList[_dialogModel].Requires == null || mContext.DialogModelsList[_dialogModel].Requires.Count == 0)
                return true;

            if (!mContext.HistoricalDialogs.Any())
                return false;

            var _lastHistoricalDialog = mContext.HistoricalDialogs.Last();

            foreach (var _requiredTag in mContext.DialogModelsList[_dialogModel].Requires)
            {
                var _currentRequiredTagSatisfied = false;

                foreach (var _histDialog in mContext.HistoricalDialogs)
                {
                    // could speed by only going through unique historical dialog index #s
                    if (mContext.DialogModelsList[_histDialog.DialogIndex].Adventure == mContext.DialogModelsList[_dialogModel].Adventure)
                    {
                        foreach (var _providedTag in mContext.DialogModelsList[_histDialog.DialogIndex].Provides)
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

        private int _findNextAdventureDialogForCharacters(List<int> _mostRecentAdventureDialogIndexes)
        {
            var _ch1First = new bool();
            var _ch2First = new bool();

            //if we have recently done adventures give priority to adventure dialogs check them first
            foreach (var _recentAdventureIdx in _mostRecentAdventureDialogIndexes)
            {
                //given recent adventures
                foreach (var _possibleDialog in mContext.DialogModelsList) //TODO probably a cleaner way to do this with Linq and lamda expressions
                {
                    //look for follow on adventure possibilities
                    var _possibleDialogIdx = mContext.DialogModelsList.IndexOf(_possibleDialog);

                    if (mContext.DialogModelsList[_recentAdventureIdx].Adventure == _possibleDialog.Adventure)
                    {
                        foreach (var _providedStringKey in mContext.DialogModelsList[_recentAdventureIdx].Provides)
                        {
                            if (_possibleDialog.Requires.Contains(_providedStringKey))
                            {
                                //if a the most recent adventure dialog in the adventure provides what we require we won't 
                                //go backwards in adventures
                                _ch1First = _checkIfCharactersHavePhrasesForDialog(_possibleDialogIdx, mContext.Character1Num, mContext.Character2Num);

                                _ch2First = _checkIfCharactersHavePhrasesForDialog(_possibleDialogIdx, mContext.Character2Num, mContext.Character1Num);

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
            var _tempCh1 = mContext.Character1Num;
            mContext.Character1Num = mContext.Character2Num;
            mContext.Character2Num = _tempCh1;
            // it doesn't appear we should update prior characters 1 and 2 here
        }

        private bool _checkIfDialogModelUsedRecently(int _dialogModel)
        {
            foreach (var _recentDialogQueueEntry in mContext.RecentDialogs) // try again if dialog model recentlyused{
            {
                if (_recentDialogQueueEntry == _dialogModel)
                    return true;
            }

            return false;
        }

        private bool _checkIfCharactersHavePhrasesForDialog(int _dialogModel, int _character1Num, int _character2Num)
        {
            var _currentCharacter = _character1Num;

            foreach (var _element in mContext.DialogModelsList[_dialogModel].PhraseTypeSequence)
            {
                //try again if characters lack phrases for this model
                if (mContext.CharactersList[_currentCharacter].PhraseTotals.PhraseWeights.ContainsKey(_element))
                {
                    if (mContext.CharactersList[_currentCharacter].PhraseTotals.PhraseWeights[_element] < 0.015f)
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

        private List<int> _findMostRecentAdventureDialogIndexes()
        {
            var _mostRecentAdventureDialogs = new List<int>();
            // most recent will be in the 0 index of list
            var _foundAdventures = new List<string>();
            var j = 0;

            for (var i = mContext.HistoricalDialogs.Count - 1; i >= 0; i--)
            {
                var _dialog = mContext.DialogModelsList[mContext.HistoricalDialogs[i].DialogIndex];

                if (_dialog.Adventure.Length > 0 && !_foundAdventures.Contains(_dialog.Adventure))
                {
                    //if the dialog was part of an adventure and we haven't already found the most recent 
                    //from that adventure add the dialog to the most recent adventure list
                    _foundAdventures.Add(_dialog.Adventure);
                    _mostRecentAdventureDialogs.Add(mContext.HistoricalDialogs[i].DialogIndex);
                }

                j++;

                if (j > 400) break; //don't go through all of time looking for active adventures
            }

            return _mostRecentAdventureDialogs;
        }

        #endregion

        #region - public functions -

        public void Initialize()
        {
            if (mContext.DialogModelsList.Count > 0)
                mContext.DialogModelsList.Clear();

            var _dialogModelInfoList = mDialogModelRepository.GetAllByState(ModelDialogState.Available);

            foreach (ModelDialogInfo _modelDialogInfo in _dialogModelInfoList)
            {
                mDialogModelPopularitySum += _modelDialogInfo.ArrayOfDialogModels.Sum(_modelDialogItem => _modelDialogItem.Popularity);

                foreach (ModelDialog _dialogModel in _modelDialogInfo.ArrayOfDialogModels)
                {
                    mContext.DialogModelsList.Add(_dialogModel);
                }
            }
        }

        public int PickAWeightedDialog()
        {
            var _dialogModel = 0;
            var _dialogWeightIndex = 0.0;
            var _attempts = 0;
            var _dialogModelFits = false;
            var _mostRecentAdventureDialogIndexes = _findMostRecentAdventureDialogIndexes();

            // most recent will be in the 0 index of list which will be hit first in foreach
            if (_mostRecentAdventureDialogIndexes.Count > 0)
            {
                var _nextAdventureDialogIdx = _findNextAdventureDialogForCharacters(_mostRecentAdventureDialogIndexes);
                if (_nextAdventureDialogIdx > 0 && _nextAdventureDialogIdx < mContext.DialogModelsList.Count)
                    return _nextAdventureDialogIdx; // we have an adventure dialog for these characters go with it
            }

            int _max_attempts = 30000;
            while (!_dialogModelFits && _attempts < _max_attempts)
            {
                _attempts++;
                _dialogWeightIndex = mRandom.NextDouble();
                _dialogWeightIndex *= mDialogModelPopularitySum;
                double _currentDialogWeightSum = 0;

                foreach (var _dialog in mContext.DialogModelsList)
                {
                    _currentDialogWeightSum += _dialog.Popularity;

                    if (_currentDialogWeightSum > _dialogWeightIndex)
                    {
                        _dialogModel = mContext.DialogModelsList.IndexOf(_dialog);
                        break;
                    }
                }

                var _dialogModelUsedRecently = _checkIfDialogModelUsedRecently(_dialogModel);
                var _charactersHavePhrases = _checkIfCharactersHavePhrasesForDialog(_dialogModel,
                    mContext.Character1Num, mContext.Character2Num);
                var _dialogPreRequirementsMet = _checkIfDialogPreRequirementMet(_dialogModel);
                // don't want a greeting with same characters as last
                var _inappropriateGreeting = mContext.DialogModelsList[_dialogModel].PhraseTypeSequence[0].Equals("Greeting")
                                             && mContext.SameCharactersAsLast;

                if (_dialogPreRequirementsMet && _charactersHavePhrases && !_inappropriateGreeting &&
                    !_dialogModelUsedRecently)
                {
                    _dialogModelFits = true;
                }
                if (_attempts == 500)
                {
                    var attemptsWarningMsg = "Characters " + mContext.CharactersList[mContext.Character1Num].CharacterPrefix +
                        " and " + mContext.CharactersList[mContext.Character2Num].CharacterPrefix +
                        " took over 500 attempts to find a workable dialog model.";
                    Debug.WriteLine(attemptsWarningMsg);
                    // TODO uncomment 
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

        public PhraseEntry PickAWeightedPhrase(int _speakingCharacter, string _currentPhraseType)
        {
            PhraseEntry _selectedPhrase = null;

            try
            {
                _selectedPhrase = mContext.CharactersList[_speakingCharacter].Phrases[0]; //initialize to unused phrase
                //Randomly select a phrase of correct Type
                var _phraseIsDuplicate = true;

                for (var k = 0; k < 6 && _phraseIsDuplicate; k++) //do retries if selected phrase is recently used
                {
                    _phraseIsDuplicate = false;
                    var _phraseTableWeightedIndex = mRandom.NextDouble(); // rand 0.0 - 1.0
                    _phraseTableWeightedIndex *= mContext.CharactersList[_speakingCharacter].PhraseTotals.PhraseWeights[_currentPhraseType];
                    double _amountOfCurrentPhraseType = 0;

                    foreach (var _currentPhraseTableEntry in mContext.CharactersList[_speakingCharacter].Phrases)
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

                    foreach (var _recentPhraseQueueEntry in mContext.CharactersList[_speakingCharacter].RecentPhrases)
                    {
                        if (_recentPhraseQueueEntry.Equals(_selectedPhrase))
                        {
                            _phraseIsDuplicate = true; //send through retry loop k again
                            break; // doesn't matter if duplicated more than once
                        }
                    }
                }

                //eventually overload enque to remove first to keep size same or create a replace
                mContext.CharactersList[_speakingCharacter].RecentPhrases.Dequeue();
                mContext.CharactersList[_speakingCharacter].RecentPhrases.Enqueue(_selectedPhrase);
            }
            catch (Exception ex)
            {
                mLogger.Error("PickAWeightedPhrase " + ex.Message);
            }

            return _selectedPhrase;
        }

        #endregion
    }
}
