using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine.Model;
using DialogGenerator.Events;
using DialogGenerator.Events.EventArgs;
using DialogGenerator.Model;
using DialogGenerator.Model.Enum;
using Prism.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using DialogGenerator.Utilities;

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
        private IMessageDialogService mMessageDialogService;

        #endregion

        #region - constructor -

        public DialogModelsManager(ILogger logger,IEventAggregator _eventAggregator
            ,IDialogModelRepository _dialogModelRepository
            ,DialogContext context, Random _Random, IMessageDialogService _messageDialogService)
        {
            mLogger = logger;
            mEventAggregator = _eventAggregator;
            mDialogModelRepository = _dialogModelRepository;
            mContext = context;
            mRandom = _Random;
            mMessageDialogService = _messageDialogService;

            mEventAggregator.GetEvent<InitializeDialogModelEvent>().Subscribe(_onInitializeDialogModel);
            mEventAggregator.GetEvent<SelectedCharactersPairChangedEvent>().Subscribe(_onSelectedCharactersPairChanged);
        }

        #endregion

        #region - private functions -

        private void _onInitializeDialogModel()
        {
            Initialize();
        }

        private void _onSelectedCharactersPairChanged(SelectedCharactersPairEventArgs obj)
        {            
            if (obj != null && mContext.DialogModelsList.Any())
            {
                if(obj.Character1Index >= 0 && obj.Character2Index >= 0 && obj.Character1Index 
                    <= mContext.CharactersList.Count && obj.Character2Index <= mContext.CharactersList.Count)
                {
                    mContext.PossibleDialogModelsList = _preparePossibleDialogModelsList(obj.Character1Index, obj.Character2Index);
                    if(mContext.PossibleDialogModelsList.Count > 0 && mContext.NoDialogs)
                    {
                        mContext.NoDialogs = false;
                    }

                    // S.Ristic 2021-03-30 - DLGEN-588
                    // Only if we really have a different pair of characters.
                    if((obj.Character1Index != mContext.Character2Num && obj.Character2Index != mContext.Character1Num)
                        && (obj.Character1Index != mContext.Character1Num || obj.Character2Index != mContext.Character2Num))
                    {
                        mContext.CharactersList[obj.Character1Index].ClearRecentPhrases();
                        mContext.CharactersList[obj.Character2Index].ClearRecentPhrases();
                        mContext.HistoricalDialogs.Clear();
                        mContext.HistoricalPhrases.Clear();

                        mContext.FirstRoundGone = false;
                    }
                    // S.Ristic 2021-03-30 - End of change
                    
                } 
               
            }
        }

        private List<ModelDialog> _preparePossibleDialogModelsList(int character1Index, int character2Index)
        {
            mLogger.Info("_preparePossibleDialogModelsList " + mContext.CharactersList[character1Index].CharacterName
                + " " + mContext.CharactersList[character2Index].CharacterName);

            var _possibleList = mContext.DialogModelsList
                .Where(dlg => dlg.PhraseTypeSequence
                    .Select((entry, i) => new { i, entry })
                    .Where(a => a.i % 2 == 0)
                    .Select(z => z.entry)
                    .ToList()
                    .All(pts => mContext.CharactersList[character1Index].Phrases
                        .Any(phrase => phrase.PhraseWeights.Keys.Contains(pts)))).ToList();

            _possibleList = _possibleList
                .Where(dlg => dlg.PhraseTypeSequence
                    .Select((entry, i) => new { i, entry })
                    .Where(a => a.i % 2 != 0)
                    .Select(z => z.entry)
                    .ToList()
                    .All(pts => mContext.CharactersList[character2Index].Phrases
                        .Any(phrase => phrase.PhraseWeights.Keys.Contains(pts)))).ToList();

            return _possibleList;
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
            var _returnIndex = -1;

            var _recentAdventureDialogs = mContext.DialogModelsList.Select((s, i) => new { i, s })
                .Where(p => _mostRecentAdventureDialogIndexes.Contains(p.i))
                .Select(p => p.s)
                .ToList();

            // S.Ristic - Semi linq approach
            // Explanation: Code is nice, but the trade off is that all linq loops have to be finished, even if the value
            //              was found in the first dialog, because the linq loops can't be broken. The way this is implemented
            //              the root loop is not linq loop, so if the first dialog's Adventure property matches the Adventure 
            //              property of some of the dialogs from the recent adventure dialogs collection, we will have to wait
            //              only recent adventure dialogs loop to finish, which won't be long considering the fact that this 
            //              loop can have max 6 dialogs.

            for (int _i = 0; _i < mContext.DialogModelsList.Count; _i ++)
            {
                var _dialog = mContext.DialogModelsList[_i];
                _recentAdventureDialogs.Where(r => r.Adventure == _dialog.Adventure).ToList().ForEach(rad =>
                {
                    rad.Provides.ForEach(provides =>
                    {
                        if (_dialog.Requires.Contains(provides))
                        {
                            //if a the most recent adventure dialog in the adventure provides what we require we won't 
                            //go backwards in adventures
                            _ch1First = _checkIfCharactersHavePhrasesForDialog(_i, mContext.Character1Num, mContext.Character2Num);
                            _ch2First = _checkIfCharactersHavePhrasesForDialog(_i, mContext.Character2Num, mContext.Character1Num);

                            if (_ch1First || _ch2First)
                            {
                                if (_ch2First)
                                    _swapCharactersOneAndTwo();

                                if(_returnIndex == -1)
                                {
                                    _returnIndex = _i;
                                }
                            }
                        }
                    });
                });

                if(_returnIndex != -1)
                    break;                

            }
            return _returnIndex; // code for no next adventure continuance found
        }

        private bool _isGreetingDialog(int idx)
        {
            var _dlg = mContext.DialogModelsList[idx];
            if (_dlg.PhraseTypeSequence.Where(p => p.Equals("Greeting")).Count() == 2)
            {
                return true;
            }

            return false;
        }

        private bool _checkIfDialogModelUsedRecently(int _dialogModel)
        {
            if(mContext.HistoricalDialogs.Count > 0)
            {
                int _depth = Math.Min(DialogEngineConstants.RecentDialogsQueSize, mContext.HistoricalDialogs.Count);
                for (int _k = 0; _k < _depth; _k++)
                {
                    if (mContext.HistoricalDialogs[mContext.HistoricalDialogs.Count - 1 - _k].Completed)
                    {
                        int _dialogIndex = mContext.HistoricalDialogs[mContext.HistoricalDialogs.Count - 1 - _k].DialogIndex;
                        if (_dialogIndex == _dialogModel)
                        {
                            return true;
                        }
                    }
                }
            }
            

            return false;
        }

        private bool _checkIfCharactersHavePhrasesForDialog(int _dialogModel, int _character1Num, int _character2Num)
        {
            bool bResult = false;
            bool bFirstCharacter = true;
            bool bSecondCharacter = true;

            var dialog = mContext.DialogModelsList[_dialogModel];

            // Entries with the even indices.
            dialog.PhraseTypeSequence.Select((entry, i) => new { i, entry }).Where(p => p.i % 2 == 0).Select(e => e.entry).ToList().ForEach(
                str =>
                {
                    if (!mContext.CharactersList[_character1Num].Phrases
                        .Any(rp => rp.PhraseWeights.Keys.Contains(str)))
                    {
                        bFirstCharacter = false;
                    }
                });

            // Entries with the odd indices.
            if (bFirstCharacter)
            {
                dialog.PhraseTypeSequence.Select((entry, i) => new { i, entry }).Where(p => p.i % 2 != 0).Select(e => e.entry).ToList().ForEach(
                    str =>
                    {
                        if (!mContext.CharactersList[mContext.Character2Num].Phrases
                            .Any(rp => rp.PhraseWeights.Keys.Contains(str)))
                        {
                            bSecondCharacter = false;
                        }
                    });
            }

            bResult = bFirstCharacter && bSecondCharacter;

            return bResult;

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

        private bool _checkForRecentPhrases(int dialogModel)
        {
            var dialog = mContext.DialogModelsList[dialogModel];
            bool result = false;

            // Entries with the even indices.
            dialog.PhraseTypeSequence.Select((entry, i) => new { i, entry }).Where(p => p.i % 2 == 0).Select(e => e.entry).ToList().ForEach(
                str =>
                {
                    if (!result && mContext.CharactersList[mContext.Character1Num].RecentPhrases
                        .Any(rp => rp.PhraseWeights.Keys.Contains(str)))
                    {
                        result = true;
                    }
                });

            // Entries with the odd indices.
            if (!result)
            {
                dialog.PhraseTypeSequence.Select((entry, i) => new { i, entry }).Where(p => p.i % 2 != 0).Select(e => e.entry).ToList().ForEach(
                    str =>
                    {
                        if (!result && mContext.CharactersList[mContext.Character2Num].RecentPhrases
                            .Any(rp => rp.PhraseWeights.Keys.Contains(str)))
                        {
                            result = true;
                        }
                    });
            }
            
            return result;
        }

        #endregion

        #region - public functions -

        public void _swapCharactersOneAndTwo()
        {
            var _tempCh1 = mContext.Character1Num;
            mContext.Character1Num = mContext.Character2Num;
            mContext.Character2Num = _tempCh1;
            // TODO I suspect we need to _prepareDialogModel parameters  -isaac
            // it doesn't appear we should update prior characters 1 and 2 here
        }

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
            mLogger.Info("DialogModelsList initialized mDialogModelPopularitySum = " + mDialogModelPopularitySum.ToString());
        }

        // PickAWeightedDialog and PickAWeightedPhrase use a statistical approach to randomly select DialogModels and 
        // Phrases.  Each DialogModel has a popularity weighting factor and each phrase has a PhraseWeight number.  
        // The larger these numbers are the more often that DialogModel or Phrase will come up.  Here is a sample calculation
        // for phrases.  Say that Johny has three lines with a value for the PhraseWeight RequestAffirmation.
        //  "Have you seen my mommy?" 0.4
        //  "Mommy! Mommy!"  0.6
        //  "I don't feel so good" 2.0
        // The lines above will also have PhraseWeights for other PhraseWeight tags like Exclamation YesNoQuestion etc. but
        // if the dialog model calls for a RequestAffirmation line we look for all the lines with a RequestAffirmation PhraseWeight
        // tag and add them up 2+0.6+0.4=3.  We get a random number which comes back between 0-1, lets say it came up at .75
        // We mutiply the random number by the sum of the weights  .75*3 = 2.25
        // We now step through the lines (or DialogModels in the other method) to find our random selection by weight/poularity
        //  Have you seen my mommy gets us to 0.4, not bigger than 2.25 yet
        //  Now we addin 0.6 for the "Mommy! Mommy!" line .4+.6 = 1 and not larger than 2.25 yet
        //  Now we add in the 2 weighting for the "I don't feel so good" line and .4+.6+2 = 3 is larger than 2.25 so we have found 
        //  our randomly selected line.  You can see that if the random number were lower we would have selected one of the earlier 
        //  lines.  You can also see that the weight of the last line at 2 is larger than the weight of the first line at 0.4 so
        //  the last line is more likely to come up than both the previous two lines.
        public int PickAWeightedDialog()
        {
            var _dialogModel = 0;
            var _mostRecentAdventureDialogIndexes = _findMostRecentAdventureDialogIndexes();

            var settings = ApplicationData.Instance;

            if(settings.HasPreferredDialog)
            {
                var _preferredDialog = mContext.DialogModelsList.Where(d => d.Name.Equals(settings.PreferredDialogName)).FirstOrDefault();
                if(_preferredDialog != null)
                {
                    if(_isDialogEgligibleForCharacters(_preferredDialog, mContext.Character1Num, mContext.Character2Num))
                    {
                        mLogger.Info("PickAWeightedDialog returning preferredDialog " + _preferredDialog.Name);
                        return mContext.DialogModelsList.IndexOf(_preferredDialog);
                    }
                }
            }

            // most recent will be in the 0 index of list which will be hit first in foreach
            if (_mostRecentAdventureDialogIndexes.Count > 0)
            {
                var _nextAdventureDialogIdx = _findNextAdventureDialogForCharacters(_mostRecentAdventureDialogIndexes);
                if (_nextAdventureDialogIdx > 0 && _nextAdventureDialogIdx < mContext.DialogModelsList.Count)
                {
                    mLogger.Info("Adventure dialog model found " + mContext.DialogModelsList[_nextAdventureDialogIdx].Name);
                    return _nextAdventureDialogIdx; // we have an adventure dialog for these characters go with it
                }
            }

            if (mContext.PossibleDialogModelsList == null || !mContext.PossibleDialogModelsList.Any())
            {
                mLogger.Info("PossibleDialogModelsList empty calling  _preparePossibleDialogModelsList " + 
                    mContext.CharactersList[mContext.Character1Num].CharacterName + " " +
                    mContext.CharactersList[mContext.Character2Num].CharacterName);
                mContext.PossibleDialogModelsList = _preparePossibleDialogModelsList(mContext.Character1Num, mContext.Character2Num);
            }

            if(mContext.PossibleDialogModelsList.Count == 0)
            {
                mLogger.Info($"PICK A WEIGHTED DIALOG - No possible dialogs for characters {mContext.Character1Num} and {mContext.Character2Num}.");
                return -1;
            } else
            {
                if(mContext.NoDialogs)
                {
                    mContext.NoDialogs = false;
                }

                mLogger.Info("PossibleDialogModelList entries count " + mContext.PossibleDialogModelsList.Count.ToString());
                mEventAggregator.GetEvent<CharactersHaveDialogsEvent>().Publish(true);
            }

            var _itemsToRemove = _dialogsToRemove();
            var _filteredList = new List<ModelDialog>();
            if(_itemsToRemove.Count > 0)
            {
                //var _resetHistory = _itemsToRemove.Count == mContext.PossibleDialogModelsList.Count ? true : false;
                if(mContext.PossibleDialogModelsList.Count == _itemsToRemove.Count)
                {
                    mContext.HistoricalDialogs.Clear();
                    mContext.CharactersList[mContext.Character1Num].ClearRecentPhrases();
                    mContext.CharactersList[mContext.Character2Num].ClearRecentPhrases();
                    _itemsToRemove = _dialogsToRemove();
                    if (_itemsToRemove.Count == mContext.PossibleDialogModelsList.Count)
                    {
                        // Leave at least one dialog.
                        _itemsToRemove.RemoveAt(0);
                    }
                    
                }

                _filteredList = mContext.PossibleDialogModelsList.Except(_itemsToRemove).ToList();
                
            } else
            {
                _filteredList = mContext.PossibleDialogModelsList;
            }
            mLogger.Info("Possible dialog models that can be used filtered down to " + _filteredList.Count.ToString());


            var _currentDialogWeightSum = 0.0;
            var _dialogWeightIndex = mRandom.NextDouble();
            _dialogWeightIndex *= _filteredList.Sum(dlg => dlg.Popularity);
            foreach (var _dialog in _filteredList)
            {
                _currentDialogWeightSum += _dialog.Popularity;

                if (_currentDialogWeightSum > _dialogWeightIndex)
                {
                    _dialogModel = mContext.DialogModelsList.IndexOf(_dialog);
                    break;
                }
            }

            return _dialogModel;
        }
                       
        //  See the comment above PickAWeightedDialog() above since PickAWeightedPhrase works the same way
        public PhraseEntry PickAWeightedPhrase(int _speakingCharacter, string _currentPhraseType)
        {
            PhraseEntry _selectedPhrase = null;
            int k = 1000;

            try
            {
                mLogger.Info("PickAWeightedPhrase starting for  " + mContext.CharactersList[_speakingCharacter].CharacterName +
               " " + _currentPhraseType);

                _selectedPhrase = new PhraseEntry
                {
                    DialogStr = " .... ",
                };

                //Randomly select a phrase of correct Type
                var _phraseIsDuplicate = true;
                for (k = 0; k < 25 && _phraseIsDuplicate; k++) //do retries if selected phrase is recently used
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
            mLogger.Info("PickAWeightedPhrase for character " + mContext.CharactersList[_speakingCharacter].CharacterName + 
                " used " + k.ToString() + " retries to select a " + _currentPhraseType + " of:  -" +
                (_selectedPhrase.DialogStr.Length <= 128 ? _selectedPhrase.DialogStr : _selectedPhrase.DialogStr.Substring(0, 126)) + "...");

            return _selectedPhrase;
        }

        #endregion

        #region Private Methods

        private bool _isDialogEgligibleForCharacters(ModelDialog preferredDialog, int character1Num, int character2Num)
        {

            bool character1First = false;
            bool character2Second = false;
            bool character1Second = false;
            bool character2First = false;


            character1First = preferredDialog.PhraseTypeSequence.Select((entry, i) => new { i, entry })
                .Where(a => a.i % 2 == 0)
                .Select(z => z.entry)
                .ToList()
                .All(pts => mContext.CharactersList[character1Num].Phrases.Any(phrase => phrase.PhraseWeights.Keys.Contains(pts)));
            if (character1First)
            {
                character2Second = preferredDialog.PhraseTypeSequence.Select((entry, i) => new { i, entry })
                    .Where(a => a.i % 2 == 1)
                    .Select(z => z.entry)
                    .ToList()
                    .All(pts => mContext.CharactersList[character2Num].Phrases.Any(phrase => phrase.PhraseWeights.Keys.Contains(pts)));

                if (character1First && character2Second)
                    return true;
            }
            else
            {
                character1Second = preferredDialog.PhraseTypeSequence.Select((entry, i) => new { i, entry })
                                .Where(a => a.i % 2 == 1)
                                .Select(z => z.entry)
                                .ToList()
                                .All(pts => mContext.CharactersList[character1Num].Phrases.Any(phrase => phrase.PhraseWeights.Keys.Contains(pts)));

                character2First = preferredDialog.PhraseTypeSequence.Select((entry, i) => new { i, entry })
                                .Where(a => a.i % 2 == 0)
                                .Select(z => z.entry)
                                .ToList()
                                .All(pts => mContext.CharactersList[character2Num].Phrases.Any(phrase => phrase.PhraseWeights.Keys.Contains(pts)));
                if (character1Second && character2First)
                    return true;

            }

            return false;
        }

        private List<ModelDialog> _dialogsToRemove()
        {
            var _itemsToRemove = new List<ModelDialog>();
            ModelDialog _greetingDialog = null;

            int _recentlyUsed = 0;
            int _requirementsNotMet = 0;
            int _hasRecentPhrases = 0;
            int _isOneOfGreetingDialogs = 0;
            int _isGreetingFirstPhrase = 0;


            mContext.PossibleDialogModelsList.ForEach(dlg =>
            {
                bool _removeCriteriaMet = false;
                var _idx = mContext.DialogModelsList.IndexOf(dlg);
                if (_checkIfDialogModelUsedRecently(_idx))
                {
                    _removeCriteriaMet = true;
                    _recentlyUsed++;
                }

                if (!_removeCriteriaMet && !_checkIfDialogPreRequirementMet(_idx))
                {
                    _removeCriteriaMet = true;
                    _requirementsNotMet++;
                }

                if (!_removeCriteriaMet && _checkForRecentPhrases(_idx))
                {
                    _removeCriteriaMet = true;
                    _hasRecentPhrases++;
                }

                if (!_removeCriteriaMet && _isGreetingDialog(_idx))
                {
                    if (_greetingDialog != null)
                    {
                        _removeCriteriaMet = true;
                        _isOneOfGreetingDialogs++;
                        
                    }
                    else
                    {
                        _greetingDialog = dlg;
                    }
                }

                if (!_removeCriteriaMet && (_greetingDialog != null && dlg.PhraseTypeSequence.Contains("Greeting")))
                {

                    if (mContext.FirstRoundGone)
                    {
                        _removeCriteriaMet = true;
                        _isGreetingFirstPhrase++;
                    }
                }

                if (_removeCriteriaMet)
                {
                    _itemsToRemove.Add(dlg);
                }
            });

            if (_greetingDialog != null && !mContext.FirstRoundGone)
            {
                var _popularity = _greetingDialog.Popularity;
                if (_itemsToRemove.Where(dlg => dlg.Popularity > _popularity).Any())
                {
                    double _maxPopularity = _itemsToRemove.Max(dlg => dlg.Popularity);
                    var _mostPopularGreetingDlg = _itemsToRemove.First(dlg => dlg.Popularity == _maxPopularity);

                    _itemsToRemove.Remove(_mostPopularGreetingDlg);
                    _itemsToRemove.Add(_greetingDialog);
                }
            }

            mLogger.Info($"DIALOGS TO REMOVE - There are {_itemsToRemove.Count} dialogs to be removed from the list.");
            mLogger.Info($"DIALOGS TO REMOVE - " +
                $"{_recentlyUsed} recentrly used, " +
                $"{_requirementsNotMet} requirements not met, " +
                $"{_hasRecentPhrases} have recent phrases, " +
                $"{_isOneOfGreetingDialogs} greetins dialogs, " +
                $"{_isGreetingFirstPhrase} with a greeting as at least one of the phrases.");

            return _itemsToRemove;
        }

        #endregion

    }
}
