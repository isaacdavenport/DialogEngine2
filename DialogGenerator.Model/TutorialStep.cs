using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DialogGenerator.Model
{
    public class TutorialStep : ICloneable
    {
        [JsonProperty("VideoFileName")]
        public string VideoFileName { get; set; }

        [JsonProperty("InstructionalText")]
        public string InstructionalText { get; set; }

        [JsonProperty("CollectUserInput")]
        public bool CollectUserInput { get; set; }

        [JsonProperty("Commands")]
        public string Commands { get; set; }

        [JsonProperty("PhraseWeights")]
        public Dictionary<string, double> PhraseWeights { get; set; }

        [JsonProperty("PhraseRating")]
        public string PhraseRating { get; set; }

        [JsonProperty("Popularity")]
        public double Popularity { get; set; }

        [JsonProperty("PlayUserRecordedAudioInContext")]
        public List<List<string>> PlayUserRecordedAudioInContext { get; set; }

        public object Clone()
        {
            TutorialStep _step = new TutorialStep
            {
                VideoFileName = VideoFileName,
                InstructionalText = InstructionalText,
                CollectUserInput = CollectUserInput,
                Commands = Commands,
                PhraseRating = PhraseRating,
                Popularity = Popularity,
            };

            _step.PhraseWeights = new Dictionary<string, double>();
            foreach(KeyValuePair<string,double> _phraseWeight in PhraseWeights)
            {
                _step.PhraseWeights.Add(_phraseWeight.Key, _phraseWeight.Value);
            }

            _step.PlayUserRecordedAudioInContext = new List<List<string>>();
            foreach(var _dialog in PlayUserRecordedAudioInContext)
            {
                var _dialogPhrases = new List<string>();
                foreach(var _dialogPhrase in _dialog)
                {
                    _dialogPhrases.Add(_dialogPhrase);
                }

                _step.PlayUserRecordedAudioInContext.Add(_dialogPhrases);
            }

            return _step;
        }
    }
}
