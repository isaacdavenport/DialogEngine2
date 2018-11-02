using DialogGenerator.DialogEngine.Model;
using DialogGenerator.Model;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DialogGenerator.DialogEngine.Model
{
    public class DialogContext
    {
        public int Character1Num = 0;
        public int Character2Num = 1;
        public bool SameCharactersAsLast;
        public Queue<int> RecentDialogs = new Queue<int>();
        public List<ModelDialog> DialogModelsList = new List<ModelDialog>();
        public ObservableCollection<Character> CharactersList = new ObservableCollection<Character>();
        public List<HistoricalDialog> HistoricalDialogs = new List<HistoricalDialog>();
        public List<HistoricalPhrase> HistoricalPhrases = new List<HistoricalPhrase>();
    }
}
