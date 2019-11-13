using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DialogGenerator.Model
{
    public class CreateCharacterWizardStep
    {
        public string Key { get; set; }
        public int StepIndex { get; set; }
        public string StepName { get; set; }
        public string StepDescription { get; set; }
        public string StepControl { get; set; }
    }
}
