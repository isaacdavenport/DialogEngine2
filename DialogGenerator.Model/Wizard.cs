using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace DialogGenerator.Model
{
    public class Wizard : IEquatable<Wizard>, ICloneable
    {
        [JsonProperty("WizardName")]
        public string WizardName { get; set; }

        [JsonProperty("Commands")]
        public string Commands { get; set; }

        [JsonProperty("TutorialSteps")]
        public List<TutorialStep> TutorialSteps { get; set; }

        [JsonIgnore]
        public string FileName { get; set; }

        [JsonIgnore]
        public int JsonArrayIndex { get; set; }

        [JsonIgnore]
        public bool Editable { get; set; }

        public object Clone()
        {
            Wizard _wizard = new Wizard
            {
                WizardName = WizardName,
                Commands = Commands,
                FileName = FileName,
                JsonArrayIndex = JsonArrayIndex,
                Editable = Editable,
            };

            _wizard.TutorialSteps = new List<TutorialStep>();
            foreach(var _tutorialStep in TutorialSteps)
            {
                TutorialStep _clonedTutorialStep = (TutorialStep)_tutorialStep.Clone();
                _wizard.TutorialSteps.Add(_clonedTutorialStep);
            }

            return _wizard;
        }

        public bool Equals(Wizard other)
        {
            return this.WizardName.Equals(other.WizardName);
        }
    }
}
