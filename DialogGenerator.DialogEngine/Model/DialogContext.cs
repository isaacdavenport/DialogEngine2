using DialogGenerator.DialogEngine.Model;
using DialogGenerator.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DialogGenerator.DialogEngine.Model
{
    public class DialogContext
    {
        public int Character1Num = 1;
        public int Character2Num = 2;
        public bool SameCharactersAsLast;
        public List<ModelDialog> DialogModelsList = new List<ModelDialog>();
        public ObservableCollection<Character> CharactersList = new ObservableCollection<Character>();
        public List<HistoricalDialog> HistoricalDialogs = new List<HistoricalDialog>();
        public List<HistoricalPhrase> HistoricalPhrases = new List<HistoricalPhrase>();
    }
}
