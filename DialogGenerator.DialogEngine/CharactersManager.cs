using DialogGenerator.Core;
using DialogGenerator.DataAccess;
using DialogGenerator.DialogEngine.Model;
using DialogGenerator.Model;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace DialogGenerator.DialogEngine
{
    public class CharactersManager
    {
        #region - fields -

        private ILogger mLogger;
        private DialogContext mContext;
        private ICharacterRepository mCharacterRepository;

        #endregion

        #region - ctor -

        public CharactersManager(ILogger logger,DialogContext context,ICharacterRepository _characterRepository)
        {
            mLogger = logger;
            mContext = context;
            mCharacterRepository = _characterRepository;
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
                //Nothing is needed to be done. 
                //The history dialogs collection will be reset the next time the arena starts.
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

            if (character.RecentPhrases.Count == 0)
            {
                for (var i = 0; i < Character.RecentPhrasesQueueSize && i < character.Phrases.Count / 2; i++)
                {
                    // we always deque after enque so this sets que size
                    character.RecentPhrases.Enqueue(character.Phrases[0]);
                }
            }
            
        }

        private void _removePhrasesOverParentalRating(Character _inCharacter)
        {
            var _maxParentalRating = ParentalRatings.GetNumeric(ApplicationData.Instance.CurrentParentalRating);
            var _minParentalRating = ParentalRatings.GetNumeric("G");

            _inCharacter.Phrases.Where(ph => ParentalRatings.GetNumeric(ph.PhraseRating) > _maxParentalRating
                                             || ParentalRatings.GetNumeric(ph.PhraseRating) < _minParentalRating)
                                .ToList()
                                .All(i => _inCharacter.Phrases.Remove(i));

        }

        #endregion

        #region - public functions -

        public void Initialize()
        {
            mContext.CharactersList = mCharacterRepository.GetAll();
            mContext.CharactersList.CollectionChanged += _charactersList_CollectionChanged;

            foreach (Character character in mContext.CharactersList)
            {
                _initializeCharacter(character);
            }

        }

        #endregion
    }
}
