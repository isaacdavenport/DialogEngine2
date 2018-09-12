using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace DialogGenerator.Model
{
    public class JSONObjectsTypesList
    {
        [JsonProperty("Wizards")]
        public ObservableCollection<Wizard> Wizards { get; set; } = new ObservableCollection<Wizard>();

        [JsonProperty("Characters")]
        public ObservableCollection<Character> Characters { get; set; } = new ObservableCollection<Character>();

        [JsonProperty("DialogModels")]
        public ObservableCollection<ModelDialogInfo> DialogModels { get; set; } = new ObservableCollection<ModelDialogInfo>();
    }
}
