using System;

namespace DialogGenerator.DialogEngine.Model
{
    public class HistoricalPhrase
    {
        public int CharacterIndex;
        public string CharacterPrefix = "";
        public string PhraseFile = "";
        public int PhraseIndex;
        public DateTime StartedTime = DateTime.MinValue;
    }
}
