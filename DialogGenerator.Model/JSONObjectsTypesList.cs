using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DialogGenerator.Model
{
    public class JSONObjectsTypesList
    {
        [JsonProperty("Wizards")]
        public List<Wizard> Wizards { get; set; } = new List<Wizard>();

        [JsonProperty("Characters")]
        public ObservableCollection<Character> Characters { get; set; } = new ObservableCollection<Character>();

        [JsonProperty("DialogModels")]
        public ObservableCollection<ModelDialogInfo> DialogModels { get; set; } = new ObservableCollection<ModelDialogInfo>();
    }
}
