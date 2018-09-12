using Newtonsoft.Json;
using System.Collections.Generic;

namespace DialogGenerator.Model
{
    public class TutorialStep
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
    }
}
