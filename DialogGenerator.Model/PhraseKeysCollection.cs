using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogGenerator.Model
{
    public class PhraseKeysCollection
    {
        [JsonProperty("Version")]
        public string Version { get; set; }

        [JsonProperty("Phrases")]
        public ObservableCollection<PhraseKey> Phrases { get; set; } = new ObservableCollection<PhraseKey>();
    }
}
