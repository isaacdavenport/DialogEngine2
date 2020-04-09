using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogGenerator.Model
{
    public class CharacterRadioBinding
    {
        [JsonProperty("CharacterPrefix")]
        public string CharacterPrefix { get; set; }

        [JsonProperty("RadioNumber")]
        public int RadioNumber { get; set; }
    }
}
