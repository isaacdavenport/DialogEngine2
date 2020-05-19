using DialogGenerator.Core;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DialogGenerator.Model
{
    public class JSONObjectsTypesList
    {
        [JsonProperty("Version")]
        public string Version { get; set; } = ApplicationData.Instance.JSONFilesVersion;

        [JsonProperty("Editable", Required = Required.Default)]
        public bool Editable { get; set; } 

        [JsonProperty("Wizards",DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<Wizard> Wizards { get; set; } = new List<Wizard>();

        [JsonProperty("Characters",DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ObservableCollection<Character> Characters { get; set; } = new ObservableCollection<Character>();

        [JsonProperty("DialogModels",DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ObservableCollection<ModelDialogInfo> DialogModels { get; set; } = new ObservableCollection<ModelDialogInfo>();
    }
}
